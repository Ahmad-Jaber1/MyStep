using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskGenerationRequestService
{
    Task<Result<TaskGenerationRequestResponseDto>> CreateAsync(Guid studentId, CreateTaskGenerationRequestDto dto);
    Task<Result<List<TaskGenerationRequestResponseDto>>> GetBySupervisorAsync(Guid supervisorId);
    Task<Result<TaskGenerationRequestResponseDto>> ApproveAsync(Guid supervisorId, Guid requestId);
    Task<Result<TaskGenerationRequestResponseDto>> RejectAsync(Guid supervisorId, Guid requestId);

    // Return a generated preview JSON for a pending request (supervisor review, no persistence).
    Task<Result<GenerateTaskResponseDto>> GeneratePreviewForRequestAsync(Guid supervisorId, Guid requestId);

    // Approve a request and persist the provided generated task content into the DB.
    Task<Result<TaskGenerationRequestResponseDto>> ApproveAndPersistAsync(Guid supervisorId, Guid requestId, string generatedContent);

    // Approve a request by generating and persisting the task in one server-side call.
    Task<Result<ApproveAndGenerateTaskResponseDto>> ApproveAndGenerateAsync(Guid supervisorId, Guid requestId);
}
