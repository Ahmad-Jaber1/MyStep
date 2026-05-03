using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class TaskSubmissionEvaluationService : ITaskSubmissionEvaluationService
{
    private readonly IStudentTaskRepo _studentTaskRepo;
    private readonly ITaskItemRepo _taskItemRepo;
    private readonly ILearningObjectiveRepo _learningObjectiveRepo;
    private readonly IGitHubRepositoryCodeService _gitHubRepositoryCodeService;
    private readonly IGenerationClient _generationClient;
    private readonly MyStepDbContext _dbContext;

    public TaskSubmissionEvaluationService(
        IStudentTaskRepo studentTaskRepo,
        ITaskItemRepo taskItemRepo,
        ILearningObjectiveRepo learningObjectiveRepo,
        IGitHubRepositoryCodeService gitHubRepositoryCodeService,
        IGenerationClient generationClient,
        MyStepDbContext dbContext)
    {
        _studentTaskRepo = studentTaskRepo;
        _taskItemRepo = taskItemRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
        _gitHubRepositoryCodeService = gitHubRepositoryCodeService;
        _generationClient = generationClient;
        _dbContext = dbContext;
    }

    public async Task<Result<TaskSubmissionEvaluationResponseDto>> EvaluateSubmissionAsync(Guid studentId, Guid taskId, string repositoryUrl, string? reference = null)
    {
        if (studentId == Guid.Empty || taskId == Guid.Empty)
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure("Student id and task id are required.");
        }

        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure("Repository URL is required.");
        }

        var studentTask = await _studentTaskRepo.GetAsync(studentId, taskId);
        if (studentTask is null)
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure("Student task was not found.");
        }

        var task = await _taskItemRepo.GetByIdAsync(taskId);
        if (task is null)
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure("Task was not found.");
        }

        var validationResult = await BuildValidationContextAsync(task);
        if (!validationResult.IsSuccess || validationResult.Data is null)
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure(validationResult.ErrorMessage ?? "Task validation extraction failed.");
        }

        var flattenResult = await _gitHubRepositoryCodeService.FlattenRepositoryCodeAsync(repositoryUrl, reference);
        if (!flattenResult.IsSuccess || flattenResult.Data is null)
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure(flattenResult.ErrorMessage ?? "Failed to read repository code.");
        }

        var submittedCode = flattenResult.Data.FlattenedCode;
        if (string.IsNullOrWhiteSpace(submittedCode))
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure("Repository code was empty after flattening.");
        }

        var prompt = BuildEvaluationPrompt(task, submittedCode, validationResult.Data.Validations);
        var generationResult = await _generationClient.GenerateContentAsync(prompt);
        if (!generationResult.IsSuccess)
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure(generationResult.ErrorMessage ?? "Submission evaluation failed.");
        }

        var rawModelJson = ExtractJsonObject(generationResult.Data ?? string.Empty);
        if (string.IsNullOrWhiteSpace(rawModelJson))
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure("Evaluation API returned an empty or invalid JSON response.");
        }

        if (!TryParseModelResponse(rawModelJson, out var modelResponse, out var parseError))
        {
            return Result<TaskSubmissionEvaluationResponseDto>.Failure(parseError);
        }

        var processedValidationResults = MapValidationResults(validationResult.Data.Validations, modelResponse.ValidationResults ?? []);
        var evaluationId = Guid.NewGuid();
        var evaluatedAt = DateTime.UtcNow;
        var totalValidations = processedValidationResults.Count;
        var passedValidations = processedValidationResults.Count(item => item.IsPass);
        var passedRatio = totalValidations == 0 ? 0 : (double)passedValidations / totalValidations;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var evaluation = new TaskSubmissionEvaluation
            {
                Id = evaluationId,
                StudentId = studentId,
                TaskId = taskId,
                RepositoryUrl = repositoryUrl.Trim(),
                Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
                OverallSummary = modelResponse.OverallSummary?.Trim() ?? string.Empty,
                RawModelResponseJson = rawModelJson,
                ValidationCount = totalValidations,
                PassedValidationCount = passedValidations,
                FailedValidationCount = totalValidations - passedValidations,
                CreatedAt = evaluatedAt
            };

            foreach (var validation in processedValidationResults)
            {
                evaluation.ValidationResults.Add(new TaskSubmissionEvaluationValidation
                {
                    Id = Guid.NewGuid(),
                    TaskSubmissionEvaluationId = evaluationId,
                    ValidationId = validation.ValidationId,
                    SkillId = validation.SkillId,
                    ObjectiveId = validation.ObjectiveId,
                    ValidationString = validation.ValidationString,
                    IsPass = validation.IsPass,
                    WhyNotPass = validation.WhyNotPass,
                    CreatedAt = evaluatedAt
                });
            }

            await _dbContext.TaskSubmissionEvaluations.AddAsync(evaluation);

            foreach (var validation in processedValidationResults)
            {
                await ApplyObjectiveOutcomeAsync(studentId, validation, evaluatedAt);
            }

            if (!studentTask.Passed && passedRatio >= 0.75)
            {
                studentTask.Passed = true;
                studentTask.CompletedAt = evaluatedAt;
                studentTask.Score = passedRatio * 100d;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = new TaskSubmissionEvaluationResponseDto
            {
                EvaluationId = evaluationId,
                StudentId = studentId,
                TaskId = taskId,
                EvaluatedAt = evaluatedAt,
                OverallSummary = evaluation.OverallSummary,
                ValidationResults = processedValidationResults.Select(MapToResponse).ToList(),
                StudentGoodPoints = NormalizeStringList(modelResponse.StudentGoodPoints),
                StudentWeaknesses = NormalizeStringList(modelResponse.StudentWeaknesses),
                TopicsToRead = NormalizeStringList(modelResponse.TopicsToRead)
            };

            return Result<TaskSubmissionEvaluationResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<TaskSubmissionEvaluationResponseDto>.Failure($"Failed to persist evaluation results: {ex.Message}");
        }
    }

    private async Task<Result<EvaluationPreparationContext>> BuildValidationContextAsync(TaskItem task)
    {
        if (task.TaskData is null)
        {
            return Result<EvaluationPreparationContext>.Failure("Task data is missing.");
        }

        var objectives = await _learningObjectiveRepo.GetAllAsync();
        var objectiveById = objectives.ToDictionary(objective => objective.Id);

        var root = task.TaskData.RootElement;
        if (!TryBuildObjectiveScopeContext(root, out var scopeContext))
        {
            return Result<EvaluationPreparationContext>.Failure("Task does not contain the required target and prerequisite objective lists.");
        }

        if (!TryGetValidationArray(root, out var validationElements))
        {
            return Result<EvaluationPreparationContext>.Failure("Task does not contain validation criteria.");
        }

        var validations = new List<ValidationPromptItem>();
        var validationId = 1;
        foreach (var element in validationElements)
        {
            if (!TryReadInt(element, out var skillId, "skill_id", "skillId") ||
                !TryReadInt(element, out var objectiveId, "related_learning_objective", "objective_id", "learning_objective_id") ||
                !TryReadString(element, out var validationString, "validation_string", "criterion", "validation", "description"))
            {
                return Result<EvaluationPreparationContext>.Failure("One or more validation criteria are missing required fields.");
            }

            var skillName = skillId == 0
                ? "Business logic"
                : objectiveById.TryGetValue(objectiveId, out var objective) && objective.SkillId == skillId
                    ? objective.Skill?.Name ?? $"Skill {skillId}"
                    : $"Skill {skillId}";

            var objectiveDescription = objectiveId == 0
                ? "Business logic validation not tied to a specific learning objective."
                : objectiveById.TryGetValue(objectiveId, out var matchedObjective)
                    ? matchedObjective.Description?.Trim() ?? string.Empty
                    : string.Empty;

            validations.Add(new ValidationPromptItem(
                validationId++,
                skillId,
                skillName,
                objectiveId,
                objectiveDescription,
                validationString.Trim(),
                DetermineObjectiveType(objectiveId, scopeContext)));
        }

        if (validations.Count == 0)
        {
            return Result<EvaluationPreparationContext>.Failure("Task does not contain any validation criteria.");
        }

        return Result<EvaluationPreparationContext>.Success(new EvaluationPreparationContext(scopeContext, validations));
    }

    private static string BuildEvaluationPrompt(TaskItem task, string submittedCode, IReadOnlyCollection<ValidationPromptItem> validations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are grading a student's submitted solution for a programming task.");
        builder.AppendLine("Read the task carefully, read the submitted code carefully, and evaluate each validation independently.");
        builder.AppendLine("Use only the provided task, submitted code, and validations. Do not invent extra requirements.");
        builder.AppendLine("Return only valid JSON. No markdown. No explanation text outside JSON.");
        builder.AppendLine();

        builder.AppendLine("TASK JSON:");
        builder.AppendLine("<<<TASK_JSON");
        builder.AppendLine(task.TaskData.RootElement.GetRawText());
        builder.AppendLine("TASK_JSON>>>");
        builder.AppendLine();

        builder.AppendLine("VALIDATIONS WITH OBJECTIVE CONTEXT:");
        builder.AppendLine("<<<VALIDATIONS_JSON");
        builder.AppendLine(JsonSerializer.Serialize(validations, new JsonSerializerOptions { WriteIndented = true }));
        builder.AppendLine("VALIDATIONS_JSON>>>");
        builder.AppendLine();

        builder.AppendLine("SUBMITTED CODE:");
        builder.AppendLine("<<<SUBMITTED_CODE");
        builder.AppendLine(submittedCode);
        builder.AppendLine("SUBMITTED_CODE>>>");
        builder.AppendLine();

        builder.AppendLine("EVALUATION RULES:");
        builder.AppendLine("- For each validation, decide if the submitted code passes it.");
        builder.AppendLine("- If a validation is tied to skill_id 0 and objective_id 0, treat it as a business-logic check based on the task requirements.");
        builder.AppendLine("- If the code clearly satisfies the validation but contains a small syntax slip, typo, missing semicolon, missing quote, or similar obvious compile-time mistake, do not fail the validation just for that reason.");
        builder.AppendLine("- Prefer the student's intended implementation and overall logic when the intent is clear.");
        builder.AppendLine("- Only mark a validation as failed when the logic, structure, or behavior does not satisfy the requirement.");
        builder.AppendLine("- If the code passes, set is_pass to true and keep why_not_pass empty.");
        builder.AppendLine("- If the code is ambiguous, incomplete, or the syntax issue changes the intended behavior, do not mark the validation as passed.");
        builder.AppendLine("- Also summarize what the student did well, what is weak or strange, and what specific concepts they should read next to improve those weaknesses.");
        builder.AppendLine("- For topics_to_read, return short concrete concept names, such as inheritance, dependency injection, interface design, async/await, LINQ, exception handling, generics, collections, SOLID principles, or unit testing, based on the actual weakness in the code.");
        builder.AppendLine();

        builder.AppendLine("REQUIRED JSON RESPONSE SHAPE:");
        builder.AppendLine("{");
        builder.AppendLine("  \"overall_summary\": \"Short summary of the result.\",");
        builder.AppendLine("  \"validation_results\": [");
        builder.AppendLine("    {");
        builder.AppendLine("      \"validation_id\": 1,");
        builder.AppendLine("      \"skill_id\": 0,");
        builder.AppendLine("      \"objective_id\": 0,");
        builder.AppendLine("      \"is_pass\": false,");
        builder.AppendLine("      \"why_not_pass\": \"Short reason when false, otherwise empty string.\" ");
        builder.AppendLine("    }");
        builder.AppendLine("  ],");
        builder.AppendLine("  \"student_good_points\": [\"...\"],");
        builder.AppendLine("  \"student_weaknesses\": [\"...\"],");
        builder.AppendLine("  \"topics_to_read\": [\"...\"]");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("FINAL INSTRUCTION: return only one JSON object matching the required shape.");

        return builder.ToString();
    }

    private async Task ApplyObjectiveOutcomeAsync(Guid studentId, ProcessedValidationResult validation, DateTime evaluatedAt)
    {
        if (validation.ObjectiveType is ObjectiveUpdateType.None || validation.ObjectiveId == 0)
        {
            return;
        }

        var objective = await _dbContext.StudentLearningObjectives.FindAsync(studentId, validation.ObjectiveId);
        if (objective is null)
        {
            objective = new StudentLearningObjective
            {
                StudentId = studentId,
                LearningObjectiveId = validation.ObjectiveId,
                Score = 0,
                StreakCount = 1,
                LastUpdated = evaluatedAt
            };

            await _dbContext.StudentLearningObjectives.AddAsync(objective);
        }

        var scoreStep = validation.ObjectiveType == ObjectiveUpdateType.Prerequisite ? 0.05 : 0.1;
        var streakStep = validation.ObjectiveType == ObjectiveUpdateType.Prerequisite ? 0.5 : 1d;

        if (validation.IsPass)
        {
            objective.Score = Math.Min(1d, objective.Score + (scoreStep * objective.StreakCount));
            objective.StreakCount = Math.Max(1d, objective.StreakCount + streakStep);
        }
        else
        {
            objective.Score = Math.Max(0d, objective.Score - scoreStep);
            objective.StreakCount = Math.Max(1d, objective.StreakCount - streakStep);
        }

        objective.LastUpdated = evaluatedAt;
    }

    private static List<ProcessedValidationResult> MapValidationResults(
        IReadOnlyCollection<ValidationPromptItem> expectedValidations,
        IReadOnlyCollection<ModelValidationResult> modelResults)
    {
        var modelById = modelResults.ToDictionary(item => item.ValidationId);
        var results = new List<ProcessedValidationResult>();

        foreach (var expected in expectedValidations)
        {
            if (!modelById.TryGetValue(expected.ValidationId, out var modelResult))
            {
                results.Add(new ProcessedValidationResult(
                    expected.ValidationId,
                    expected.SkillId,
                    expected.ObjectiveId,
                    expected.ValidationString,
                    false,
                    "The model did not return a result for this validation.",
                    expected.ObjectiveType));
                continue;
            }

            results.Add(new ProcessedValidationResult(
                expected.ValidationId,
                modelResult.SkillId,
                modelResult.ObjectiveId,
                expected.ValidationString,
                modelResult.IsPass,
                modelResult.WhyNotPass?.Trim() ?? string.Empty,
                expected.ObjectiveType));
        }

        return results.OrderBy(item => item.ValidationId).ToList();
    }

    private static TaskValidationEvaluationDto MapToResponse(ProcessedValidationResult validation)
    {
        return new TaskValidationEvaluationDto
        {
            ValidationId = validation.ValidationId,
            SkillId = validation.SkillId,
            ObjectiveId = validation.ObjectiveId,
            ValidationString = validation.ValidationString,
            IsPass = validation.IsPass,
            WhyNotPass = validation.WhyNotPass
        };
    }

    private static List<string> NormalizeStringList(IEnumerable<string>? values)
    {
        return values?
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private static bool TryBuildObjectiveScopeContext(JsonElement root, out ObjectiveScopeContext context)
    {
        context = new ObjectiveScopeContext([], []);

        if (!TryReadObjectiveIdSet(root, "targeted_objectives", out var targetObjectiveIds))
        {
            return false;
        }

        if (!TryReadPrerequisiteObjectiveIdSet(root, out var prerequisiteObjectiveIds))
        {
            return false;
        }

        if (targetObjectiveIds.Count == 0)
        {
            return false;
        }

        context = new ObjectiveScopeContext(targetObjectiveIds, prerequisiteObjectiveIds);
        return true;
    }

    private static bool TryReadObjectiveIdSet(JsonElement root, string propertyName, out HashSet<int> objectiveIds)
    {
        objectiveIds = [];

        if (!root.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var element in property.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
            {
                objectiveIds.Add(value);
                continue;
            }

            if (element.ValueKind == JsonValueKind.Object && TryReadInt(element, out var objectId, "id", "objective_id", "learning_objective_id"))
            {
                objectiveIds.Add(objectId);
            }
        }

        return true;
    }

    private static bool TryReadPrerequisiteObjectiveIdSet(JsonElement root, out HashSet<int> objectiveIds)
    {
        objectiveIds = [];

        if (!root.TryGetProperty("additional_skills_required", out var property))
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var element in property.EnumerateArray())
        {
            if (TryReadInt(element, out var objectiveId, "used_learning_goal", "learning_goal_id", "objective_id", "related_learning_objective"))
            {
                objectiveIds.Add(objectiveId);
            }
        }

        return true;
    }

    private static ObjectiveUpdateType DetermineObjectiveType(int objectiveId, ObjectiveScopeContext scopeContext)
    {
        if (objectiveId == 0)
        {
            return ObjectiveUpdateType.None;
        }

        if (scopeContext.TargetObjectiveIds.Contains(objectiveId))
        {
            return ObjectiveUpdateType.Target;
        }

        if (scopeContext.PrerequisiteObjectiveIds.Contains(objectiveId))
        {
            return ObjectiveUpdateType.Prerequisite;
        }

        return ObjectiveUpdateType.None;
    }

    private static bool TryGetValidationArray(JsonElement root, out List<JsonElement> validations)
    {
        validations = [];

        if (!root.TryGetProperty("validation_criteria", out var criteriaElement))
        {
            return false;
        }

        if (criteriaElement.ValueKind == JsonValueKind.Array)
        {
            validations = criteriaElement.EnumerateArray().ToList();
            return true;
        }

        if (criteriaElement.ValueKind == JsonValueKind.Object)
        {
            validations = [criteriaElement];
            return true;
        }

        return false;
    }

    private static bool TryReadInt(JsonElement element, out int value, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value))
                {
                    return true;
                }

                if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value))
                {
                    return true;
                }
            }
        }

        value = 0;
        return false;
    }

    private static bool TryReadString(JsonElement element, out string value, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
            {
                value = property.GetString() ?? string.Empty;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private sealed record EvaluationPreparationContext(ObjectiveScopeContext ScopeContext, List<ValidationPromptItem> Validations);

    private sealed record ObjectiveScopeContext(HashSet<int> TargetObjectiveIds, HashSet<int> PrerequisiteObjectiveIds);

    private sealed record ValidationPromptItem(
        int ValidationId,
        int SkillId,
        string SkillName,
        int ObjectiveId,
        string ObjectiveDescription,
        string ValidationString,
        ObjectiveUpdateType ObjectiveType);

    private sealed record ProcessedValidationResult(
        int ValidationId,
        int SkillId,
        int ObjectiveId,
        string ValidationString,
        bool IsPass,
        string WhyNotPass,
        ObjectiveUpdateType ObjectiveType);

    private enum ObjectiveUpdateType
    {
        None = 0,
        Target = 1,
        Prerequisite = 2
    }

    private sealed class ModelEvaluationResponse
    {
        [JsonPropertyName("overall_summary")]
        public string? OverallSummary { get; set; }

        [JsonPropertyName("validation_results")]
        public List<ModelValidationResult>? ValidationResults { get; set; }

        [JsonPropertyName("student_good_points")]
        public List<string>? StudentGoodPoints { get; set; }

        [JsonPropertyName("student_weaknesses")]
        public List<string>? StudentWeaknesses { get; set; }

        [JsonPropertyName("topics_to_read")]
        public List<string>? TopicsToRead { get; set; }
    }

    private sealed class ModelValidationResult
    {
        [JsonPropertyName("validation_id")]
        public int ValidationId { get; set; }

        [JsonPropertyName("skill_id")]
        public int SkillId { get; set; }

        [JsonPropertyName("objective_id")]
        public int ObjectiveId { get; set; }

        [JsonPropertyName("is_pass")]
        public bool IsPass { get; set; }

        [JsonPropertyName("why_not_pass")]
        public string? WhyNotPass { get; set; }
    }

    private static bool TryParseModelResponse(string json, out ModelEvaluationResponse response, out string errorMessage)
    {
        response = null!;
        errorMessage = string.Empty;

        try
        {
            response = JsonSerializer.Deserialize<ModelEvaluationResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new JsonException("Deserialized response was null.");

            response.ValidationResults ??= [];
            response.StudentGoodPoints ??= [];
            response.StudentWeaknesses ??= [];
            response.TopicsToRead ??= [];

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to parse evaluation response: {ex.Message}";
            return false;
        }
    }

    private static string ExtractJsonObject(string rawContent)
    {
        var trimmed = rawContent.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            if (firstNewLine >= 0)
            {
                trimmed = trimmed[(firstNewLine + 1)..].Trim();
            }

            if (trimmed.EndsWith("```", StringComparison.Ordinal))
            {
                trimmed = trimmed[..^3].Trim();
            }
        }

        var startIndex = trimmed.IndexOf('{');
        var endIndex = trimmed.LastIndexOf('}');
        if (startIndex < 0 || endIndex <= startIndex)
        {
            return string.Empty;
        }

        return trimmed[startIndex..(endIndex + 1)];
    }
}
