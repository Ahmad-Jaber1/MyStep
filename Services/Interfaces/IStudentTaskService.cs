using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface IStudentTaskService
{
    Task<Result<StudentTaskResponseDto>> GetAsync(Guid studentId, Guid taskId);
    Task<Result<List<StudentTaskResponseDto>>> GetByStudentAsync(Guid studentId);
    Task<Result<StudentTaskResponseDto>> CreateAsync(CreateStudentTaskDto dto);
    Task<Result<StudentTaskResponseDto>> UpdateAsync(Guid studentId, Guid taskId, UpdateStudentTaskDto dto);
    Task<Result<StudentTaskResponseDto>> MarkAsPassedAsync(Guid studentId, Guid taskId, double? score = null);
    Task<Result<bool>> DeleteAsync(Guid studentId, Guid taskId);
}
