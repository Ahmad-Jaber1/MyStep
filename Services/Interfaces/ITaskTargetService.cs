using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskTargetService
{
    Task<Result<List<TaskTargetResponseDto>>> GetAllAsync();
    Task<Result<TaskTargetResponseDto>> GetByIdAsync(Guid taskId, int learningObjectiveId);
    Task<Result<List<TaskTargetResponseDto>>> GetByTaskIdAsync(Guid taskId);
    Task<Result<TaskTargetResponseDto>> CreateAsync(CreateTaskTargetDto dto);
    Task<Result<TaskTargetResponseDto>> UpdateAsync(Guid taskId, int learningObjectiveId, UpdateTaskTargetDto dto);
    Task<Result<bool>> DeleteAsync(Guid taskId, int learningObjectiveId);
}