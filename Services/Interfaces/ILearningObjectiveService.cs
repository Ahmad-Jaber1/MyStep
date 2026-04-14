using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ILearningObjectiveService
{
    Task<Result<List<LearningObjectiveResponseDto>>> GetAllAsync();
    Task<Result<LearningObjectiveResponseDto>> GetByIdAsync(int id);
    Task<Result<List<LearningObjectiveResponseDto>>> GetBySkillIdAsync(int skillId);
    Task<Result<LearningObjectiveResponseDto>> CreateAsync(CreateLearningObjectiveDto dto);
    Task<Result<LearningObjectiveResponseDto>> UpdateAsync(int id, UpdateLearningObjectiveDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}