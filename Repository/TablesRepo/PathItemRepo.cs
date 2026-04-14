using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class PathItemRepo : IPathItemRepo
{
    private readonly MyStepDbContext _context;

    public PathItemRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PathItem path)
    {
        await _context.Paths.AddAsync(path);
        await _context.SaveChangesAsync();
    }

    public async Task<List<PathItem>> GetAllAsync()
    {
        return await _context.Paths
            .Include(p => p.Skills)
            .ToListAsync();
    }

    public async Task<PathItem?> GetByIdAsync(int id)
    {
        return await _context.Paths
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task UpdateAsync(PathItem path)
    {
        _context.Paths.Update(path);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var path = await _context.Paths.FindAsync(id);
        if (path == null) return;

        _context.Paths.Remove(path);
        await _context.SaveChangesAsync();
    }
}