using Models;

public interface ITaskGenerationRequestRepo
{
    Task<TaskGenerationRequest?> GetByIdAsync(Guid id);
    Task<TaskGenerationRequest?> GetPendingByStudentAndSkillAsync(Guid studentId, int mainSkillId);
    Task<List<TaskGenerationRequest>> GetBySupervisorAsync(Guid supervisorId);
    Task AddAsync(TaskGenerationRequest request);
    Task UpdateAsync(TaskGenerationRequest request);
}
