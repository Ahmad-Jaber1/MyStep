using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class SkillService : ISkillService
{
    private readonly ISkillRepo _skillRepo;
    private readonly IPathItemRepo _pathItemRepo;

    public SkillService(ISkillRepo skillRepo, IPathItemRepo pathItemRepo)
    {
        _skillRepo = skillRepo;
        _pathItemRepo = pathItemRepo;
    }

    public async Task<Result<List<SkillResponseDto>>> GetAllAsync()
    {
        var skills = await _skillRepo.GetAllAsync();
        return Result<List<SkillResponseDto>>.Success(skills.Select(MapToResponse).ToList());
    }

    public async Task<Result<SkillResponseDto>> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return Result<SkillResponseDto>.Failure("Skill id must be greater than zero.");
        }

        var skill = await _skillRepo.GetByIdAsync(id);
        if (skill is null)
        {
            return Result<SkillResponseDto>.Failure($"Skill with id {id} was not found.");
        }

        return Result<SkillResponseDto>.Success(MapToResponse(skill));
    }

    public async Task<Result<List<SkillResponseDto>>> GetByPathIdAsync(int pathId)
    {
        if (pathId <= 0)
        {
            return Result<List<SkillResponseDto>>.Failure("Path id must be greater than zero.");
        }

        var pathExists = await _pathItemRepo.GetByIdAsync(pathId);
        if (pathExists is null)
        {
            return Result<List<SkillResponseDto>>.Failure($"Path with id {pathId} was not found.");
        }

        var skills = await _skillRepo.GetByPathIdAsync(pathId);
        return Result<List<SkillResponseDto>>.Success(skills.Select(MapToResponse).ToList());
    }

    public async Task<Result<SkillResponseDto>> CreateAsync(CreateSkillDto dto)
    {
        if (dto is null)
        {
            return Result<SkillResponseDto>.Failure("Skill payload is required.");
        }

        var normalizedName = TextNormalizer.NormalizeRequired(dto.Name);
        if (normalizedName is null)
        {
            return Result<SkillResponseDto>.Failure("Skill name is required.");
        }

        if (dto.PathId <= 0)
        {
            return Result<SkillResponseDto>.Failure("Skill path id must be greater than zero.");
        }

        var pathExists = await _pathItemRepo.GetByIdAsync(dto.PathId);
        if (pathExists is null)
        {
            return Result<SkillResponseDto>.Failure($"Path with id {dto.PathId} was not found.");
        }

        var entity = new Skill
        {
            Name = normalizedName,
            Description = TextNormalizer.NormalizeOptional(dto.Description),
            PathId = dto.PathId
        };

        await _skillRepo.AddAsync(entity);
        return Result<SkillResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<SkillResponseDto>> UpdateAsync(int id, UpdateSkillDto dto)
    {
        if (id <= 0)
        {
            return Result<SkillResponseDto>.Failure("Skill id must be greater than zero.");
        }

        if (dto is null)
        {
            return Result<SkillResponseDto>.Failure("Skill payload is required.");
        }

        var existingSkill = await _skillRepo.GetByIdAsync(id);
        if (existingSkill is null)
        {
            return Result<SkillResponseDto>.Failure($"Skill with id {id} was not found.");
        }

        var normalizedName = TextNormalizer.NormalizeRequired(dto.Name);
        if (normalizedName is null)
        {
            return Result<SkillResponseDto>.Failure("Skill name is required.");
        }

        if (dto.PathId <= 0)
        {
            return Result<SkillResponseDto>.Failure("Skill path id must be greater than zero.");
        }

        var pathExists = await _pathItemRepo.GetByIdAsync(dto.PathId);
        if (pathExists is null)
        {
            return Result<SkillResponseDto>.Failure($"Path with id {dto.PathId} was not found.");
        }

        existingSkill.Name = normalizedName;
        existingSkill.Description = TextNormalizer.NormalizeOptional(dto.Description);
        existingSkill.PathId = dto.PathId;

        await _skillRepo.UpdateAsync(existingSkill);
        return Result<SkillResponseDto>.Success(MapToResponse(existingSkill));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            return Result<bool>.Failure("Skill id must be greater than zero.");
        }

        var existingSkill = await _skillRepo.GetByIdAsync(id);
        if (existingSkill is null)
        {
            return Result<bool>.Failure($"Skill with id {id} was not found.");
        }

        await _skillRepo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    private static SkillResponseDto MapToResponse(Skill skill)
    {
        return new SkillResponseDto
        {
            Id = skill.Id,
            PathId = skill.PathId,
            Name = skill.Name,
            Description = skill.Description
        };
    }
}