using Models;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class StudentLearningObjectiveService : IStudentLearningObjectiveService
{
    private readonly IStudentLearningObjectiveRepo _studentLearningObjectiveRepo;
    private readonly IStudentRepo _studentRepo;
    private readonly ILearningObjectiveRepo _learningObjectiveRepo;

    public StudentLearningObjectiveService(
        IStudentLearningObjectiveRepo studentLearningObjectiveRepo,
        IStudentRepo studentRepo,
        ILearningObjectiveRepo learningObjectiveRepo)
    {
        _studentLearningObjectiveRepo = studentLearningObjectiveRepo;
        _studentRepo = studentRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
    }

    public async Task<Result<List<StudentLearningObjectiveResponseDto>>> GetAllAsync()
    {
        var list = await _studentLearningObjectiveRepo.GetAllAsync();
        return Result<List<StudentLearningObjectiveResponseDto>>.Success(list.Select(MapToResponse).ToList());
    }

    public async Task<Result<StudentLearningObjectiveResponseDto>> GetByIdAsync(Guid studentId, int learningObjectiveId)
    {
        var validationError = ValidateIdentity(studentId, learningObjectiveId);
        if (validationError is not null)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure(validationError);
        }

        var entity = await _studentLearningObjectiveRepo.GetByIdAsync(studentId, learningObjectiveId);
        if (entity is null)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure(
                $"Student learning objective with student id {studentId} and learning objective id {learningObjectiveId} was not found.");
        }

        return Result<StudentLearningObjectiveResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<List<StudentLearningObjectiveResponseDto>>> GetByStudentIdAsync(Guid studentId)
    {
        if (studentId == Guid.Empty)
        {
            return Result<List<StudentLearningObjectiveResponseDto>>.Failure("Student id is required.");
        }

        var student = await _studentRepo.GetByIdAsync(studentId);
        if (student is null)
        {
            return Result<List<StudentLearningObjectiveResponseDto>>.Failure($"Student with id {studentId} was not found.");
        }

        var list = await _studentLearningObjectiveRepo.GetByStudentIdAsync(studentId);
        return Result<List<StudentLearningObjectiveResponseDto>>.Success(list.Select(MapToResponse).ToList());
    }

    public async Task<Result<List<StudentLearningObjectiveResponseDto>>> GetByLearningObjectiveIdAsync(int learningObjectiveId)
    {
        if (learningObjectiveId <= 0)
        {
            return Result<List<StudentLearningObjectiveResponseDto>>.Failure("Learning objective id must be greater than zero.");
        }

        var learningObjective = await _learningObjectiveRepo.GetByIdAsync(learningObjectiveId);
        if (learningObjective is null)
        {
            return Result<List<StudentLearningObjectiveResponseDto>>.Failure(
                $"Learning objective with id {learningObjectiveId} was not found.");
        }

        var list = await _studentLearningObjectiveRepo.GetByLearningObjectiveIdAsync(learningObjectiveId);
        return Result<List<StudentLearningObjectiveResponseDto>>.Success(list.Select(MapToResponse).ToList());
    }

    public async Task<Result<StudentLearningObjectiveResponseDto>> CreateAsync(CreateStudentLearningObjectiveDto dto)
    {
        if (dto is null)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure("Student learning objective payload is required.");
        }

        var validationError = await ValidatePayloadAsync(dto.StudentId, dto.LearningObjectiveId, dto.Score);
        if (validationError is not null)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure(validationError);
        }

        var existing = await _studentLearningObjectiveRepo.GetByIdAsync(dto.StudentId, dto.LearningObjectiveId);
        if (existing is not null)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure("Student learning objective already exists.");
        }

        var entity = new StudentLearningObjective
        {
            StudentId = dto.StudentId,
            LearningObjectiveId = dto.LearningObjectiveId,
            Score = dto.Score,
            LastUpdated = DateTime.UtcNow
        };

        await _studentLearningObjectiveRepo.AddAsync(entity);
        return Result<StudentLearningObjectiveResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<StudentLearningObjectiveResponseDto>> UpdateAsync(
        Guid studentId,
        int learningObjectiveId,
        UpdateStudentLearningObjectiveDto dto)
    {
        var identityValidationError = ValidateIdentity(studentId, learningObjectiveId);
        if (identityValidationError is not null)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure(identityValidationError);
        }

        if (dto is null)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure("Student learning objective payload is required.");
        }

        if (dto.Score < 0 || dto.Score > 100)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure("Score must be between 0 and 100.");
        }

        var entity = await _studentLearningObjectiveRepo.GetByIdAsync(studentId, learningObjectiveId);
        if (entity is null)
        {
            return Result<StudentLearningObjectiveResponseDto>.Failure(
                $"Student learning objective with student id {studentId} and learning objective id {learningObjectiveId} was not found.");
        }

        entity.Score = dto.Score;
        entity.LastUpdated = DateTime.UtcNow;

        await _studentLearningObjectiveRepo.UpdateAsync(entity);
        return Result<StudentLearningObjectiveResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<bool>> DeleteAsync(Guid studentId, int learningObjectiveId)
    {
        var validationError = ValidateIdentity(studentId, learningObjectiveId);
        if (validationError is not null)
        {
            return Result<bool>.Failure(validationError);
        }

        var entity = await _studentLearningObjectiveRepo.GetByIdAsync(studentId, learningObjectiveId);
        if (entity is null)
        {
            return Result<bool>.Failure(
                $"Student learning objective with student id {studentId} and learning objective id {learningObjectiveId} was not found.");
        }

        await _studentLearningObjectiveRepo.DeleteAsync(studentId, learningObjectiveId);
        return Result<bool>.Success(true);
    }

    private async Task<string?> ValidatePayloadAsync(Guid studentId, int learningObjectiveId, double score)
    {
        var identityValidationError = ValidateIdentity(studentId, learningObjectiveId);
        if (identityValidationError is not null)
        {
            return identityValidationError;
        }

        if (score < 0 || score > 100)
        {
            return "Score must be between 0 and 100.";
        }

        var student = await _studentRepo.GetByIdAsync(studentId);
        if (student is null)
        {
            return $"Student with id {studentId} was not found.";
        }

        var learningObjective = await _learningObjectiveRepo.GetByIdAsync(learningObjectiveId);
        if (learningObjective is null)
        {
            return $"Learning objective with id {learningObjectiveId} was not found.";
        }

        return null;
    }

    private static string? ValidateIdentity(Guid studentId, int learningObjectiveId)
    {
        if (studentId == Guid.Empty)
        {
            return "Student id is required.";
        }

        if (learningObjectiveId <= 0)
        {
            return "Learning objective id must be greater than zero.";
        }

        return null;
    }

    private static StudentLearningObjectiveResponseDto MapToResponse(StudentLearningObjective entity)
    {
        return new StudentLearningObjectiveResponseDto
        {
            StudentId = entity.StudentId,
            LearningObjectiveId = entity.LearningObjectiveId,
            Score = entity.Score,
            LastUpdated = entity.LastUpdated
        };
    }
}
