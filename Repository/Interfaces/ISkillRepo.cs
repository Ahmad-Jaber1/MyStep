using Models;

public interface ISkillRepo
{
    Task<Skill?> GetByIdAsync(int id);
    Task<List<Skill>> GetAllAsync();
    Task<List<Skill>> GetByPathIdAsync(int pathId);
    Task AddAsync(Skill skill);
    Task UpdateAsync(Skill skill);
    Task DeleteAsync(int id);
}