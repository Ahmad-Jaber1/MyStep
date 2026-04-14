using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class LearningObjectiveService : ILearningObjectiveService
{
    private readonly ILearningObjectiveRepo _learningObjectiveRepo;
    private readonly ISkillRepo _skillRepo;

    public LearningObjectiveService(ILearningObjectiveRepo learningObjectiveRepo, ISkillRepo skillRepo)
    {
        _learningObjectiveRepo = learningObjectiveRepo;
        _skillRepo = skillRepo;
    }

    public async Task<Result<List<LearningObjectiveResponseDto>>> GetAllAsync()
    {
        var objectives = await _learningObjectiveRepo.GetAllAsync();
        return Result<List<LearningObjectiveResponseDto>>.Success(objectives.Select(MapToResponse).ToList());
    }

    public async Task<Result<LearningObjectiveResponseDto>> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return Result<LearningObjectiveResponseDto>.Failure("Learning objective id must be greater than zero.");
        }

        var objective = await _learningObjectiveRepo.GetByIdAsync(id);
        if (objective is null)
        {
            return Result<LearningObjectiveResponseDto>.Failure($"Learning objective with id {id} was not found.");
        }

        return Result<LearningObjectiveResponseDto>.Success(MapToResponse(objective));
    }

    public async Task<Result<List<LearningObjectiveResponseDto>>> GetBySkillIdAsync(int skillId)
    {
        if (skillId <= 0)
        {
            return Result<List<LearningObjectiveResponseDto>>.Failure("Skill id must be greater than zero.");
        }

        var skillExists = await _skillRepo.GetByIdAsync(skillId);
        if (skillExists is null)
        {
            return Result<List<LearningObjectiveResponseDto>>.Failure($"Skill with id {skillId} was not found.");
        }

        var objectives = await _learningObjectiveRepo.GetBySkillIdAsync(skillId);
        return Result<List<LearningObjectiveResponseDto>>.Success(objectives.Select(MapToResponse).ToList());
    }

    public async Task<Result<LearningObjectiveResponseDto>> CreateAsync(CreateLearningObjectiveDto dto)
    {
        if (dto is null)
        {
            return Result<LearningObjectiveResponseDto>.Failure("Learning objective payload is required.");
        }

        var normalizedDescription = TextNormalizer.NormalizeRequired(dto.Description);
        if (normalizedDescription is null)
        {
            return Result<LearningObjectiveResponseDto>.Failure("Learning objective description is required.");
        }

        if (dto.SkillId <= 0)
        {
            return Result<LearningObjectiveResponseDto>.Failure("Learning objective skill id must be greater than zero.");
        }

        var skillExists = await _skillRepo.GetByIdAsync(dto.SkillId);
        if (skillExists is null)
        {
            return Result<LearningObjectiveResponseDto>.Failure($"Skill with id {dto.SkillId} was not found.");
        }

        var entity = new LearningObjective
        {
            SkillId = dto.SkillId,
            Description = normalizedDescription
        };

        await _learningObjectiveRepo.AddAsync(entity);
        return Result<LearningObjectiveResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<LearningObjectiveResponseDto>> UpdateAsync(int id, UpdateLearningObjectiveDto dto)
    {
        if (id <= 0)
        {
            return Result<LearningObjectiveResponseDto>.Failure("Learning objective id must be greater than zero.");
        }

        if (dto is null)
        {
            return Result<LearningObjectiveResponseDto>.Failure("Learning objective payload is required.");
        }

        var existingObjective = await _learningObjectiveRepo.GetByIdAsync(id);
        if (existingObjective is null)
        {
            return Result<LearningObjectiveResponseDto>.Failure($"Learning objective with id {id} was not found.");
        }

        var normalizedDescription = TextNormalizer.NormalizeRequired(dto.Description);
        if (normalizedDescription is null)
        {
            return Result<LearningObjectiveResponseDto>.Failure("Learning objective description is required.");
        }

        if (dto.SkillId <= 0)
        {
            return Result<LearningObjectiveResponseDto>.Failure("Learning objective skill id must be greater than zero.");
        }

        var skillExists = await _skillRepo.GetByIdAsync(dto.SkillId);
        if (skillExists is null)
        {
            return Result<LearningObjectiveResponseDto>.Failure($"Skill with id {dto.SkillId} was not found.");
        }

        existingObjective.SkillId = dto.SkillId;
        existingObjective.Description = normalizedDescription;

        await _learningObjectiveRepo.UpdateAsync(existingObjective);
        return Result<LearningObjectiveResponseDto>.Success(MapToResponse(existingObjective));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            return Result<bool>.Failure("Learning objective id must be greater than zero.");
        }

        var existingObjective = await _learningObjectiveRepo.GetByIdAsync(id);
        if (existingObjective is null)
        {
            return Result<bool>.Failure($"Learning objective with id {id} was not found.");
        }

        await _learningObjectiveRepo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    private static LearningObjectiveResponseDto MapToResponse(LearningObjective objective)
    {
        return new LearningObjectiveResponseDto
        {
            Id = objective.Id,
            SkillId = objective.SkillId,
            Description = objective.Description
        };
    }
}