using Models;

public interface ITaskItemRepo
{
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<List<TaskItem>> GetAllAsync();
    Task<List<TaskItem>> GetByPathIdAsync(int pathId);
    Task<List<TaskItem>> GetByMainSkillIdAsync(int mainSkillId);
    Task AddAsync(TaskItem taskItem);
    Task UpdateAsync(TaskItem taskItem);
    Task DeleteAsync(Guid id);
}