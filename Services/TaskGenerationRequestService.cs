using Models;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class TaskGenerationRequestService : ITaskGenerationRequestService
{
    private readonly IStudentRepo _studentRepo;
    private readonly ISupervisorRepo _supervisorRepo;
    private readonly ISupervisorStudentRepo _supervisorStudentRepo;
    private readonly IStudentTaskRepo _studentTaskRepo;
    private readonly ITaskGenerationRequestRepo _taskGenerationRequestRepo;
    private readonly IPathItemRepo _pathItemRepo;
    private readonly ISkillRepo _skillRepo;
    private readonly ITaskSearchVectorService _taskSearchVectorService;

    public TaskGenerationRequestService(
        IStudentRepo studentRepo,
        ISupervisorRepo supervisorRepo,
        ISupervisorStudentRepo supervisorStudentRepo,
        IStudentTaskRepo studentTaskRepo,
        ITaskGenerationRequestRepo taskGenerationRequestRepo,
        IPathItemRepo pathItemRepo,
        ISkillRepo skillRepo,
        ITaskSearchVectorService taskSearchVectorService)
    {
        _studentRepo = studentRepo;
        _supervisorRepo = supervisorRepo;
        _supervisorStudentRepo = supervisorStudentRepo;
        _studentTaskRepo = studentTaskRepo;
        _taskGenerationRequestRepo = taskGenerationRequestRepo;
        _pathItemRepo = pathItemRepo;
        _skillRepo = skillRepo;
        _taskSearchVectorService = taskSearchVectorService;
    }

    public async Task<Result<TaskGenerationRequestResponseDto>> CreateAsync(Guid studentId, CreateTaskGenerationRequestDto dto)
    {
        if (studentId == Guid.Empty)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Student id is required.");
        }

        if (dto is null || dto.MainSkillId <= 0)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Main skill id is required.");
        }

        var student = await _studentRepo.GetByIdAsync(studentId);
        if (student is null)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Student was not found.");
        }

        if (student.SelectedPathId is null)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Student must select a path before requesting a task.");
        }

        var mainSkill = await _skillRepo.GetByIdAsync(dto.MainSkillId);
        if (mainSkill is null)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Main skill was not found.");
        }

        if (mainSkill.PathId != student.SelectedPathId.Value)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Requested skill does not belong to the student's selected path.");
        }

        var hasUnpassedTask = await _studentTaskRepo.HasUnpassedTaskForMainSkillAsync(studentId, dto.MainSkillId);
        if (hasUnpassedTask)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("There is already an unfinished task for this student in the selected main skill.");
        }

        var existingPending = await _taskGenerationRequestRepo.GetPendingByStudentAndSkillAsync(studentId, dto.MainSkillId);
        if (existingPending is not null)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("There is already a pending generation request for this student and skill.");
        }

        var approvedSupervisor = await _supervisorStudentRepo.GetApprovedByStudentAndPathAsync(studentId, student.SelectedPathId.Value);
        if (approvedSupervisor is null)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("No approved supervisor was found for this student and path.");
        }

        var request = new TaskGenerationRequest
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            SupervisorId = approvedSupervisor.SupervisorId,
            PathId = student.SelectedPathId.Value,
            MainSkillId = dto.MainSkillId,
            Status = TaskGenerationRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _taskGenerationRequestRepo.AddAsync(request);
        request.Student = student;
        request.Supervisor = approvedSupervisor.Supervisor;
        request.Path = await _pathItemRepo.GetByIdAsync(request.PathId);

        return Result<TaskGenerationRequestResponseDto>.Success(Map(request, mainSkill.Name));
    }

    public async Task<Result<List<TaskGenerationRequestResponseDto>>> GetBySupervisorAsync(Guid supervisorId)
    {
        if (supervisorId == Guid.Empty)
        {
            return Result<List<TaskGenerationRequestResponseDto>>.Failure("Supervisor id is required.");
        }

        var supervisor = await _supervisorRepo.GetByIdAsync(supervisorId);
        if (supervisor is null)
        {
            return Result<List<TaskGenerationRequestResponseDto>>.Failure("Supervisor was not found.");
        }

        var requests = await _taskGenerationRequestRepo.GetBySupervisorAsync(supervisorId);
        var skillIds = requests.Select(request => request.MainSkillId).Distinct().ToList();
        var skills = new Dictionary<int, string>();
        foreach (var skillId in skillIds)
        {
            var skill = await _skillRepo.GetByIdAsync(skillId);
            if (skill is not null)
            {
                skills[skillId] = skill.Name;
            }
        }

        return Result<List<TaskGenerationRequestResponseDto>>.Success(
            requests.Select(request => Map(request, skills.TryGetValue(request.MainSkillId, out var skillName) ? skillName : string.Empty)).ToList());
    }

    public Task<Result<TaskGenerationRequestResponseDto>> ApproveAsync(Guid supervisorId, Guid requestId)
    {
        return Task.FromResult(Result<TaskGenerationRequestResponseDto>.Failure(
            "Approving a task request requires persisting a generated task. Use approve-and-persist instead."));
    }

    public async Task<Result<TaskGenerationRequestResponseDto>> RejectAsync(Guid supervisorId, Guid requestId)
    {
        return await UpdateStatusAsync(supervisorId, requestId, TaskGenerationRequestStatus.Rejected);
    }

    public async Task<Result<GenerateTaskResponseDto>> GeneratePreviewForRequestAsync(Guid supervisorId, Guid requestId)
    {
        if (supervisorId == Guid.Empty || requestId == Guid.Empty)
        {
            return Result<GenerateTaskResponseDto>.Failure("Supervisor id and request id are required.");
        }

        var request = await _taskGenerationRequestRepo.GetByIdAsync(requestId);
        if (request is null)
        {
            return Result<GenerateTaskResponseDto>.Failure("Task generation request was not found.");
        }

        if (request.SupervisorId != supervisorId)
        {
            return Result<GenerateTaskResponseDto>.Failure("You are not allowed to view this request.");
        }

        if (request.Status != TaskGenerationRequestStatus.Pending)
        {
            return Result<GenerateTaskResponseDto>.Failure("Only pending requests can be previewed.");
        }

        return await _taskSearchVectorService.GeneratePreviewAsync(request.StudentId, request.MainSkillId);
    }

    public async Task<Result<TaskGenerationRequestResponseDto>> ApproveAndPersistAsync(Guid supervisorId, Guid requestId, string generatedContent)
    {
        if (supervisorId == Guid.Empty || requestId == Guid.Empty)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Supervisor id and request id are required.");
        }

        if (string.IsNullOrWhiteSpace(generatedContent))
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Generated content is required to persist the task.");
        }

        var request = await _taskGenerationRequestRepo.GetByIdAsync(requestId);
        if (request is null)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Task generation request was not found.");
        }

        if (request.SupervisorId != supervisorId)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("You are not allowed to update this request.");
        }

        if (request.Status != TaskGenerationRequestStatus.Pending)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Only pending requests can be approved and persisted.");
        }

        var persistResult = await _taskSearchVectorService.PersistGeneratedTaskFromContentAsync(request.StudentId, request.MainSkillId, generatedContent);
        if (!persistResult.IsSuccess)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure(persistResult.ErrorMessage ?? "Failed to persist generated task.");
        }

        request.Status = TaskGenerationRequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;
        await _taskGenerationRequestRepo.UpdateAsync(request);

        var skill = await _skillRepo.GetByIdAsync(request.MainSkillId);
        return Result<TaskGenerationRequestResponseDto>.Success(Map(request, skill?.Name ?? string.Empty));
    }

    public async Task<Result<ApproveAndGenerateTaskResponseDto>> ApproveAndGenerateAsync(Guid supervisorId, Guid requestId)
    {
        if (supervisorId == Guid.Empty || requestId == Guid.Empty)
        {
            return Result<ApproveAndGenerateTaskResponseDto>.Failure("Supervisor id and request id are required.");
        }

        var request = await _taskGenerationRequestRepo.GetByIdAsync(requestId);
        if (request is null)
        {
            return Result<ApproveAndGenerateTaskResponseDto>.Failure("Task generation request was not found.");
        }

        if (request.SupervisorId != supervisorId)
        {
            return Result<ApproveAndGenerateTaskResponseDto>.Failure("You are not allowed to update this request.");
        }

        if (request.Status != TaskGenerationRequestStatus.Pending)
        {
            return Result<ApproveAndGenerateTaskResponseDto>.Failure("Only pending requests can be approved and generated.");
        }

        var generationResult = await _taskSearchVectorService.GenerateTaskAsync(request.StudentId, request.MainSkillId);
        if (!generationResult.IsSuccess || generationResult.Data is null)
        {
            return Result<ApproveAndGenerateTaskResponseDto>.Failure(generationResult.ErrorMessage ?? "Failed to generate and persist task.");
        }

        request.Status = TaskGenerationRequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;
        await _taskGenerationRequestRepo.UpdateAsync(request);

        var skill = await _skillRepo.GetByIdAsync(request.MainSkillId);
        var mappedRequest = Map(request, skill?.Name ?? string.Empty);

        return Result<ApproveAndGenerateTaskResponseDto>.Success(new ApproveAndGenerateTaskResponseDto
        {
            Request = mappedRequest,
            TaskId = generationResult.Data.TaskId,
            TaskData = generationResult.Data.TaskData
        });
    }

    private async Task<Result<TaskGenerationRequestResponseDto>> UpdateStatusAsync(Guid supervisorId, Guid requestId, TaskGenerationRequestStatus status)
    {
        if (supervisorId == Guid.Empty || requestId == Guid.Empty)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Supervisor id and request id are required.");
        }

        var request = await _taskGenerationRequestRepo.GetByIdAsync(requestId);
        if (request is null)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("Task generation request was not found.");
        }

        if (request.SupervisorId != supervisorId)
        {
            return Result<TaskGenerationRequestResponseDto>.Failure("You are not allowed to update this request.");
        }

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        await _taskGenerationRequestRepo.UpdateAsync(request);

        var skill = await _skillRepo.GetByIdAsync(request.MainSkillId);
        return Result<TaskGenerationRequestResponseDto>.Success(Map(request, skill?.Name ?? string.Empty));
    }

    private static TaskGenerationRequestResponseDto Map(TaskGenerationRequest request, string mainSkillName)
    {
        return new TaskGenerationRequestResponseDto
        {
            Id = request.Id,
            StudentId = request.StudentId,
            StudentFullName = request.Student?.FullName ?? string.Empty,
            StudentEmail = request.Student?.Email ?? string.Empty,
            SupervisorId = request.SupervisorId,
            SupervisorFullName = request.Supervisor?.FullName ?? string.Empty,
            SupervisorEmail = request.Supervisor?.Email ?? string.Empty,
            PathId = request.PathId,
            PathName = request.Path?.Name ?? string.Empty,
            MainSkillId = request.MainSkillId,
            MainSkillName = mainSkillName,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }
}
