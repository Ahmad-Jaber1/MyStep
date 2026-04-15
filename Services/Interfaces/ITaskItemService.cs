using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskItemService
{
    Task<Result<List<TaskItemResponseDto>>> GetAllAsync();
    Task<Result<TaskItemResponseDto>> GetByIdAsync(Guid id);
    Task<Result<List<TaskItemResponseDto>>> GetByPathIdAsync(int pathId);
    Task<Result<List<TaskItemResponseDto>>> GetByMainSkillIdAsync(int mainSkillId);
    Task<Result<TaskItemResponseDto>> CreateAsync(CreateTaskItemDto dto);
    Task<Result<TaskItemResponseDto>> UpdateAsync(Guid id, UpdateTaskItemDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}