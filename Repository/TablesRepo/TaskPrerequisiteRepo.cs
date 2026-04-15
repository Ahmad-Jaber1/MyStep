using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class TaskPrerequisiteRepo : ITaskPrerequisiteRepo
{
    private readonly MyStepDbContext _context;

    public TaskPrerequisiteRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TaskPrerequisite taskPrerequisite)
    {
        await _context.TaskPrerequisites.AddAsync(taskPrerequisite);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TaskPrerequisite>> GetAllAsync()
    {
        return await _context.TaskPrerequisites
            .Include(tp => tp.Task)
            .Include(tp => tp.LearningObjective)
            .ToListAsync();
    }

    public async Task<TaskPrerequisite?> GetByIdAsync(Guid taskId, int learningObjectiveId)
    {
        return await _context.TaskPrerequisites
            .Include(tp => tp.Task)
            .Include(tp => tp.LearningObjective)
            .FirstOrDefaultAsync(tp => tp.TaskId == taskId && tp.LearningObjectiveId == learningObjectiveId);
    }

    public async Task<List<TaskPrerequisite>> GetByTaskIdAsync(Guid taskId)
    {
        return await _context.TaskPrerequisites
            .Where(tp => tp.TaskId == taskId)
            .ToListAsync();
    }

    public async Task UpdateAsync(TaskPrerequisite taskPrerequisite)
    {
        _context.TaskPrerequisites.Update(taskPrerequisite);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid taskId, int learningObjectiveId)
    {
        var prerequisite = await _context.TaskPrerequisites.FindAsync(taskId, learningObjectiveId);
        if (prerequisite == null) return;

        _context.TaskPrerequisites.Remove(prerequisite);
        await _context.SaveChangesAsync();
    }
}