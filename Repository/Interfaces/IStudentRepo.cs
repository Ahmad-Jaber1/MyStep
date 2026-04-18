using Models;

public interface IStudentRepo
{
    Task<Student?> GetByIdAsync(Guid id);
    Task<Student?> GetByEmailAsync(string email);
    Task<List<Student>> GetAllAsync();
    Task AddAsync(Student student);
    Task UpdateAsync(Student student);
    Task DeleteAsync(Guid id);
}
