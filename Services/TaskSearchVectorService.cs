using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;
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
    private readonly ISkillRepo _skillRepo;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly ITaskTargetRepo _taskTargetRepo;
    private readonly ITaskPrerequisiteRepo _taskPrerequisiteRepo;

    private sealed record PrerequisitePromptItem(int SkillId, string SkillName, int ObjectiveId, string ObjectiveDescription, double Score);
    private sealed record TargetPromptItem(int ObjectiveId, string ObjectiveDescription, double Score);

    public TaskSearchVectorService(
        ITaskItemRepo taskItemRepo,
        ILearningObjectiveRepo learningObjectiveRepo,
        IStudentLearningObjectiveRepo studentLearningObjectiveRepo,
        ISkillRepo skillRepo,
        IEmbeddingClient embeddingClient,
        ITaskTargetRepo taskTargetRepo,
        ITaskPrerequisiteRepo taskPrerequisiteRepo)
    {
        _taskItemRepo = taskItemRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
        _studentLearningObjectiveRepo = studentLearningObjectiveRepo;
        _skillRepo = skillRepo;
        _embeddingClient = embeddingClient;
        _taskTargetRepo = taskTargetRepo;
        _taskPrerequisiteRepo = taskPrerequisiteRepo;
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
        var mainSkill = await _skillRepo.GetByIdAsync(mainSkillId);
        if (mainSkill is null)
        {
            return Result<TaskGenerationPreparationResponseDto>.Failure($"Main skill with ID {mainSkillId} was not found.");
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
            return Result<TaskGenerationPreparationResponseDto>.Failure(embeddingResult.ErrorMessage!);
        }

        if (embeddingResult.Data is null || embeddingResult.Data.Length != ExpectedVectorSize)
        {
            return Result<TaskGenerationPreparationResponseDto>.Failure($"Embedding API returned vector with {embeddingResult.Data?.Length ?? 0} values, expected {ExpectedVectorSize}.");
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

        return Result<TaskGenerationPreparationResponseDto>.Success(new TaskGenerationPreparationResponseDto
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
        });
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
        builder.AppendLine("10) validation_criteria must also include checks for each additional objective actually used in additional_skills_required.");
        builder.AppendLine("11) validation_criteria must include business-logic correctness checks for scenario requirements.");
        builder.AppendLine("12) For business-logic validations not tied to one learning objective, set both skill_id and related_learning_objective to 0.");
        builder.AppendLine("13) Every validation entry must include skill_id and related_learning_objective fields.");
        builder.AppendLine("14) Keep validations atomic, objective, and technically verifiable from implementation behavior.");
        builder.AppendLine("15) instructions must guide student step by step without revealing full solution code.");
        builder.AppendLine("16) hints must be progressive from general to specific, still without giving final code.");
        builder.AppendLine("17) Keep output language clear and professional.");
        builder.AppendLine("18) Output MUST be JSON only.");
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
        builder.AppendLine("- validation_criteria MUST:");
        builder.AppendLine("  - Include at least one criterion for EACH targeted_objective.");
        builder.AppendLine("  - Include at least one criterion for EACH objective used in additional_skills_required.");
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
}