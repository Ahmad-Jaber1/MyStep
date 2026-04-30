using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ITaskSubmissionEvaluationService
{
    Task<Result<TaskSubmissionEvaluationResponseDto>> EvaluateSubmissionAsync(Guid studentId, Guid taskId, string repositoryUrl, string? reference = null);
}
