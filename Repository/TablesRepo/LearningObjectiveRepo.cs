using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class LearningObjectiveRepository : ILearningObjectiveRepo
{
    private readonly MyStepDbContext _context;

    public LearningObjectiveRepository(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LearningObjective objective)
    {
        await _context.LearningObjectives.AddAsync(objective);
        await _context.SaveChangesAsync();
    }

    public async Task<List<LearningObjective>> GetAllAsync()
    {
        return await _context.LearningObjectives
            .Include(l => l.Skill)
            .ToListAsync();
    }

    public async Task<LearningObjective?> GetByIdAsync(int id)
    {
        return await _context.LearningObjectives
            .Include(l => l.Skill)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<LearningObjective>> GetBySkillIdAsync(int skillId)
    {
        return await _context.LearningObjectives
            .Where(l => l.SkillId == skillId)
            .ToListAsync();
    }

    public async Task UpdateAsync(LearningObjective objective)
    {
        _context.LearningObjectives.Update(objective);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var obj = await _context.LearningObjectives.FindAsync(id);
        if (obj == null) return;

        _context.LearningObjectives.Remove(obj);
        await _context.SaveChangesAsync();
    }
}