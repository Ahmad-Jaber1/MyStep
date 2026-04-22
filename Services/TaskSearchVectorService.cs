using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

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

    public TaskSearchVectorService(
        ITaskItemRepo taskItemRepo,
        ILearningObjectiveRepo learningObjectiveRepo,
        IStudentLearningObjectiveRepo studentLearningObjectiveRepo,
        ISkillRepo skillRepo,
        IEmbeddingClient embeddingClient)
    {
        _taskItemRepo = taskItemRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
        _studentLearningObjectiveRepo = studentLearningObjectiveRepo;
        _skillRepo = skillRepo;
        _embeddingClient = embeddingClient;
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
        var objectiveById = learningObjectives.ToDictionary(objective => objective.Id);

        var studentObjectives = await _studentLearningObjectiveRepo.GetByStudentIdAsync(studentId);

        var prerequisiteDescriptions = new List<string>();
        var targetDescriptions = new List<string>();

        foreach (var studentObjective in studentObjectives)
        {
            if (!objectiveById.TryGetValue(studentObjective.LearningObjectiveId, out var learningObjective))
            {
                continue;
            }

            var description = learningObjective.Description?.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            if (learningObjective.SkillId == mainSkillId && studentObjective.Score < MasteryThreshold)
            {
                targetDescriptions.Add(description);
                continue;
            }

            if (learningObjective.SkillId != mainSkillId
                && pathSkillIds.Contains(learningObjective.SkillId)
                && studentObjective.Score >= MasteryThreshold)
            {
                prerequisiteDescriptions.Add(description);
            }
        }

        var normalizedPrerequisites = prerequisiteDescriptions
            .Distinct(StringComparer.Ordinal)
            .OrderBy(description => description, StringComparer.Ordinal)
            .ToList();

        var normalizedTargets = targetDescriptions
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

        var taskMatches = similarTasks
            .Select(task =>
            {
                var similarityScore = ComputeCosineSimilarity(queryVector, task.SearchVector.ToArray());
                return new StudentTaskMatchResponseDto
                {
                    TaskId = task.Id,
                    PathId = task.PathId,
                    MainSkillId = task.MainSkillId,
                    TaskData = task.TaskData,
                    SimilarityScore = similarityScore
                };
            })
            .ToList();

        return Result<TaskGenerationPreparationResponseDto>.Success(new TaskGenerationPreparationResponseDto
        {
            StudentId = studentId,
            MainSkillId = mainSkillId,
            MasteryThreshold = MasteryThreshold,
            PrerequisiteLearningObjectives = normalizedPrerequisites,
            TargetLearningObjectives = normalizedTargets,
            InputText = inputText,
            QueryVector = queryVector,
            TopTaskMatches = taskMatches
        });
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
}