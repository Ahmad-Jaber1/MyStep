using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskPrerequisiteService
{
    Task<Result<List<TaskPrerequisiteResponseDto>>> GetAllAsync();
    Task<Result<TaskPrerequisiteResponseDto>> GetByIdAsync(Guid taskId, int learningObjectiveId);
    Task<Result<List<TaskPrerequisiteResponseDto>>> GetByTaskIdAsync(Guid taskId);
    Task<Result<TaskPrerequisiteResponseDto>> CreateAsync(CreateTaskPrerequisiteDto dto);
    Task<Result<TaskPrerequisiteResponseDto>> UpdateAsync(Guid taskId, int learningObjectiveId, UpdateTaskPrerequisiteDto dto);
    Task<Result<bool>> DeleteAsync(Guid taskId, int learningObjectiveId);
}