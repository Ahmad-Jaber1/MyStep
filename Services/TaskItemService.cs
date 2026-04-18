using System.Text.Json;
using Models;
using Pgvector;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class TaskItemService : ITaskItemService
{
    private readonly ITaskItemRepo _taskItemRepo;
    private readonly IPathItemRepo _pathItemRepo;
    private readonly ISkillRepo _skillRepo;

    public TaskItemService(ITaskItemRepo taskItemRepo, IPathItemRepo pathItemRepo, ISkillRepo skillRepo)
    {
        _taskItemRepo = taskItemRepo;
        _pathItemRepo = pathItemRepo;
        _skillRepo = skillRepo;
    }

    public async Task<Result<List<TaskItemResponseDto>>> GetAllAsync()
    {
        var tasks = await _taskItemRepo.GetAllAsync();
        return Result<List<TaskItemResponseDto>>.Success(tasks.Select(MapToResponse).ToList());
    }

    public async Task<Result<TaskItemResponseDto>> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return Result<TaskItemResponseDto>.Failure("Task id is required.");
        }

        var task = await _taskItemRepo.GetByIdAsync(id);
        if (task is null)
        {
            return Result<TaskItemResponseDto>.Failure($"Task with id {id} was not found.");
        }

        return Result<TaskItemResponseDto>.Success(MapToResponse(task));
    }

    public async Task<Result<List<TaskItemResponseDto>>> GetByPathIdAsync(int pathId)
    {
        if (pathId <= 0)
        {
            return Result<List<TaskItemResponseDto>>.Failure("Path id must be greater than zero.");
        }

        var pathExists = await _pathItemRepo.GetByIdAsync(pathId);
        if (pathExists is null)
        {
            return Result<List<TaskItemResponseDto>>.Failure($"Path with id {pathId} was not found.");
        }

        var tasks = await _taskItemRepo.GetByPathIdAsync(pathId);
        return Result<List<TaskItemResponseDto>>.Success(tasks.Select(MapToResponse).ToList());
    }

    public async Task<Result<List<TaskItemResponseDto>>> GetByMainSkillIdAsync(int mainSkillId)
    {
        if (mainSkillId <= 0)
        {
            return Result<List<TaskItemResponseDto>>.Failure("Main skill id must be greater than zero.");
        }

        var skillExists = await _skillRepo.GetByIdAsync(mainSkillId);
        if (skillExists is null)
        {
            return Result<List<TaskItemResponseDto>>.Failure($"Skill with id {mainSkillId} was not found.");
        }

        var tasks = await _taskItemRepo.GetByMainSkillIdAsync(mainSkillId);
        return Result<List<TaskItemResponseDto>>.Success(tasks.Select(MapToResponse).ToList());
    }

    public async Task<Result<TaskItemResponseDto>> CreateAsync(CreateTaskItemDto dto)
    {
        if (dto is null)
        {
            return Result<TaskItemResponseDto>.Failure("Task payload is required.");
        }

        var validationError = await ValidateTaskPayloadAsync(dto.PathId, dto.MainSkillId, dto.TaskData, dto.SearchVector);
        if (validationError is not null)
        {
            return Result<TaskItemResponseDto>.Failure(validationError);
        }

        var entity = new TaskItem
        {
            Id = Guid.NewGuid(),
            PathId = dto.PathId,
            MainSkillId = dto.MainSkillId,
            TaskData = dto.TaskData!,
            SearchVector = new Vector(dto.SearchVector!)
        };

        await _taskItemRepo.AddAsync(entity);
        return Result<TaskItemResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<TaskItemResponseDto>> UpdateAsync(Guid id, UpdateTaskItemDto dto)
    {
        if (id == Guid.Empty)
        {
            return Result<TaskItemResponseDto>.Failure("Task id is required.");
        }

        if (dto is null)
        {
            return Result<TaskItemResponseDto>.Failure("Task payload is required.");
        }

        var existingTask = await _taskItemRepo.GetByIdAsync(id);
        if (existingTask is null)
        {
            return Result<TaskItemResponseDto>.Failure($"Task with id {id} was not found.");
        }

        var validationError = await ValidateTaskPayloadAsync(dto.PathId, dto.MainSkillId, dto.TaskData, dto.SearchVector);
        if (validationError is not null)
        {
            return Result<TaskItemResponseDto>.Failure(validationError);
        }

        existingTask.PathId = dto.PathId;
        existingTask.MainSkillId = dto.MainSkillId;
        existingTask.TaskData = dto.TaskData!;
        existingTask.SearchVector = new Vector(dto.SearchVector!);

        await _taskItemRepo.UpdateAsync(existingTask);
        return Result<TaskItemResponseDto>.Success(MapToResponse(existingTask));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return Result<bool>.Failure("Task id is required.");
        }

        var existingTask = await _taskItemRepo.GetByIdAsync(id);
        if (existingTask is null)
        {
            return Result<bool>.Failure($"Task with id {id} was not found.");
        }

        await _taskItemRepo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    private async Task<string?> ValidateTaskPayloadAsync(int pathId, int mainSkillId, JsonDocument? taskData, float[]? searchVector)
    {
        if (pathId <= 0)
        {
            return "Task path id must be greater than zero.";
        }

        var pathExists = await _pathItemRepo.GetByIdAsync(pathId);
        if (pathExists is null)
        {
            return $"Path with id {pathId} was not found.";
        }

        if (mainSkillId <= 0)
        {
            return "Task main skill id must be greater than zero.";
        }

        var skillExists = await _skillRepo.GetByIdAsync(mainSkillId);
        if (skillExists is null)
        {
            return $"Skill with id {mainSkillId} was not found.";
        }

        if (taskData is null)
        {
            return "Task data is required.";
        }

        if (searchVector is null || searchVector.Length == 0)
        {
            return "Task search vector is required.";
        }

        //if (searchVector.Length != 4096)
        //{
          //  return "Task search vector must contain exactly 4096 values.";
        //}

        return null;
    }

    private static TaskItemResponseDto MapToResponse(TaskItem task)
    {
        return new TaskItemResponseDto
        {
            Id = task.Id,
            PathId = task.PathId,
            MainSkillId = task.MainSkillId,
            TaskData = task.TaskData,
            SearchVector = task.SearchVector.ToArray()
        };
    }
}