using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ISupervisorService
{
    Task<Result<SupervisorStudentResponseDto>> AddStudentAsync(Guid supervisorId, AddSupervisorStudentDto dto);
    Task<Result<List<SupervisorStudentResponseDto>>> GetMyStudentsAsync(Guid supervisorId);
    Task<Result<List<SupervisorStudentResponseDto>>> GetPendingRequestsAsync(Guid studentId);
    Task<Result<SupervisorStudentResponseDto>> ApproveAsync(Guid studentId, Guid supervisorId, int pathId);
    Task<Result<SupervisorStudentResponseDto>> RejectAsync(Guid studentId, Guid supervisorId, int pathId);
}
