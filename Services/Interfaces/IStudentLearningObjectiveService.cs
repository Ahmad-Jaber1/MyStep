using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface IStudentLearningObjectiveService
{
    Task<Result<List<StudentLearningObjectiveResponseDto>>> GetAllAsync();
    Task<Result<StudentLearningObjectiveResponseDto>> GetByIdAsync(Guid studentId, int learningObjectiveId);
    Task<Result<List<StudentLearningObjectiveResponseDto>>> GetByStudentIdAsync(Guid studentId);
    Task<Result<List<StudentLearningObjectiveResponseDto>>> GetByLearningObjectiveIdAsync(int learningObjectiveId);
    Task<Result<StudentLearningObjectiveResponseDto>> CreateAsync(CreateStudentLearningObjectiveDto dto);
    Task<Result<StudentLearningObjectiveResponseDto>> UpdateAsync(Guid studentId, int learningObjectiveId, UpdateStudentLearningObjectiveDto dto);
    Task<Result<bool>> DeleteAsync(Guid studentId, int learningObjectiveId);
}
