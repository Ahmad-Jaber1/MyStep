using Models;

public interface ILearningObjectiveRepo
{
    Task<LearningObjective?> GetByIdAsync(int id);
    Task<List<LearningObjective>> GetAllAsync();
    Task<List<LearningObjective>> GetBySkillIdAsync(int skillId);
    Task AddAsync(LearningObjective objective);
    Task UpdateAsync(LearningObjective objective);
    Task DeleteAsync(int id);
}