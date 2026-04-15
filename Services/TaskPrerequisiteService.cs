using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class TaskPrerequisiteService : ITaskPrerequisiteService
{
    private readonly ITaskPrerequisiteRepo _taskPrerequisiteRepo;
    private readonly ITaskItemRepo _taskItemRepo;
    private readonly ILearningObjectiveRepo _learningObjectiveRepo;

    public TaskPrerequisiteService(
        ITaskPrerequisiteRepo taskPrerequisiteRepo,
        ITaskItemRepo taskItemRepo,
        ILearningObjectiveRepo learningObjectiveRepo)
    {
        _taskPrerequisiteRepo = taskPrerequisiteRepo;
        _taskItemRepo = taskItemRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
    }

    public async Task<Result<List<TaskPrerequisiteResponseDto>>> GetAllAsync()
    {
        var items = await _taskPrerequisiteRepo.GetAllAsync();
        return Result<List<TaskPrerequisiteResponseDto>>.Success(items.Select(MapToResponse).ToList());
    }

    public async Task<Result<TaskPrerequisiteResponseDto>> GetByIdAsync(Guid taskId, int learningObjectiveId)
    {
        if (taskId == Guid.Empty)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure("Task id is required.");
        }

        if (learningObjectiveId <= 0)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure("Learning objective id must be greater than zero.");
        }

        var item = await _taskPrerequisiteRepo.GetByIdAsync(taskId, learningObjectiveId);
        if (item is null)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure(
                $"Task prerequisite with task id {taskId} and learning objective id {learningObjectiveId} was not found.");
        }

        return Result<TaskPrerequisiteResponseDto>.Success(MapToResponse(item));
    }

    public async Task<Result<List<TaskPrerequisiteResponseDto>>> GetByTaskIdAsync(Guid taskId)
    {
        if (taskId == Guid.Empty)
        {
            return Result<List<TaskPrerequisiteResponseDto>>.Failure("Task id is required.");
        }

        var taskExists = await _taskItemRepo.GetByIdAsync(taskId);
        if (taskExists is null)
        {
            return Result<List<TaskPrerequisiteResponseDto>>.Failure($"Task with id {taskId} was not found.");
        }

        var items = await _taskPrerequisiteRepo.GetByTaskIdAsync(taskId);
        return Result<List<TaskPrerequisiteResponseDto>>.Success(items.Select(MapToResponse).ToList());
    }

    public async Task<Result<TaskPrerequisiteResponseDto>> CreateAsync(CreateTaskPrerequisiteDto dto)
    {
        if (dto is null)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure("Task prerequisite payload is required.");
        }

        var validationError = await ValidatePayloadAsync(dto.TaskId, dto.LearningObjectiveId, dto.Justification);
        if (validationError is not null)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure(validationError);
        }

        var existing = await _taskPrerequisiteRepo.GetByIdAsync(dto.TaskId, dto.LearningObjectiveId);
        if (existing is not null)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure("Task prerequisite already exists.");
        }

        var entity = new TaskPrerequisite
        {
            TaskId = dto.TaskId,
            LearningObjectiveId = dto.LearningObjectiveId,
            Justification = TextNormalizer.NormalizeRequired(dto.Justification)!
        };

        await _taskPrerequisiteRepo.AddAsync(entity);
        return Result<TaskPrerequisiteResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<TaskPrerequisiteResponseDto>> UpdateAsync(Guid taskId, int learningObjectiveId, UpdateTaskPrerequisiteDto dto)
    {
        if (taskId == Guid.Empty)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure("Task id is required.");
        }

        if (learningObjectiveId <= 0)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure("Learning objective id must be greater than zero.");
        }

        if (dto is null)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure("Task prerequisite payload is required.");
        }

        var existing = await _taskPrerequisiteRepo.GetByIdAsync(taskId, learningObjectiveId);
        if (existing is null)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure(
                $"Task prerequisite with task id {taskId} and learning objective id {learningObjectiveId} was not found.");
        }

        var validationError = await ValidatePayloadAsync(dto.TaskId, dto.LearningObjectiveId, dto.Justification);
        if (validationError is not null)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure(validationError);
        }

        var normalizedJustification = TextNormalizer.NormalizeRequired(dto.Justification)!;
        var isSameCompositeKey = taskId == dto.TaskId && learningObjectiveId == dto.LearningObjectiveId;
        if (isSameCompositeKey)
        {
            existing.Justification = normalizedJustification;
            await _taskPrerequisiteRepo.UpdateAsync(existing);
            return Result<TaskPrerequisiteResponseDto>.Success(MapToResponse(existing));
        }

        var nextExists = await _taskPrerequisiteRepo.GetByIdAsync(dto.TaskId, dto.LearningObjectiveId);
        if (nextExists is not null)
        {
            return Result<TaskPrerequisiteResponseDto>.Failure("Task prerequisite with the new key already exists.");
        }

        await _taskPrerequisiteRepo.DeleteAsync(taskId, learningObjectiveId);

        var entity = new TaskPrerequisite
        {
            TaskId = dto.TaskId,
            LearningObjectiveId = dto.LearningObjectiveId,
            Justification = normalizedJustification
        };

        await _taskPrerequisiteRepo.AddAsync(entity);
        return Result<TaskPrerequisiteResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<bool>> DeleteAsync(Guid taskId, int learningObjectiveId)
    {
        if (taskId == Guid.Empty)
        {
            return Result<bool>.Failure("Task id is required.");
        }

        if (learningObjectiveId <= 0)
        {
            return Result<bool>.Failure("Learning objective id must be greater than zero.");
        }

        var existing = await _taskPrerequisiteRepo.GetByIdAsync(taskId, learningObjectiveId);
        if (existing is null)
        {
            return Result<bool>.Failure(
                $"Task prerequisite with task id {taskId} and learning objective id {learningObjectiveId} was not found.");
        }

        await _taskPrerequisiteRepo.DeleteAsync(taskId, learningObjectiveId);
        return Result<bool>.Success(true);
    }

    private async Task<string?> ValidatePayloadAsync(Guid taskId, int learningObjectiveId, string? justification)
    {
        if (taskId == Guid.Empty)
        {
            return "Task id is required.";
        }

        var taskExists = await _taskItemRepo.GetByIdAsync(taskId);
        if (taskExists is null)
        {
            return $"Task with id {taskId} was not found.";
        }

        if (learningObjectiveId <= 0)
        {
            return "Learning objective id must be greater than zero.";
        }

        var learningObjectiveExists = await _learningObjectiveRepo.GetByIdAsync(learningObjectiveId);
        if (learningObjectiveExists is null)
        {
            return $"Learning objective with id {learningObjectiveId} was not found.";
        }

        var normalizedJustification = TextNormalizer.NormalizeRequired(justification);
        if (normalizedJustification is null)
        {
            return "Task prerequisite justification is required.";
        }

        if (normalizedJustification.Length > 500)
        {
            return "Task prerequisite justification cannot exceed 500 characters.";
        }

        return null;
    }

    private static TaskPrerequisiteResponseDto MapToResponse(TaskPrerequisite prerequisite)
    {
        return new TaskPrerequisiteResponseDto
        {
            TaskId = prerequisite.TaskId,
            LearningObjectiveId = prerequisite.LearningObjectiveId,
            Justification = prerequisite.Justification
        };
    }
}