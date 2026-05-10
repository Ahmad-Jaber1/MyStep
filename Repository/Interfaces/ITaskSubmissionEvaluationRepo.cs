using Models;

public interface ITaskSubmissionEvaluationRepo
{
    Task<TaskSubmissionEvaluation?> GetByIdAsync(Guid evaluationId);
    Task<TaskSubmissionEvaluation?> GetLatestByStudentAndTaskAsync(Guid studentId, Guid taskId);
    Task<List<TaskSubmissionEvaluation>> GetByStudentAndTaskAsync(Guid studentId, Guid taskId);
    Task<List<TaskSubmissionEvaluation>> GetByStudentAsync(Guid studentId);
    Task AddAsync(TaskSubmissionEvaluation evaluation);
    Task UpdateAsync(TaskSubmissionEvaluation evaluation);
    Task DeleteAsync(Guid evaluationId);
}
