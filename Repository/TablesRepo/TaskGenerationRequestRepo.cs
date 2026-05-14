using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class TaskGenerationRequestRepo : ITaskGenerationRequestRepo
{
    private readonly MyStepDbContext _context;

    public TaskGenerationRequestRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TaskGenerationRequest request)
    {
        await _context.TaskGenerationRequests.AddAsync(request);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TaskGenerationRequest>> GetBySupervisorAsync(Guid supervisorId)
    {
        return await _context.TaskGenerationRequests
            .Include(r => r.Student)
            .Include(r => r.Supervisor)
            .Include(r => r.Path)
            .Where(r => r.SupervisorId == supervisorId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskGenerationRequest?> GetByIdAsync(Guid id)
    {
        return await _context.TaskGenerationRequests
            .Include(r => r.Student)
            .Include(r => r.Supervisor)
            .Include(r => r.Path)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<TaskGenerationRequest?> GetPendingByStudentAndSkillAsync(Guid studentId, int mainSkillId)
    {
        return await _context.TaskGenerationRequests
            .Include(r => r.Student)
            .Include(r => r.Supervisor)
            .Include(r => r.Path)
            .FirstOrDefaultAsync(r =>
                r.StudentId == studentId &&
                r.MainSkillId == mainSkillId &&
                r.Status == TaskGenerationRequestStatus.Pending);
    }

    public async Task UpdateAsync(TaskGenerationRequest request)
    {
        _context.TaskGenerationRequests.Update(request);
        await _context.SaveChangesAsync();
    }
}
