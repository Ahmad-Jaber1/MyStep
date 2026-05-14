using Models;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class SupervisorService : ISupervisorService
{
    private readonly ISupervisorRepo _supervisorRepo;
    private readonly ISupervisorStudentRepo _supervisorStudentRepo;
    private readonly IStudentRepo _studentRepo;

    public SupervisorService(
        ISupervisorRepo supervisorRepo,
        ISupervisorStudentRepo supervisorStudentRepo,
        IStudentRepo studentRepo)
    {
        _supervisorRepo = supervisorRepo;
        _supervisorStudentRepo = supervisorStudentRepo;
        _studentRepo = studentRepo;
    }

    public async Task<Result<SupervisorStudentResponseDto>> AddStudentAsync(Guid supervisorId, AddSupervisorStudentDto dto)
    {
        if (supervisorId == Guid.Empty)
        {
            return Result<SupervisorStudentResponseDto>.Failure("Supervisor id is required.");
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.StudentEmail))
        {
            return Result<SupervisorStudentResponseDto>.Failure("Student email is required.");
        }

        var supervisor = await _supervisorRepo.GetByIdAsync(supervisorId);
        if (supervisor is null)
        {
            return Result<SupervisorStudentResponseDto>.Failure("Supervisor was not found.");
        }

        var student = await _studentRepo.GetByEmailAsync(dto.StudentEmail.Trim().ToLowerInvariant());
        if (student is null)
        {
            return Result<SupervisorStudentResponseDto>.Failure("Student was not found.");
        }

        var existing = await _supervisorStudentRepo.GetAsync(supervisorId, student.Id, supervisor.PathId);
        if (existing is not null)
        {
            return Result<SupervisorStudentResponseDto>.Failure("This student already has a request for this supervisor and path.");
        }

        var relation = new SupervisorStudent
        {
            SupervisorId = supervisorId,
            StudentId = student.Id,
            PathId = supervisor.PathId,
            Status = ApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _supervisorStudentRepo.AddAsync(relation);
        relation.Supervisor = supervisor;
        relation.Student = student;
        relation.Path = supervisor.Path;

        return Result<SupervisorStudentResponseDto>.Success(Map(relation));
    }

    public async Task<Result<List<SupervisorStudentResponseDto>>> GetMyStudentsAsync(Guid supervisorId)
    {
        if (supervisorId == Guid.Empty)
        {
            return Result<List<SupervisorStudentResponseDto>>.Failure("Supervisor id is required.");
        }

        var relations = await _supervisorStudentRepo.GetBySupervisorAsync(supervisorId);
        return Result<List<SupervisorStudentResponseDto>>.Success(relations.Select(Map).ToList());
    }

    public async Task<Result<List<SupervisorStudentResponseDto>>> GetPendingRequestsAsync(Guid studentId)
    {
        if (studentId == Guid.Empty)
        {
            return Result<List<SupervisorStudentResponseDto>>.Failure("Student id is required.");
        }

        var relations = await _supervisorStudentRepo.GetPendingByStudentAsync(studentId);
        return Result<List<SupervisorStudentResponseDto>>.Success(relations.Select(Map).ToList());
    }

    public async Task<Result<SupervisorStudentResponseDto>> ApproveAsync(Guid studentId, Guid supervisorId, int pathId)
    {
        return await UpdateStatusAsync(studentId, supervisorId, pathId, ApprovalStatus.Approved);
    }

    public async Task<Result<SupervisorStudentResponseDto>> RejectAsync(Guid studentId, Guid supervisorId, int pathId)
    {
        return await UpdateStatusAsync(studentId, supervisorId, pathId, ApprovalStatus.Rejected);
    }

    private async Task<Result<SupervisorStudentResponseDto>> UpdateStatusAsync(Guid studentId, Guid supervisorId, int pathId, ApprovalStatus status)
    {
        if (studentId == Guid.Empty || supervisorId == Guid.Empty || pathId <= 0)
        {
            return Result<SupervisorStudentResponseDto>.Failure("Student id, supervisor id, and path id are required.");
        }

        var relation = await _supervisorStudentRepo.GetAsync(supervisorId, studentId, pathId);
        if (relation is null)
        {
            return Result<SupervisorStudentResponseDto>.Failure("Supervisor request was not found.");
        }

        relation.Status = status;
        relation.ApprovedAt = status == ApprovalStatus.Approved ? DateTime.UtcNow : null;
        await _supervisorStudentRepo.UpdateAsync(relation);

        return Result<SupervisorStudentResponseDto>.Success(Map(relation));
    }

    private static SupervisorStudentResponseDto Map(SupervisorStudent relation)
    {
        return new SupervisorStudentResponseDto
        {
            SupervisorId = relation.SupervisorId,
            SupervisorFullName = relation.Supervisor?.FullName ?? string.Empty,
            SupervisorEmail = relation.Supervisor?.Email ?? string.Empty,
            StudentId = relation.StudentId,
            StudentFullName = relation.Student?.FullName ?? string.Empty,
            StudentEmail = relation.Student?.Email ?? string.Empty,
            PathId = relation.PathId,
            Status = relation.Status,
            CreatedAt = relation.CreatedAt,
            ApprovedAt = relation.ApprovedAt
        };
    }
}
