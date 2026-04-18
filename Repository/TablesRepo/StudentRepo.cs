using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

public class StudentRepo : IStudentRepo
{
    private readonly MyStepDbContext _context;

    public StudentRepo(MyStepDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Student student)
    {
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Student>> GetAllAsync()
    {
        return await _context.Students
            .Include(s => s.SelectedPath)
            .Include(s => s.StudentLearningObjectives)
            .ToListAsync();
    }

    public async Task<Student?> GetByIdAsync(Guid id)
    {
        return await _context.Students
            .Include(s => s.SelectedPath)
            .Include(s => s.StudentLearningObjectives)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Student?> GetByEmailAsync(string email)
    {
        return await _context.Students
            .Include(s => s.SelectedPath)
            .Include(s => s.StudentLearningObjectives)
            .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
    }

    public async Task UpdateAsync(Student student)
    {
        _context.Students.Update(student);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null) return;

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();
    }
}
