using Models;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class StudentTaskService : IStudentTaskService
{
    private readonly IStudentTaskRepo _studentTaskRepo;
    private readonly IStudentRepo _studentRepo;
    private readonly ITaskItemRepo _taskItemRepo;

    public StudentTaskService(
        IStudentTaskRepo studentTaskRepo,
        IStudentRepo studentRepo,
        ITaskItemRepo taskItemRepo)
    {
        _studentTaskRepo = studentTaskRepo;
        _studentRepo = studentRepo;
        _taskItemRepo = taskItemRepo;
    }

    public async Task<Result<StudentTaskResponseDto>> GetAsync(Guid studentId, Guid taskId)
    {
        if (studentId == Guid.Empty || taskId == Guid.Empty)
        {
            return Result<StudentTaskResponseDto>.Failure("Student id and task id are required.");
        }

        var entity = await _studentTaskRepo.GetAsync(studentId, taskId);
        if (entity is null) return Result<StudentTaskResponseDto>.Failure("Student task was not found.");

        return Result<StudentTaskResponseDto>.Success(MapToDto(entity));
    }

    public async Task<Result<List<StudentTaskResponseDto>>> GetByStudentAsync(Guid studentId)
    {
        if (studentId == Guid.Empty) return Result<List<StudentTaskResponseDto>>.Failure("Student id is required.");

        var list = await _studentTaskRepo.GetByStudentAsync(studentId);
        return Result<List<StudentTaskResponseDto>>.Success(list.Select(MapToDto).ToList());
    }

    public async Task<Result<StudentTaskResponseDto>> CreateAsync(CreateStudentTaskDto dto)
    {
        if (dto is null) return Result<StudentTaskResponseDto>.Failure("Payload is required.");
        if (dto.StudentId == Guid.Empty || dto.TaskId == Guid.Empty) return Result<StudentTaskResponseDto>.Failure("Student id and task id are required.");

        var student = await _studentRepo.GetByIdAsync(dto.StudentId);
        if (student is null) return Result<StudentTaskResponseDto>.Failure("Student was not found.");

        var task = await _taskItemRepo.GetByIdAsync(dto.TaskId);
        if (task is null) return Result<StudentTaskResponseDto>.Failure("Task was not found.");

        // Determine next number for this main skill for the student
        var currentCount = await _studentTaskRepo.GetCountByStudentAndMainSkillAsync(dto.StudentId, task.MainSkillId);
        var number = currentCount + 1;

        var entity = new StudentTask
        {
            StudentId = dto.StudentId,
            TaskId = dto.TaskId,
            NumberInMainSkill = number,
            Passed = false,
            StartedAt = DateTime.UtcNow
        };

        await _studentTaskRepo.AddAsync(entity);
        return Result<StudentTaskResponseDto>.Success(MapToDto(entity));
    }

    public async Task<Result<StudentTaskResponseDto>> UpdateAsync(Guid studentId, Guid taskId, UpdateStudentTaskDto dto)
    {
        if (studentId == Guid.Empty || taskId == Guid.Empty) return Result<StudentTaskResponseDto>.Failure("Student id and task id are required.");
        var existing = await _studentTaskRepo.GetAsync(studentId, taskId);
        if (existing is null) return Result<StudentTaskResponseDto>.Failure("Student task was not found.");

        if (dto.Passed.HasValue) existing.Passed = dto.Passed.Value;
        if (dto.Score.HasValue) existing.Score = dto.Score.Value;
        if (dto.StartedAt.HasValue) existing.StartedAt = dto.StartedAt;
        if (dto.CompletedAt.HasValue) existing.CompletedAt = dto.CompletedAt;

        await _studentTaskRepo.UpdateAsync(existing);
        return Result<StudentTaskResponseDto>.Success(MapToDto(existing));
    }

    public async Task<Result<StudentTaskResponseDto>> MarkAsPassedAsync(Guid studentId, Guid taskId, double? score = null)
    {
        if (studentId == Guid.Empty || taskId == Guid.Empty)
            return Result<StudentTaskResponseDto>.Failure("Student id and task id are required.");

        var existing = await _studentTaskRepo.GetAsync(studentId, taskId);
        if (existing is null)
            return Result<StudentTaskResponseDto>.Failure("Student task was not found.");

        existing.Passed = true;
        existing.CompletedAt = DateTime.UtcNow;
        if (score.HasValue && score.Value >= 0 && score.Value <= 100)
            existing.Score = score.Value;

        await _studentTaskRepo.UpdateAsync(existing);
        return Result<StudentTaskResponseDto>.Success(MapToDto(existing));
    }

    public async Task<Result<bool>> DeleteAsync(Guid studentId, Guid taskId)
    {
        if (studentId == Guid.Empty || taskId == Guid.Empty) return Result<bool>.Failure("Student id and task id are required.");
        await _studentTaskRepo.DeleteAsync(studentId, taskId);
        return Result<bool>.Success(true);
    }

    private static StudentTaskResponseDto MapToDto(StudentTask st)
    {
        return new StudentTaskResponseDto
        {
            StudentId = st.StudentId,
            TaskId = st.TaskId,
            NumberInMainSkill = st.NumberInMainSkill,
            Passed = st.Passed,
            StartedAt = st.StartedAt,
            CompletedAt = st.CompletedAt,
            Score = st.Score
        };
    }
}
