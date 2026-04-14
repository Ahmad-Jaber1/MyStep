using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ISkillService
{
    Task<Result<List<SkillResponseDto>>> GetAllAsync();
    Task<Result<SkillResponseDto>> GetByIdAsync(int id);
    Task<Result<List<SkillResponseDto>>> GetByPathIdAsync(int pathId);
    Task<Result<SkillResponseDto>> CreateAsync(CreateSkillDto dto);
    Task<Result<SkillResponseDto>> UpdateAsync(int id, UpdateSkillDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}