using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class TaskTargetRepo : ITaskTargetRepo
{
    private readonly MyStepDbContext _context;

    public TaskTargetRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TaskTarget taskTarget)
    {
        await _context.TaskTargets.AddAsync(taskTarget);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TaskTarget>> GetAllAsync()
    {
        return await _context.TaskTargets
            .Include(tt => tt.Task)
            .Include(tt => tt.LearningObjective)
            .ToListAsync();
    }

    public async Task<TaskTarget?> GetByIdAsync(Guid taskId, int learningObjectiveId)
    {
        return await _context.TaskTargets
            .Include(tt => tt.Task)
            .Include(tt => tt.LearningObjective)
            .FirstOrDefaultAsync(tt => tt.TaskId == taskId && tt.LearningObjectiveId == learningObjectiveId);
    }

    public async Task<List<TaskTarget>> GetByTaskIdAsync(Guid taskId)
    {
        return await _context.TaskTargets
            .Where(tt => tt.TaskId == taskId)
            .ToListAsync();
    }

    public async Task UpdateAsync(TaskTarget taskTarget)
    {
        _context.TaskTargets.Update(taskTarget);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid taskId, int learningObjectiveId)
    {
        var target = await _context.TaskTargets.FindAsync(taskId, learningObjectiveId);
        if (target == null) return;

        _context.TaskTargets.Remove(target);
        await _context.SaveChangesAsync();
    }
}