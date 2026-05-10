using Models;

public interface IStudentTaskRepo
{
    Task<StudentTask?> GetAsync(Guid studentId, Guid taskId);
    Task<List<StudentTask>> GetByStudentAsync(Guid studentId);
    Task<List<StudentTask>> GetByStudentAndSkillAsync(Guid studentId, int skillId);
    Task<int> GetCountByStudentAndMainSkillAsync(Guid studentId, int mainSkillId);
    Task<bool> HasUnpassedTaskForMainSkillAsync(Guid studentId, int mainSkillId);
    Task AddAsync(StudentTask studentTask);
    Task UpdateAsync(StudentTask studentTask);
    Task DeleteAsync(Guid studentId, Guid taskId);
}
