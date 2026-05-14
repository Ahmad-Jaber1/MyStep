using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class SupervisorRepo : ISupervisorRepo
{
    private readonly MyStepDbContext _context;

    public SupervisorRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Supervisor supervisor)
    {
        await _context.Supervisors.AddAsync(supervisor);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Supervisor>> GetAllAsync()
    {
        return await _context.Supervisors
            .Include(s => s.Path)
            .Include(s => s.SupervisorStudents)
            .ToListAsync();
    }

    public async Task<Supervisor?> GetByIdAsync(Guid id)
    {
        return await _context.Supervisors
            .Include(s => s.Path)
            .Include(s => s.SupervisorStudents)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supervisor?> GetByEmailAsync(string email)
    {
        return await _context.Supervisors
            .Include(s => s.Path)
            .Include(s => s.SupervisorStudents)
            .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
    }

    public async Task UpdateAsync(Supervisor supervisor)
    {
        _context.Supervisors.Update(supervisor);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var supervisor = await _context.Supervisors.FindAsync(id);
        if (supervisor == null) return;

        _context.Supervisors.Remove(supervisor);
        await _context.SaveChangesAsync();
    }
}
