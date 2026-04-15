using Models;

public interface ITaskPrerequisiteRepo
{
    Task<TaskPrerequisite?> GetByIdAsync(Guid taskId, int learningObjectiveId);
    Task<List<TaskPrerequisite>> GetAllAsync();
    Task<List<TaskPrerequisite>> GetByTaskIdAsync(Guid taskId);
    Task AddAsync(TaskPrerequisite taskPrerequisite);
    Task UpdateAsync(TaskPrerequisite taskPrerequisite);
    Task DeleteAsync(Guid taskId, int learningObjectiveId);
}