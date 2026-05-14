using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskEditService
{
    Task<Result<TaskEditResponseDto>> EditObjectivesAsync(Guid supervisorId, Guid taskId, EditTaskObjectivesDto dto);
    Task<Result<TaskEditResponseDto>> EditPrerequisitesAsync(Guid supervisorId, Guid taskId, EditTaskPrerequisitesDto dto);
    Task<Result<TaskEditResponseDto>> EditValidationsAsync(Guid supervisorId, Guid taskId, EditTaskValidationsDto dto);
}
