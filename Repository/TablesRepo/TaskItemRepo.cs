using Microsoft.EntityFrameworkCore;
using Models;
using Pgvector;
using Repository;

public class TaskItemRepo : ITaskItemRepo
{
    private readonly MyStepDbContext _context;

    public TaskItemRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TaskItem taskItem)
    {
        await _context.Tasks.AddAsync(taskItem);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks
            .Include(t => t.Path)
            .Include(t => t.MainSkill)
            .Include(t => t.Targets)
            .Include(t => t.Prerequisites)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks
            .Include(t => t.Path)
            .Include(t => t.MainSkill)
            .Include(t => t.Targets)
            .Include(t => t.Prerequisites)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<TaskItem>> GetByPathIdAsync(int pathId)
    {
        return await _context.Tasks
            .Where(t => t.PathId == pathId)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetByMainSkillIdAsync(int mainSkillId)
    {
        return await _context.Tasks
            .Where(t => t.MainSkillId == mainSkillId)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetMostSimilarByVectorAsync(int mainSkillId, Vector queryVector, int topK)
    {
        if (mainSkillId <= 0 || topK <= 0)
        {
            return [];
        }

        return await _context.Tasks
            .FromSqlInterpolated($@"
                SELECT *
                FROM tasks
                WHERE ""MainSkillId"" = {mainSkillId}
                  AND ""SearchVector"" IS NOT NULL
                ORDER BY ""SearchVector"" <=> {queryVector}
                LIMIT {topK}")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateAsync(TaskItem taskItem)
    {
        _context.Tasks.Update(taskItem);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }
}