using Models;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class TaskTargetService : ITaskTargetService
{
    private readonly ITaskTargetRepo _taskTargetRepo;
    private readonly ITaskItemRepo _taskItemRepo;
    private readonly ILearningObjectiveRepo _learningObjectiveRepo;

    public TaskTargetService(
        ITaskTargetRepo taskTargetRepo,
        ITaskItemRepo taskItemRepo,
        ILearningObjectiveRepo learningObjectiveRepo)
    {
        _taskTargetRepo = taskTargetRepo;
        _taskItemRepo = taskItemRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
    }

    public async Task<Result<List<TaskTargetResponseDto>>> GetAllAsync()
    {
        var items = await _taskTargetRepo.GetAllAsync();
        return Result<List<TaskTargetResponseDto>>.Success(items.Select(MapToResponse).ToList());
    }

    public async Task<Result<TaskTargetResponseDto>> GetByIdAsync(Guid taskId, int learningObjectiveId)
    {
        if (taskId == Guid.Empty)
        {
            return Result<TaskTargetResponseDto>.Failure("Task id is required.");
        }

        if (learningObjectiveId <= 0)
        {
            return Result<TaskTargetResponseDto>.Failure("Learning objective id must be greater than zero.");
        }

        var item = await _taskTargetRepo.GetByIdAsync(taskId, learningObjectiveId);
        if (item is null)
        {
            return Result<TaskTargetResponseDto>.Failure(
                $"Task target with task id {taskId} and learning objective id {learningObjectiveId} was not found.");
        }

        return Result<TaskTargetResponseDto>.Success(MapToResponse(item));
    }

    public async Task<Result<List<TaskTargetResponseDto>>> GetByTaskIdAsync(Guid taskId)
    {
        if (taskId == Guid.Empty)
        {
            return Result<List<TaskTargetResponseDto>>.Failure("Task id is required.");
        }

        var taskExists = await _taskItemRepo.GetByIdAsync(taskId);
        if (taskExists is null)
        {
            return Result<List<TaskTargetResponseDto>>.Failure($"Task with id {taskId} was not found.");
        }

        var items = await _taskTargetRepo.GetByTaskIdAsync(taskId);
        return Result<List<TaskTargetResponseDto>>.Success(items.Select(MapToResponse).ToList());
    }

    public async Task<Result<TaskTargetResponseDto>> CreateAsync(CreateTaskTargetDto dto)
    {
        if (dto is null)
        {
            return Result<TaskTargetResponseDto>.Failure("Task target payload is required.");
        }

        var validationError = await ValidatePayloadAsync(dto.TaskId, dto.LearningObjectiveId);
        if (validationError is not null)
        {
            return Result<TaskTargetResponseDto>.Failure(validationError);
        }

        var existing = await _taskTargetRepo.GetByIdAsync(dto.TaskId, dto.LearningObjectiveId);
        if (existing is not null)
        {
            return Result<TaskTargetResponseDto>.Failure("Task target already exists.");
        }

        var entity = new TaskTarget
        {
            TaskId = dto.TaskId,
            LearningObjectiveId = dto.LearningObjectiveId
        };

        await _taskTargetRepo.AddAsync(entity);
        return Result<TaskTargetResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<TaskTargetResponseDto>> UpdateAsync(Guid taskId, int learningObjectiveId, UpdateTaskTargetDto dto)
    {
        if (taskId == Guid.Empty)
        {
            return Result<TaskTargetResponseDto>.Failure("Task id is required.");
        }

        if (learningObjectiveId <= 0)
        {
            return Result<TaskTargetResponseDto>.Failure("Learning objective id must be greater than zero.");
        }

        if (dto is null)
        {
            return Result<TaskTargetResponseDto>.Failure("Task target payload is required.");
        }

        var existing = await _taskTargetRepo.GetByIdAsync(taskId, learningObjectiveId);
        if (existing is null)
        {
            return Result<TaskTargetResponseDto>.Failure(
                $"Task target with task id {taskId} and learning objective id {learningObjectiveId} was not found.");
        }

        var validationError = await ValidatePayloadAsync(dto.TaskId, dto.LearningObjectiveId);
        if (validationError is not null)
        {
            return Result<TaskTargetResponseDto>.Failure(validationError);
        }

        var isSameCompositeKey = taskId == dto.TaskId && learningObjectiveId == dto.LearningObjectiveId;
        if (isSameCompositeKey)
        {
            return Result<TaskTargetResponseDto>.Success(MapToResponse(existing));
        }

        var nextExists = await _taskTargetRepo.GetByIdAsync(dto.TaskId, dto.LearningObjectiveId);
        if (nextExists is not null)
        {
            return Result<TaskTargetResponseDto>.Failure("Task target with the new key already exists.");
        }

        await _taskTargetRepo.DeleteAsync(taskId, learningObjectiveId);

        var entity = new TaskTarget
        {
            TaskId = dto.TaskId,
            LearningObjectiveId = dto.LearningObjectiveId
        };

        await _taskTargetRepo.AddAsync(entity);
        return Result<TaskTargetResponseDto>.Success(MapToResponse(entity));
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

        var existing = await _taskTargetRepo.GetByIdAsync(taskId, learningObjectiveId);
        if (existing is null)
        {
            return Result<bool>.Failure(
                $"Task target with task id {taskId} and learning objective id {learningObjectiveId} was not found.");
        }

        await _taskTargetRepo.DeleteAsync(taskId, learningObjectiveId);
        return Result<bool>.Success(true);
    }

    private async Task<string?> ValidatePayloadAsync(Guid taskId, int learningObjectiveId)
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

        return null;
    }

    private static TaskTargetResponseDto MapToResponse(TaskTarget target)
    {
        return new TaskTargetResponseDto
        {
            TaskId = target.TaskId,
            LearningObjectiveId = target.LearningObjectiveId
        };
    }
}