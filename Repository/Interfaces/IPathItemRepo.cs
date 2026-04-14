using Models;

public interface IPathItemRepo
{
    Task<PathItem?> GetByIdAsync(int id);
    Task<List<PathItem>> GetAllAsync();
    Task AddAsync(PathItem path);
    Task UpdateAsync(PathItem path);
    Task DeleteAsync(int id);
}