using Models;

public interface ISupervisorStudentRepo
{
    Task<SupervisorStudent?> GetAsync(Guid supervisorId, Guid studentId, int pathId);
    Task<List<SupervisorStudent>> GetBySupervisorAsync(Guid supervisorId);
    Task<List<SupervisorStudent>> GetPendingByStudentAsync(Guid studentId);
    Task<SupervisorStudent?> GetApprovedByStudentAndPathAsync(Guid studentId, int pathId);
    Task AddAsync(SupervisorStudent supervisorStudent);
    Task UpdateAsync(SupervisorStudent supervisorStudent);
}
