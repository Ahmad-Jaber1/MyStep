using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class TaskSubmissionEvaluationRepo : ITaskSubmissionEvaluationRepo
{
    private readonly MyStepDbContext _context;

    public TaskSubmissionEvaluationRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task<TaskSubmissionEvaluation?> GetByIdAsync(Guid evaluationId)
    {
        return await _context.TaskSubmissionEvaluations
            .Include(e => e.ValidationResults)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);
    }

    public async Task<TaskSubmissionEvaluation?> GetLatestByStudentAndTaskAsync(Guid studentId, Guid taskId)
    {
        return await _context.TaskSubmissionEvaluations
            .Where(e => e.StudentId == studentId && e.TaskId == taskId)
            .Include(e => e.ValidationResults)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<TaskSubmissionEvaluation>> GetByStudentAndTaskAsync(Guid studentId, Guid taskId)
    {
        return await _context.TaskSubmissionEvaluations
            .Where(e => e.StudentId == studentId && e.TaskId == taskId)
            .Include(e => e.ValidationResults)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TaskSubmissionEvaluation>> GetByStudentAsync(Guid studentId)
    {
        return await _context.TaskSubmissionEvaluations
            .Where(e => e.StudentId == studentId)
            .Include(e => e.ValidationResults)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(TaskSubmissionEvaluation evaluation)
    {
        await _context.TaskSubmissionEvaluations.AddAsync(evaluation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TaskSubmissionEvaluation evaluation)
    {
        _context.TaskSubmissionEvaluations.Update(evaluation);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid evaluationId)
    {
        var entity = await _context.TaskSubmissionEvaluations.FindAsync(evaluationId);
        if (entity == null) return;
        _context.TaskSubmissionEvaluations.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
