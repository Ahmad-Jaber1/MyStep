using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;
using Repository;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace Services;

public class TaskSearchVectorService : ITaskSearchVectorService
{
    private const int ExpectedVectorSize = 1024;
    private const double MasteryThreshold = 0.7;
    private const int DefaultTopTaskCount = 3;

    private readonly ITaskItemRepo _taskItemRepo;
    private readonly ILearningObjectiveRepo _learningObjectiveRepo;
    private readonly IStudentLearningObjectiveRepo _studentLearningObjectiveRepo;
    private readonly IStudentTaskRepo _studentTaskRepo;
    private readonly IStudentRepo _studentRepo;
    private readonly ISkillRepo _skillRepo;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IGenerationClient _generationClient;
    private readonly ITaskTargetRepo _taskTargetRepo;
    private readonly ITaskPrerequisiteRepo _taskPrerequisiteRepo;
    private readonly MyStepDbContext _dbContext;

    private sealed record PrerequisitePromptItem(int SkillId, string SkillName, int ObjectiveId, string ObjectiveDescription, double Score);
    private sealed record TargetPromptItem(int ObjectiveId, string ObjectiveDescription, double Score);

    private sealed record GenerationContext(
        TaskGenerationPreparationResponseDto Preparation,
        List<PrerequisitePromptItem> PrerequisiteDetails,
        List<TargetPromptItem> TargetDetails,
        HashSet<int> AllowedPrerequisiteObjectiveIds,
        HashSet<int> AllowedTargetObjectiveIds);

    private sealed class GeneratedAdditionalSkill
    {
        [JsonPropertyName("skill_id")]
        public int SkillId { get; set; }

        [JsonPropertyName("skill_name")]
        public string SkillName { get; set; } = string.Empty;

        [JsonPropertyName("used_learning_goal")]
        public int UsedLearningGoal { get; set; }

        [JsonPropertyName("justification")]
        public string Justification { get; set; } = string.Empty;
    }

    private sealed class GeneratedTaskPayload
    {
        [JsonPropertyName("targeted_objectives")]
        public List<int> TargetedObjectives { get; set; } = [];

        [JsonPropertyName("additional_skills_required")]
        public List<GeneratedAdditionalSkill> AdditionalSkillsRequired { get; set; } = [];
    }

    public TaskSearchVectorService(
        ITaskItemRepo taskItemRepo,
        ILearningObjectiveRepo learningObjectiveRepo,
        IStudentLearningObjectiveRepo studentLearningObjectiveRepo,
        IStudentTaskRepo studentTaskRepo,
        IStudentRepo studentRepo,
        ISkillRepo skillRepo,
        IEmbeddingClient embeddingClient,
        IGenerationClient generationClient,
        ITaskTargetRepo taskTargetRepo,
        ITaskPrerequisiteRepo taskPrerequisiteRepo,
        MyStepDbContext dbContext)
    {
        _taskItemRepo = taskItemRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
        _studentLearningObjectiveRepo = studentLearningObjectiveRepo;
        _studentTaskRepo = studentTaskRepo;
        _studentRepo = studentRepo;
        _skillRepo = skillRepo;
        _embeddingClient = embeddingClient;
        _generationClient = generationClient;
        _taskTargetRepo = taskTargetRepo;
        _taskPrerequisiteRepo = taskPrerequisiteRepo;
        _dbContext = dbContext;
    }

    public async Task<Result<float[]>> BuildVectorAsync(TaskItem task)
    {
        if (task is null)
        {
            return Result<float[]>.Failure("Task is required.");
        }

        var learningObjectives = await GetLearningObjectiveMapAsync();
        return await BuildVectorAsync(task, learningObjectives);
    }

    public async Task<Result<TaskSearchVectorRebuildResponseDto>> RebuildAllAsync()
    {
        var tasks = await _taskItemRepo.GetAllAsync();
        var learningObjectives = await GetLearningObjectiveMapAsync();
        var summary = new TaskSearchVectorRebuildResponseDto
        {
            TotalTasks = tasks.Count
        };

        foreach (var task in tasks)
        {
            var vectorResult = await BuildVectorAsync(task, learningObjectives);
            if (!vectorResult.IsSuccess)
            {
                summary.Failures.Add(new TaskSearchVectorRebuildFailureDto
                {
                    TaskId = task.Id,
                    ErrorMessage = vectorResult.ErrorMessage ?? "Unknown embedding failure."
                });
                continue;
            }

            task.SearchVector = new Pgvector.Vector(vectorResult.Data!);
            await _taskItemRepo.UpdateAsync(task);
            summary.UpdatedTasks++;
        }

        return Result<TaskSearchVectorRebuildResponseDto>.Success(summary);
    }

    public async Task<Result<TaskGenerationPreparationResponseDto>> PrepareTaskGenerationAsync(Guid studentId, int mainSkillId)
    {
        var contextResult = await BuildGenerationContextAsync(studentId, mainSkillId);
        if (!contextResult.IsSuccess || contextResult.Data is null)
        {
            return Result<TaskGenerationPreparationResponseDto>.Failure(contextResult.ErrorMessage ?? "Task generation preparation failed.");
        }

        return Result<TaskGenerationPreparationResponseDto>.Success(contextResult.Data.Preparation);
    }

    public async Task<Result<JsonDocument>> GenerateTaskAsync(Guid studentId, int mainSkillId)
    {
        var contextResult = await BuildGenerationContextAsync(studentId, mainSkillId);
        if (!contextResult.IsSuccess || contextResult.Data is null)
        {
            return Result<JsonDocument>.Failure(contextResult.ErrorMessage ?? "Task generation preparation failed.");
        }

        var generationResult = await _generationClient.GenerateContentAsync(contextResult.Data.Preparation.GenerationPrompt);
        if (!generationResult.IsSuccess)
        {
            return Result<JsonDocument>.Failure(generationResult.ErrorMessage ?? "Task generation failed.");
        }

        if (!TryParseGeneratedTask(generationResult.Data!, out var generatedTask, out var generatedPayload, out var parseError))
        {
            return Result<JsonDocument>.Failure(parseError);
        }

        var persistResult = await PersistGeneratedTaskAsync(studentId, mainSkillId, contextResult.Data, generatedTask, generatedPayload!);
        if (!persistResult.IsSuccess)
        {
            return Result<JsonDocument>.Failure(persistResult.ErrorMessage ?? "Failed to persist generated task.");
        }

        return Result<JsonDocument>.Success(generatedTask);
    }

    private async Task<Result<GenerationContext>> BuildGenerationContextAsync(Guid studentId, int mainSkillId)
    {
        var student = await _studentRepo.GetByIdAsync(studentId);
        if (student is null)
        {
            return Result<GenerationContext>.Failure($"Student with ID {studentId} was not found.");
        }

        var hasUnpassedTask = await _studentTaskRepo.HasUnpassedTaskForMainSkillAsync(studentId, mainSkillId);
        if (hasUnpassedTask)
        {
            return Result<GenerationContext>.Failure("There is already an unfinished task for this student in the selected main skill.");
        }

        var mainSkill = await _skillRepo.GetByIdAsync(mainSkillId);
        if (mainSkill is null)
        {
            return Result<GenerationContext>.Failure($"Main skill with ID {mainSkillId} was not found.");
        }

        var allPathSkills = await _skillRepo.GetByPathIdAsync(mainSkill.PathId);
        var pathSkillIds = allPathSkills
            .Select(skill => skill.Id)
            .ToHashSet();

        var learningObjectives = await _learningObjectiveRepo.GetAllAsync();
        var objectivesBySkillId = learningObjectives
            .GroupBy(objective => objective.SkillId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Id).ToList());
        var skillsById = allPathSkills.ToDictionary(skill => skill.Id);

        var studentObjectives = await _studentLearningObjectiveRepo.GetByStudentIdAsync(studentId);
        var studentScoreByObjectiveId = studentObjectives
            .GroupBy(item => item.LearningObjectiveId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.LastUpdated).First().Score);

        var prerequisiteDetails = learningObjectives
            .Where(objective => objective.SkillId != mainSkillId
                && pathSkillIds.Contains(objective.SkillId)
                && studentScoreByObjectiveId.TryGetValue(objective.Id, out var score)
                && score >= MasteryThreshold
                && !string.IsNullOrWhiteSpace(objective.Description))
            .OrderBy(objective => objective.SkillId)
            .ThenBy(objective => objective.Id)
            .Select(objective => new PrerequisitePromptItem(
                objective.SkillId,
                skillsById.TryGetValue(objective.SkillId, out var skill) ? skill.Name : $"Skill {objective.SkillId}",
                objective.Id,
                objective.Description.Trim(),
                studentScoreByObjectiveId[objective.Id]))
            .ToList();

        var normalizedPrerequisites = prerequisiteDetails
            .Select(item => item.ObjectiveDescription)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(description => description, StringComparer.Ordinal)
            .ToList();

        var targetDetails = objectivesBySkillId.TryGetValue(mainSkillId, out var mainSkillObjectives)
            ? mainSkillObjectives
                .Where(objective => !string.IsNullOrWhiteSpace(objective.Description))
                .Select(objective => new TargetPromptItem(
                    objective.Id,
                    objective.Description.Trim(),
                    studentScoreByObjectiveId.TryGetValue(objective.Id, out var score) ? score : 0d))
                .Where(item => item.Score < MasteryThreshold)
                .OrderBy(item => item.ObjectiveId)
                .ToList()
            : [];

        var normalizedTargets = targetDetails
            .Select(item => item.ObjectiveDescription)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(description => description, StringComparer.Ordinal)
            .ToList();

        var inputText = TaskSearchVectorBuilder.BuildInputText(normalizedPrerequisites, normalizedTargets);
        var embeddingResult = await _embeddingClient.CreateEmbeddingAsync(inputText);
        if (!embeddingResult.IsSuccess)
        {
            return Result<GenerationContext>.Failure(embeddingResult.ErrorMessage!);
        }

        if (embeddingResult.Data is null || embeddingResult.Data.Length != ExpectedVectorSize)
        {
            return Result<GenerationContext>.Failure($"Embedding API returned vector with {embeddingResult.Data?.Length ?? 0} values, expected {ExpectedVectorSize}.");
        }

        var queryVector = embeddingResult.Data;
        var similarTasks = await _taskItemRepo.GetMostSimilarByVectorAsync(mainSkillId, new Pgvector.Vector(queryVector), DefaultTopTaskCount);

        var taskMatches = new List<StudentTaskMatchResponseDto>();
        foreach (var task in similarTasks)
        {
            var similarityScore = ComputeCosineSimilarity(queryVector, task.SearchVector.ToArray());
            var taskMatch = new StudentTaskMatchResponseDto
            {
                TaskId = task.Id,
                PathId = task.PathId,
                MainSkillId = task.MainSkillId,
                TaskData = task.TaskData,
                SimilarityScore = similarityScore
            };
            
            // Enrich task data with database IDs and descriptions
            await EnrichTaskDataAsync(taskMatch);
            taskMatches.Add(taskMatch);
        }

        var generationPrompt = BuildGenerationPrompt(
            mainSkill,
            allPathSkills,
            objectivesBySkillId,
            prerequisiteDetails,
            targetDetails,
            taskMatches);

        var preparation = new TaskGenerationPreparationResponseDto
        {
            StudentId = studentId,
            MainSkillId = mainSkillId,
            MainSkillName = mainSkill.Name,
            PathId = mainSkill.PathId,
            PathName = mainSkill.Path.Name,
            MasteryThreshold = MasteryThreshold,
            PrerequisiteLearningObjectives = normalizedPrerequisites,
            TargetLearningObjectives = normalizedTargets,
            InputText = inputText,
            QueryVector = queryVector,
            GenerationPrompt = generationPrompt,
            TopTaskMatches = taskMatches
        };

        return Result<GenerationContext>.Success(new GenerationContext(
            preparation,
            prerequisiteDetails,
            targetDetails,
            prerequisiteDetails.Select(item => item.ObjectiveId).ToHashSet(),
            targetDetails.Select(item => item.ObjectiveId).ToHashSet()));
    }

    private static string BuildGenerationPrompt(
        Skill mainSkill,
        IReadOnlyCollection<Skill> pathSkills,
        IReadOnlyDictionary<int, List<LearningObjective>> objectivesBySkillId,
        IReadOnlyCollection<PrerequisitePromptItem> prerequisiteDetails,
        IReadOnlyCollection<TargetPromptItem> targetDetails,
        IReadOnlyCollection<StudentTaskMatchResponseDto> similarTasks)
    {
        var builder = new StringBuilder();
        var path = mainSkill.Path ?? throw new InvalidOperationException($"Main skill {mainSkill.Id} does not have a loaded path.");

        var pathLabel = string.IsNullOrWhiteSpace(path.Description)
            ? path.Name
            : $"{path.Name} ({path.Description.Trim()})";

        builder.AppendLine($"You are a senior task designer for the {pathLabel} path.");
        builder.AppendLine($"Generate exactly ONE medium-sized task for the {mainSkill.Name} skill.");
        builder.AppendLine("Return ONLY valid JSON. No markdown. No explanation text.");
        builder.AppendLine();

        builder.AppendLine("INPUT CONTEXT");
        builder.AppendLine($"- path_id: {mainSkill.PathId}");
        builder.AppendLine($"- path_name: {path.Name}");
        if (!string.IsNullOrWhiteSpace(path.Description))
        {
            builder.AppendLine($"- path_description: {path.Description.Trim()}");
        }
        builder.AppendLine($"- main_skill_id: {mainSkill.Id}");
        builder.AppendLine($"- main_skill_name: {mainSkill.Name}");
        if (!string.IsNullOrWhiteSpace(mainSkill.Description))
        {
            builder.AppendLine($"- main_skill_description: {mainSkill.Description.Trim()}");
        }
        builder.AppendLine();

        builder.AppendLine("ALL SKILLS IN THIS PATH (WITH OBJECTIVES)");
        foreach (var skill in pathSkills.OrderBy(skill => skill.Id))
        {
            builder.AppendLine($"- skill_id: {skill.Id}, skill_name: {skill.Name}");
            if (!objectivesBySkillId.TryGetValue(skill.Id, out var objectives) || objectives.Count == 0)
            {
                builder.AppendLine("  - objectives: []");
                continue;
            }

            foreach (var objective in objectives)
            {
                var description = objective.Description?.Trim() ?? string.Empty;
                builder.AppendLine($"  - objective_id: {objective.Id}, description: {description}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("PREREQUISITE LEARNING OBJECTIVES FOR THIS STUDENT");
        builder.AppendLine("These are objectives the student already understands well and can rely on while solving this task.");
        builder.AppendLine("Important: if this task needs any prerequisite support, you must use only the objectives in this list.");
        builder.AppendLine("Do not invent, assume, or use any prerequisite objective outside this list.");
        builder.AppendLine("Assume the student knows only what is explicitly present in this prerequisite list.");
        builder.AppendLine("Do not assume hidden or unstated prior knowledge.");
        builder.AppendLine("They are necessary supporting objectives from other skills in the same path.");
        if (prerequisiteDetails.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var prerequisite in prerequisiteDetails)
            {
                builder.AppendLine($"- skill_id: {prerequisite.SkillId}, skill_name: {prerequisite.SkillName}, objective_id: {prerequisite.ObjectiveId}, description: {prerequisite.ObjectiveDescription}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("TARGET LEARNING OBJECTIVES FOR THIS STAGE");
        builder.AppendLine("These are the objectives the student must practice in this stage for the main skill.");
        builder.AppendLine("The generated task MUST target a subset of these objectives, and may include all of them if appropriate.");
        if (targetDetails.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            foreach (var target in targetDetails)
            {
                builder.AppendLine($"- skill_id: {mainSkill.Id}, skill_name: {mainSkill.Name}, objective_id: {target.ObjectiveId}, description: {target.ObjectiveDescription}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("TARGET OBJECTIVE COUNT GUIDANCE");
        builder.AppendLine("- If the chosen target objectives are conceptually hard, advanced, or require deeper implementation, keep the task focused on 1-2 target objectives.");
        builder.AppendLine("- If the chosen target objectives are medium difficulty, use 2-3 target objectives.");
        builder.AppendLine("- If the chosen target objectives are simple or foundational, use 3-5 target objectives.");
        builder.AppendLine("- Never place too many objectives in one task; keep the task focused and realistic.");

        builder.AppendLine();
        builder.AppendLine("IMPORTANT ABOUT EXAMPLES");
        builder.AppendLine("Similar tasks may include target objectives or prerequisite objectives that are NOT valid for this student.");
        builder.AppendLine("Treat examples as style/depth reference only.");
        builder.AppendLine("For this generated task, use only objectives from TARGET LEARNING OBJECTIVES and only prerequisites from PREREQUISITE LEARNING OBJECTIVES.");
        builder.AppendLine();
        builder.AppendLine("SIMILAR TASKS EXAMPLES");
        builder.AppendLine("Use these tasks as reference examples for style and depth. Do not copy them.");
        var orderedTasks = similarTasks
            .OrderByDescending(task => task.SimilarityScore)
            .Take(DefaultTopTaskCount)
            .ToList();
        if (orderedTasks.Count == 0)
        {
            builder.AppendLine("- no similar tasks available");
        }
        else
        {
            var index = 1;
            foreach (var task in orderedTasks)
            {
                builder.AppendLine($"- example_{index}: task_id={task.TaskId}, similarity={task.SimilarityScore:0.0000}");
                builder.AppendLine(task.TaskData.RootElement.GetRawText());
                index++;
            }
        }

        builder.AppendLine();
        builder.AppendLine("STRICT TASK GENERATION RULES");
        builder.AppendLine("1) Task size is mandatory: estimated solution should be 40-150 lines of implementation code.");
        builder.AppendLine("2) Task should represent ONE feature only, not full system, not mini-project, not over-engineered architecture.");
        builder.AppendLine("3) Scenario must feel like a real business assignment from a company, with a concrete stakeholder, process, input, output, and constraints.");
        builder.AppendLine("4) targeted_objectives must contain only numeric objective IDs from TARGET LEARNING OBJECTIVES list.");
        builder.AppendLine("5) The generated task must target at least one objective from TARGET LEARNING OBJECTIVES.");
        builder.AppendLine("6) additional_skills_required may include one or more prerequisite objectives only from PREREQUISITE LEARNING OBJECTIVES list.");
        builder.AppendLine("7) For each additional_skills_required item, add skill_id, skill_name, used_learning_goal, and justification.");
        builder.AppendLine("8) used_learning_goal must be a numeric objective ID only.");
        builder.AppendLine("9) validation_criteria is critical and must include one or more checks for each targeted objective.");
        builder.AppendLine("10) Every targeted objective must have one or more validation points.");
        builder.AppendLine("11) Every prerequisite objective actually used in the generated task must have one or more validation points.");
        builder.AppendLine("12) validation_criteria must also include business-logic correctness checks for scenario requirements.");
        builder.AppendLine("13) For business-logic validations not tied to one learning objective, set both skill_id and related_learning_objective to 0.");
        builder.AppendLine("14) Every validation entry must include skill_id and related_learning_objective fields.");
        builder.AppendLine("15) Keep validations atomic, objective, and technically verifiable from implementation behavior.");
        builder.AppendLine("16) instructions must guide student step by step without revealing full solution code.");
        builder.AppendLine("17) hints must be progressive from general to specific, still without giving final code.");
        builder.AppendLine("18) Keep output language clear and professional.");
        builder.AppendLine("19) Output MUST be JSON only.");
        builder.AppendLine("20) Similar task examples are reference-only; never copy their target or prerequisite objectives unless they are present in this student's allowed lists.");
        builder.AppendLine("21) Build the task only on what the student is known to understand from PREREQUISITE LEARNING OBJECTIVES plus the chosen TARGET LEARNING OBJECTIVES.");
        builder.AppendLine("22) If the task requires prerequisite knowledge, include EVERY required prerequisite objective in additional_skills_required (from the allowed prerequisite list only).");
        builder.AppendLine("23) Never design a task that depends on unstated prerequisite knowledge.");
        builder.AppendLine();

        builder.AppendLine("STRICT VALIDATION RULES (MANDATORY)");
        builder.AppendLine("- The JSON output MUST strictly match the required schema. No missing, renamed, or extra fields are allowed.");
        builder.AppendLine("- The scenario object MUST contain exactly two fields: story and requirement. No additional or renamed fields are allowed.");
        builder.AppendLine("- targeted_objectives count MUST follow TARGET OBJECTIVE COUNT GUIDANCE.");
        builder.AppendLine("  - If task difficulty is medium: ONLY 2-3 targeted objectives are allowed.");
        builder.AppendLine("  - Exceeding the allowed range for chosen difficulty is invalid.");
        builder.AppendLine("- additional_skills_required MUST include only objectives from PREREQUISITE LEARNING OBJECTIVES.");
        builder.AppendLine("  - Using any objective outside this list is forbidden.");
        builder.AppendLine("  - If none are needed, return an empty array.");
        builder.AppendLine("- Do not copy objective IDs from SIMILAR TASKS EXAMPLES unless those IDs also exist in this student's TARGET LEARNING OBJECTIVES or PREREQUISITE LEARNING OBJECTIVES.");
        builder.AppendLine("- Do not assume student knowledge outside PREREQUISITE LEARNING OBJECTIVES.");
        builder.AppendLine("- If solving the task needs prerequisite objectives, ALL such required objectives must appear in additional_skills_required.");
        builder.AppendLine("- validation_criteria MUST:");
        builder.AppendLine("  - Include one or more criteria for EACH targeted_objective.");
        builder.AppendLine("  - Include one or more criteria for EACH objective used in additional_skills_required.");
        builder.AppendLine("  - Not reference any objective outside targeted_objectives or additional_skills_required.");
        builder.AppendLine("- skill_name values MUST exactly match provided skill names (case-sensitive, no rewording).");
        builder.AppendLine("- All validation criteria MUST be atomic, testable, and aligned with implementation behavior.");
        builder.AppendLine("- Before final output, internally verify:");
        builder.AppendLine("  - JSON structure is valid and complete.");
        builder.AppendLine("  - Objective counts are correct.");
        builder.AppendLine("  - No unauthorized objectives are used.");
        builder.AppendLine("  - validation_criteria fully aligns with objectives.");
        builder.AppendLine("- If any rule is violated, regenerate the output before responding.");
        builder.AppendLine("- Output MUST be only one JSON object. No explanations, no markdown, no extra text.");
        builder.AppendLine();

        builder.AppendLine("OUTPUT FORMAT (DO NOT RENAME EXISTING FIELDS)");
        builder.AppendLine("{");
        builder.AppendLine("  \"task_name\": \"Professional descriptive title\",");
        builder.AppendLine("  \"skill_category\": \"Main Skill Name\",");
        builder.AppendLine("  \"scenario\": {");
        builder.AppendLine("    \"story\": \"Realistic business story.\",");
        builder.AppendLine("    \"requirement\": \"Concrete technical implementation requirements.\"");
        builder.AppendLine("  },");
        builder.AppendLine("  \"targeted_objectives\": [");
        builder.AppendLine("    0");
        builder.AppendLine("  ],");
        builder.AppendLine("  \"additional_skills_required\": [");
        builder.AppendLine("    {");
        builder.AppendLine("      \"skill_id\": 0,");
        builder.AppendLine("      \"skill_name\": \"Skill Name from allowed list\",");
        builder.AppendLine("      \"used_learning_goal\": 0,");
        builder.AppendLine("      \"justification\": \"Precise technical reason this objective is required.\"");
        builder.AppendLine("    }");
        builder.AppendLine("  ],");
        builder.AppendLine("  \"instructions\": [");
        builder.AppendLine("    \"Step-by-step senior developer style instructions.\",");
        builder.AppendLine("    \"Must guide but NEVER reveal solution code.\"");
        builder.AppendLine("  ],");
        builder.AppendLine("  \"validation_criteria\": [");
        builder.AppendLine("    {");
        builder.AppendLine("      \"skill_id\": 0,");
        builder.AppendLine("      \"criterion\": \"Atomic, technical, objectively verifiable rule.\",");
        builder.AppendLine("      \"related_learning_objective\": 0");
        builder.AppendLine("    }");
        builder.AppendLine("  ],");
        builder.AppendLine("  \"hints\": [");
        builder.AppendLine("    \"Progressive hints from general to specific.\"");
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("FINAL OUTPUT REQUIREMENT: return only one valid JSON object matching this format.");

        return builder.ToString();
    }

    private async Task<IReadOnlyDictionary<int, LearningObjective>> GetLearningObjectiveMapAsync()
    {
        var learningObjectives = await _learningObjectiveRepo.GetAllAsync();
        return learningObjectives.ToDictionary(objective => objective.Id);
    }

    private async Task<Result<float[]>> BuildVectorAsync(TaskItem task, IReadOnlyDictionary<int, LearningObjective> learningObjectives)
    {
        var inputText = TaskSearchVectorBuilder.BuildInputText(task, learningObjectives);
        var embeddingResult = await _embeddingClient.CreateEmbeddingAsync(inputText);
        if (!embeddingResult.IsSuccess)
        {
            return embeddingResult;
        }

        if (embeddingResult.Data is null || embeddingResult.Data.Length != ExpectedVectorSize)
        {
            return Result<float[]>.Failure($"Embedding API returned vector with {embeddingResult.Data?.Length ?? 0} values, expected {ExpectedVectorSize}.");
        }

        return Result<float[]>.Success(embeddingResult.Data);
    }

    private static double ComputeCosineSimilarity(float[] queryVector, float[] candidateVector)
    {
        if (queryVector.Length != candidateVector.Length || queryVector.Length == 0)
        {
            return 0;
        }

        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (var i = 0; i < queryVector.Length; i++)
        {
            dot += queryVector[i] * candidateVector[i];
            normA += queryVector[i] * queryVector[i];
            normB += candidateVector[i] * candidateVector[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    /// <summary>
    /// Enriches task data by replacing local IDs with database IDs and full descriptions.
    /// This method modifies the TaskData JSON to include:
    /// - targeted_objectives: array of objects with {id, description}
    /// - additional_skills_required: array of objects with {skill_id, skill_name, learning_goal_id, justification}
    /// </summary>
    private async Task EnrichTaskDataAsync(StudentTaskMatchResponseDto taskMatch)
    {
        try
        {
            if (taskMatch.TaskData is null)
                return;

            // Fetch targets and prerequisites for this task
            var targets = await _taskTargetRepo.GetByTaskIdAsync(taskMatch.TaskId);
            var prerequisites = await _taskPrerequisiteRepo.GetByTaskIdAsync(taskMatch.TaskId);

            // Get all learning objectives for quick lookup
            var allLearningObjectives = await _learningObjectiveRepo.GetAllAsync();
            var objectivesById = allLearningObjectives.ToDictionary(obj => obj.Id);

            // Get all skills for quick lookup
            var allSkills = await _skillRepo.GetAllAsync();
            var skillsById = allSkills.ToDictionary(skill => skill.Id);

            // Build enriched targeted objectives
            var enrichedTargets = new List<object>();
            foreach (var target in targets)
            {
                if (objectivesById.TryGetValue(target.LearningObjectiveId, out var objective))
                {
                    enrichedTargets.Add(new
                    {
                        id = objective.Id,
                        description = objective.Description
                    });
                }
            }

            // Build enriched additional skills (prerequisites)
            var enrichedSkills = new List<object>();
            foreach (var prerequisite in prerequisites)
            {
                if (objectivesById.TryGetValue(prerequisite.LearningObjectiveId, out var objective) &&
                    skillsById.TryGetValue(objective.SkillId, out var skill))
                {
                    enrichedSkills.Add(new
                    {
                        skill_id = skill.Id,
                        skill_name = skill.Name,
                        learning_goal_id = objective.Id,
                        justification = prerequisite.Justification
                    });
                }
            }

            // Parse current task data
            var root = taskMatch.TaskData.RootElement;
            
            // Build a dictionary to hold the new enriched task data
            var enrichedDataDict = new Dictionary<string, object>();
            
            // Copy over existing properties
            if (root.TryGetProperty("task_name", out var taskNameElem) && taskNameElem.ValueKind != JsonValueKind.Null)
                enrichedDataDict["task_name"] = taskNameElem.GetString()!;
            
            if (root.TryGetProperty("skill_category", out var skillCatElem) && skillCatElem.ValueKind != JsonValueKind.Null)
                enrichedDataDict["skill_category"] = skillCatElem.GetString()!;
            
            if (root.TryGetProperty("scenario", out var scenarioElem) && scenarioElem.ValueKind != JsonValueKind.Null)
            {
                // Convert scenario to appropriate object or keep as string
                if (scenarioElem.ValueKind == JsonValueKind.Object || scenarioElem.ValueKind == JsonValueKind.Array)
                    enrichedDataDict["scenario"] = JsonSerializer.Deserialize<object>(scenarioElem.GetRawText())!;
                else
                    enrichedDataDict["scenario"] = scenarioElem.GetString()!;
            }
            
            if (root.TryGetProperty("instructions", out var instructionsElem) && instructionsElem.ValueKind != JsonValueKind.Null)
            {
                if (instructionsElem.ValueKind == JsonValueKind.Array)
                    enrichedDataDict["instructions"] = JsonSerializer.Deserialize<object>(instructionsElem.GetRawText())!;
                else
                    enrichedDataDict["instructions"] = instructionsElem.GetString()!;
            }
            
            if (root.TryGetProperty("validation_criteria", out var criteriaElem) && criteriaElem.ValueKind != JsonValueKind.Null)
            {
                if (criteriaElem.ValueKind == JsonValueKind.Array)
                    enrichedDataDict["validation_criteria"] = JsonSerializer.Deserialize<object>(criteriaElem.GetRawText())!;
                else
                    enrichedDataDict["validation_criteria"] = criteriaElem.GetString()!;
            }
            
            if (root.TryGetProperty("hints", out var hintsElem) && hintsElem.ValueKind != JsonValueKind.Null)
            {
                if (hintsElem.ValueKind == JsonValueKind.Array)
                    enrichedDataDict["hints"] = JsonSerializer.Deserialize<object>(hintsElem.GetRawText())!;
                else
                    enrichedDataDict["hints"] = hintsElem.GetString()!;
            }
            
            // Add enriched data - these replace any existing values
            enrichedDataDict["targeted_objectives"] = enrichedTargets;
            enrichedDataDict["additional_skills_required"] = enrichedSkills;

            // Convert enriched data to JsonDocument
            var options = new JsonSerializerOptions { WriteIndented = false };
            var jsonString = JsonSerializer.Serialize(enrichedDataDict, options);
            taskMatch.TaskData = JsonDocument.Parse(jsonString);
        }
        catch (Exception)
        {
            // If enrichment fails, keep the original task data
            // Log error if logging is available
        }
    }

    private async Task<Result<bool>> PersistGeneratedTaskAsync(
        Guid studentId,
        int mainSkillId,
        GenerationContext context,
        JsonDocument generatedTask,
        GeneratedTaskPayload payload)
    {
        if (payload.TargetedObjectives.Count == 0)
        {
            return Result<bool>.Failure("Generated task must include at least one target objective.");
        }

        var unknownTargetIds = payload.TargetedObjectives.Where(id => !context.AllowedTargetObjectiveIds.Contains(id)).Distinct().ToList();
        if (unknownTargetIds.Count > 0)
        {
            return Result<bool>.Failure($"Generated task included target objectives outside the allowed list: {string.Join(", ", unknownTargetIds)}.");
        }

        var unknownPrerequisiteIds = payload.AdditionalSkillsRequired
            .Select(item => item.UsedLearningGoal)
            .Where(id => !context.AllowedPrerequisiteObjectiveIds.Contains(id))
            .Distinct()
            .ToList();
        if (unknownPrerequisiteIds.Count > 0)
        {
            return Result<bool>.Failure($"Generated task included prerequisite objectives outside the allowed list: {string.Join(", ", unknownPrerequisiteIds)}.");
        }

        var taskEntity = new TaskItem
        {
            Id = Guid.NewGuid(),
            PathId = context.Preparation.PathId,
            MainSkillId = mainSkillId,
            TaskData = generatedTask,
            SearchVector = new Pgvector.Vector(new float[ExpectedVectorSize])
        };

        taskEntity.Targets = payload.TargetedObjectives
            .Select(objectiveId => new TaskTarget
            {
                TaskId = taskEntity.Id,
                LearningObjectiveId = objectiveId,
                Task = taskEntity
            })
            .ToList();

        taskEntity.Prerequisites = payload.AdditionalSkillsRequired
            .Select(item => new TaskPrerequisite
            {
                TaskId = taskEntity.Id,
                LearningObjectiveId = item.UsedLearningGoal,
                Justification = item.Justification,
                Task = taskEntity
            })
            .ToList();

        var taskVectorResult = await BuildVectorAsync(taskEntity);
        if (!taskVectorResult.IsSuccess)
        {
            return Result<bool>.Failure(taskVectorResult.ErrorMessage ?? "Failed to build task vector.");
        }

        taskEntity.SearchVector = new Pgvector.Vector(taskVectorResult.Data!);

        var existingCount = await _studentTaskRepo.GetCountByStudentAndMainSkillAsync(studentId, mainSkillId);
        var studentTask = new StudentTask
        {
            StudentId = studentId,
            TaskId = taskEntity.Id,
            NumberInMainSkill = existingCount + 1,
            Passed = false,
            StartedAt = DateTime.UtcNow
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await _taskItemRepo.AddAsync(taskEntity);

            foreach (var target in taskEntity.Targets)
            {
                await _taskTargetRepo.AddAsync(target);
            }

            foreach (var prerequisite in taskEntity.Prerequisites)
            {
                await _taskPrerequisiteRepo.AddAsync(prerequisite);
            }

            await _studentTaskRepo.AddAsync(studentTask);

            await transaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<bool>.Failure($"Failed to persist generated task: {ex.Message}");
        }
    }

    private static bool TryParseGeneratedTask(string content, out JsonDocument generatedTask, out GeneratedTaskPayload? payload, out string errorMessage)
    {
        generatedTask = null!;
        payload = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            errorMessage = "Generation API returned an empty task payload.";
            return false;
        }

        try
        {
            generatedTask = JsonDocument.Parse(content);
            payload = JsonSerializer.Deserialize<GeneratedTaskPayload>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload is null)
            {
                errorMessage = "Generation API returned an empty task payload.";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = $"Generation API returned invalid JSON content: {ex.Message}";
            return false;
        }
    }
}