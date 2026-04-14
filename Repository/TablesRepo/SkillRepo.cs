using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class SkillRepo : ISkillRepo
{
    private readonly MyStepDbContext _context;

    public SkillRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Skill skill)
    {
        await _context.Skills.AddAsync(skill);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Skill>> GetAllAsync()
    {
        return await _context.Skills
            .Include(s => s.Path)
            .Include(s => s.LearningObjectives)
            .ToListAsync();
    }

    public async Task<Skill?> GetByIdAsync(int id)
    {
        return await _context.Skills
            .Include(s => s.Path)
            .Include(s => s.LearningObjectives)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Skill>> GetByPathIdAsync(int pathId)
    {
        return await _context.Skills
            .Where(s => s.PathId == pathId)
            .ToListAsync();
    }

    public async Task UpdateAsync(Skill skill)
    {
        _context.Skills.Update(skill);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null) return;

        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();
    }
}