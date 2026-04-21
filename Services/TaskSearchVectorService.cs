using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class TaskSearchVectorService : ITaskSearchVectorService
{
    private const int ExpectedVectorSize = 4096;

    private readonly ITaskItemRepo _taskItemRepo;
    private readonly ILearningObjectiveRepo _learningObjectiveRepo;
    private readonly IEmbeddingClient _embeddingClient;

    public TaskSearchVectorService(
        ITaskItemRepo taskItemRepo,
        ILearningObjectiveRepo learningObjectiveRepo,
        IEmbeddingClient embeddingClient)
    {
        _taskItemRepo = taskItemRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
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
}