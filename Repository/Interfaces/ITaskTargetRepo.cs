using Models;

public interface ITaskTargetRepo
{
    Task<TaskTarget?> GetByIdAsync(Guid taskId, int learningObjectiveId);
    Task<List<TaskTarget>> GetAllAsync();
    Task<List<TaskTarget>> GetByTaskIdAsync(Guid taskId);
    Task AddAsync(TaskTarget taskTarget);
    Task UpdateAsync(TaskTarget taskTarget);
    Task DeleteAsync(Guid taskId, int learningObjectiveId);
}