using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class StudentLearningObjectiveRepo : IStudentLearningObjectiveRepo
{
    private readonly MyStepDbContext _context;

    public StudentLearningObjectiveRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StudentLearningObjective studentLearningObjective)
    {
        await _context.StudentLearningObjectives.AddAsync(studentLearningObjective);
        await _context.SaveChangesAsync();
    }

    public async Task<List<StudentLearningObjective>> GetAllAsync()
    {
        return await _context.StudentLearningObjectives
            .Include(slo => slo.Student)
            .Include(slo => slo.LearningObjective)
            .ToListAsync();
    }

    public async Task<StudentLearningObjective?> GetByIdAsync(Guid studentId, int learningObjectiveId)
    {
        return await _context.StudentLearningObjectives
            .Include(slo => slo.Student)
            .Include(slo => slo.LearningObjective)
            .FirstOrDefaultAsync(slo => slo.StudentId == studentId && slo.LearningObjectiveId == learningObjectiveId);
    }

    public async Task<List<StudentLearningObjective>> GetByStudentIdAsync(Guid studentId)
    {
        return await _context.StudentLearningObjectives
            .Where(slo => slo.StudentId == studentId)
            .ToListAsync();
    }

    public async Task<List<StudentLearningObjective>> GetByLearningObjectiveIdAsync(int learningObjectiveId)
    {
        return await _context.StudentLearningObjectives
            .Where(slo => slo.LearningObjectiveId == learningObjectiveId)
            .ToListAsync();
    }

    public async Task UpdateAsync(StudentLearningObjective studentLearningObjective)
    {
        _context.StudentLearningObjectives.Update(studentLearningObjective);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid studentId, int learningObjectiveId)
    {
        var relation = await _context.StudentLearningObjectives
            .FindAsync(studentId, learningObjectiveId);

        if (relation == null) return;

        _context.StudentLearningObjectives.Remove(relation);
        await _context.SaveChangesAsync();
    }
}
