using Models;

public interface ISupervisorRepo
{
    Task<Supervisor?> GetByIdAsync(Guid id);
    Task<Supervisor?> GetByEmailAsync(string email);
    Task<List<Supervisor>> GetAllAsync();
    Task AddAsync(Supervisor supervisor);
    Task UpdateAsync(Supervisor supervisor);
    Task DeleteAsync(Guid id);
}
