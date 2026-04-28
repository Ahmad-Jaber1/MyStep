using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class StudentTaskRepo : IStudentTaskRepo
{
    private readonly MyStepDbContext _context;

    public StudentTaskRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StudentTask studentTask)
    {
        await _context.StudentTasks.AddAsync(studentTask);
        await _context.SaveChangesAsync();
    }

    public async Task<StudentTask?> GetAsync(Guid studentId, Guid taskId)
    {
        return await _context.StudentTasks
            .Include(st => st.Task)
            .Include(st => st.Student)
            .FirstOrDefaultAsync(st => st.StudentId == studentId && st.TaskId == taskId);
    }

    public async Task<List<StudentTask>> GetByStudentAsync(Guid studentId)
    {
        return await _context.StudentTasks
            .Where(st => st.StudentId == studentId)
            .Include(st => st.Task)
            .ToListAsync();
    }

    public async Task<int> GetCountByStudentAndMainSkillAsync(Guid studentId, int mainSkillId)
    {
        return await _context.StudentTasks
            .Where(st => st.StudentId == studentId && st.Task.MainSkillId == mainSkillId)
            .CountAsync();
    }

    public async Task<bool> HasUnpassedTaskForMainSkillAsync(Guid studentId, int mainSkillId)
    {
        return await _context.StudentTasks
            .Where(st => st.StudentId == studentId && st.Task.MainSkillId == mainSkillId && !st.Passed)
            .AnyAsync();
    }

    public async Task UpdateAsync(StudentTask studentTask)
    {
        _context.StudentTasks.Update(studentTask);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid studentId, Guid taskId)
    {
        var entity = await _context.StudentTasks.FindAsync(studentId, taskId);
        if (entity == null) return;
        _context.StudentTasks.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
