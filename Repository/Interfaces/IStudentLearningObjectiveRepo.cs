using Models;

public interface IStudentLearningObjectiveRepo
{
    Task<StudentLearningObjective?> GetByIdAsync(Guid studentId, int learningObjectiveId);
    Task<List<StudentLearningObjective>> GetAllAsync();
    Task<List<StudentLearningObjective>> GetByStudentIdAsync(Guid studentId);
    Task<List<StudentLearningObjective>> GetByLearningObjectiveIdAsync(int learningObjectiveId);
    Task AddAsync(StudentLearningObjective studentLearningObjective);
    Task UpdateAsync(StudentLearningObjective studentLearningObjective);
    Task DeleteAsync(Guid studentId, int learningObjectiveId);
}
