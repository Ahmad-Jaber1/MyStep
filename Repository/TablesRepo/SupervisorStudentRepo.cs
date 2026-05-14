using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class SupervisorStudentRepo : ISupervisorStudentRepo
{
    private readonly MyStepDbContext _context;

    public SupervisorStudentRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SupervisorStudent supervisorStudent)
    {
        await _context.SupervisorStudents.AddAsync(supervisorStudent);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SupervisorStudent>> GetBySupervisorAsync(Guid supervisorId)
    {
        return await _context.SupervisorStudents
            .Include(ss => ss.Supervisor)
            .Include(ss => ss.Student)
            .Include(ss => ss.Path)
            .Where(ss => ss.SupervisorId == supervisorId)
            .OrderByDescending(ss => ss.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SupervisorStudent>> GetPendingByStudentAsync(Guid studentId)
    {
        return await _context.SupervisorStudents
            .Include(ss => ss.Supervisor)
            .Include(ss => ss.Student)
            .Include(ss => ss.Path)
            .Where(ss => ss.StudentId == studentId && ss.Status == ApprovalStatus.Pending)
            .OrderByDescending(ss => ss.CreatedAt)
            .ToListAsync();
    }

    public async Task<SupervisorStudent?> GetApprovedByStudentAndPathAsync(Guid studentId, int pathId)
    {
        return await _context.SupervisorStudents
            .Include(ss => ss.Supervisor)
            .Include(ss => ss.Student)
            .Include(ss => ss.Path)
            .FirstOrDefaultAsync(ss =>
                ss.StudentId == studentId &&
                ss.PathId == pathId &&
                ss.Status == ApprovalStatus.Approved);
    }

    public async Task<SupervisorStudent?> GetAsync(Guid supervisorId, Guid studentId, int pathId)
    {
        return await _context.SupervisorStudents
            .Include(ss => ss.Supervisor)
            .Include(ss => ss.Student)
            .Include(ss => ss.Path)
            .FirstOrDefaultAsync(ss =>
                ss.SupervisorId == supervisorId &&
                ss.StudentId == studentId &&
                ss.PathId == pathId);
    }

    public async Task UpdateAsync(SupervisorStudent supervisorStudent)
    {
        _context.SupervisorStudents.Update(supervisorStudent);
        await _context.SaveChangesAsync();
    }
}
