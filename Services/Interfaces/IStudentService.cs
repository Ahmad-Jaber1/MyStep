using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface IStudentService
{
    Task<Result<List<StudentResponseDto>>> GetAllAsync();
    Task<Result<StudentResponseDto>> GetByIdAsync(Guid id);
    Task<Result<StudentResponseDto>> CreateAsync(CreateStudentDto dto);
    Task<Result<StudentResponseDto>> UpdateAsync(Guid id, UpdateStudentDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);

    Task<Result<StudentResponseDto>> SignUpAsync(SignUpStudentDto dto);
    Task<Result<AuthResponseDto>> SignInAsync(SignInStudentDto dto);
    Task<Result<bool>> SignOutAsync(Guid studentId);
    Task<Result<CurrentStudentDto>> GetCurrentStudentAsync(Guid studentId);
}
