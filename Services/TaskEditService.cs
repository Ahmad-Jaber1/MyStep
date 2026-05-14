using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class TaskEditService : ITaskEditService
{
    private readonly MyStepDbContext _dbContext;
    private readonly ITaskItemRepo _taskItemRepo;
    private readonly ILearningObjectiveRepo _learningObjectiveRepo;
    private readonly IStudentLearningObjectiveRepo _studentLearningObjectiveRepo;
    private readonly ITaskSearchVectorService _taskSearchVectorService;

    public TaskEditService(
        MyStepDbContext dbContext,
        ITaskItemRepo taskItemRepo,
        ILearningObjectiveRepo learningObjectiveRepo,
        IStudentLearningObjectiveRepo studentLearningObjectiveRepo,
        ITaskSearchVectorService taskSearchVectorService)
    {
        _dbContext = dbContext;
        _taskItemRepo = taskItemRepo;
        _learningObjectiveRepo = learningObjectiveRepo;
        _studentLearningObjectiveRepo = studentLearningObjectiveRepo;
        _taskSearchVectorService = taskSearchVectorService;
    }

    public async Task<Result<TaskEditResponseDto>> EditObjectivesAsync(Guid supervisorId, Guid taskId, EditTaskObjectivesDto dto)
    {
        var ownership = await ValidateSupervisorOwnershipAsync(supervisorId, taskId);
        if (!ownership.IsSuccess) return Result<TaskEditResponseDto>.Failure(ownership.ErrorMessage ?? "Supervisor ownership validation failed.");

        var task = await _taskItemRepo.GetByIdAsync(taskId);
        if (task is null) return Result<TaskEditResponseDto>.Failure("Task not found.");

        // Validate that objectives belong to the main skill
        var objectives = await _learningObjectiveRepo.GetAllAsync();
        var mainSkillObjectives = objectives.Where(o => o.SkillId == task.MainSkillId).Select(o => o.Id).ToHashSet();
        var unknown = dto.ObjectiveIds.Where(id => !mainSkillObjectives.Contains(id)).ToList();
        if (unknown.Any())
        {
            return Result<TaskEditResponseDto>.Failure($"Some objectives are not part of the task's main skill: {string.Join(", ", unknown)}");
        }

        // Replace targets
        task.Targets.Clear();
        task.Targets = dto.ObjectiveIds.Select(oid => new TaskTarget { TaskId = task.Id, LearningObjectiveId = oid, Task = task }).ToList();
        task.TaskData = UpdateTaskObjectivesJson(task.TaskData, dto.ObjectiveIds);
        task.SupervisorEdited = true;
        task.EditedBySupervisorId = supervisorId;
        task.EditedAt = DateTime.UtcNow;

        var vectorResult = await _taskSearchVectorService.BuildVectorAsync(task);
        if (!vectorResult.IsSuccess)
        {
            return Result<TaskEditResponseDto>.Failure(vectorResult.ErrorMessage ?? "Failed to rebuild task vector after updating objectives.");
        }

        task.SearchVector = new Pgvector.Vector(vectorResult.Data!);

        await _taskItemRepo.UpdateAsync(task);

        return Result<TaskEditResponseDto>.Success(new TaskEditResponseDto { TaskId = task.Id, SupervisorEdited = task.SupervisorEdited });
    }

    public async Task<Result<TaskEditResponseDto>> EditPrerequisitesAsync(Guid supervisorId, Guid taskId, EditTaskPrerequisitesDto dto)
    {
        var ownership = await ValidateSupervisorOwnershipAsync(supervisorId, taskId);
        if (!ownership.IsSuccess) return Result<TaskEditResponseDto>.Failure(ownership.ErrorMessage ?? "Supervisor ownership validation failed.");

        var task = await _taskItemRepo.GetByIdAsync(taskId);
        if (task is null) return Result<TaskEditResponseDto>.Failure("Task not found.");

        // Get student id from student_tasks table (task should be assigned to one student)
        var studentTask = await _dbContext.StudentTasks.FirstOrDefaultAsync(st => st.TaskId == taskId);
        if (studentTask is null) return Result<TaskEditResponseDto>.Failure("Task is not assigned to any student.");
        var studentId = studentTask.StudentId;

        var objectives = await _learningObjectiveRepo.GetAllAsync();
        var objectivesById = objectives.ToDictionary(o => o.Id);

        // Validate prerequisites are from OTHER skills
        var invalid = dto.PrerequisiteObjectiveIds.Where(id => objectivesById.TryGetValue(id, out var obj) && obj.SkillId == task.MainSkillId).ToList();
        if (invalid.Any())
        {
            return Result<TaskEditResponseDto>.Failure($"Prerequisites cannot be from the main skill: {string.Join(", ", invalid)}");
        }

        // Validate student score >= 0.7 on prerequisites if they exist in student's records
        var studentObjectives = await _studentLearningObjectiveRepo.GetByStudentIdAsync(studentId);
        var scoreById = studentObjectives.ToDictionary(s => s.LearningObjectiveId, s => s.Score);
        var lowScores = dto.PrerequisiteObjectiveIds.Where(id => scoreById.TryGetValue(id, out var score) && score < 0.7).ToList();
        if (lowScores.Any())
        {
            return Result<TaskEditResponseDto>.Failure($"Student does not meet mastery threshold for prerequisites: {string.Join(", ", lowScores)}");
        }

        // Replace prerequisites
        task.Prerequisites.Clear();
        task.Prerequisites = dto.PrerequisiteObjectiveIds.Select(pid => new TaskPrerequisite { TaskId = task.Id, LearningObjectiveId = pid, Justification = string.Empty, Task = task }).ToList();
        task.SupervisorEdited = true;
        task.EditedBySupervisorId = supervisorId;
        task.EditedAt = DateTime.UtcNow;

        await _taskItemRepo.UpdateAsync(task);

        return Result<TaskEditResponseDto>.Success(new TaskEditResponseDto { TaskId = task.Id, SupervisorEdited = task.SupervisorEdited });
    }

    public async Task<Result<TaskEditResponseDto>> EditValidationsAsync(Guid supervisorId, Guid taskId, EditTaskValidationsDto dto)
    {
        var ownership = await ValidateSupervisorOwnershipAsync(supervisorId, taskId);
        if (!ownership.IsSuccess) return Result<TaskEditResponseDto>.Failure(ownership.ErrorMessage ?? "Supervisor ownership validation failed.");

        var task = await _taskItemRepo.GetByIdAsync(taskId);
        if (task is null) return Result<TaskEditResponseDto>.Failure("Task not found.");

        // TaskData is jsonb; update validation_criteria array
        try
        {
            var root = task.TaskData.RootElement.Clone();
            var doc = root;

            using var docWriterStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(docWriterStream))
            {
                writer.WriteStartObject();
                foreach (var prop in doc.EnumerateObject())
                {
                    if (prop.NameEquals("validation_criteria")) continue;
                    prop.WriteTo(writer);
                }

                // write validation_criteria
                writer.WritePropertyName("validation_criteria");
                writer.WriteStartArray();
                foreach (var v in dto.Validations)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("skill_id", v.SkillId);
                    writer.WriteString("criterion", v.Criterion ?? string.Empty);
                    writer.WriteNumber("related_learning_objective", v.RelatedLearningObjective ?? 0);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            var bytes = docWriterStream.ToArray();
            var jsonDoc = JsonDocument.Parse(bytes);
            task.TaskData = jsonDoc;
            task.SupervisorEdited = true;
            task.EditedBySupervisorId = supervisorId;
            task.EditedAt = DateTime.UtcNow;

            await _taskItemRepo.UpdateAsync(task);

            return Result<TaskEditResponseDto>.Success(new TaskEditResponseDto { TaskId = task.Id, SupervisorEdited = task.SupervisorEdited });
        }
        catch (Exception ex)
        {
            return Result<TaskEditResponseDto>.Failure($"Failed to update validations: {ex.Message}");
        }
    }

    private async Task<Result<bool>> ValidateSupervisorOwnershipAsync(Guid supervisorId, Guid taskId)
    {
        if (supervisorId == Guid.Empty || taskId == Guid.Empty)
        {
            return Result<bool>.Failure("Supervisor id and task id are required.");
        }

        // Find the student task linking this task to a student
        var studentTask = await _dbContext.StudentTasks.FirstOrDefaultAsync(st => st.TaskId == taskId);
        if (studentTask is null)
        {
            return Result<bool>.Failure("Task is not assigned to any student.");
        }

        var task = await _taskItemRepo.GetByIdAsync(taskId);
        if (task is null)
        {
            return Result<bool>.Failure("Task not found.");
        }

        // Check supervisor_student approved relation
        var rel = await _dbContext.SupervisorStudents.FirstOrDefaultAsync(ss => ss.StudentId == studentTask.StudentId && ss.PathId == task.PathId && ss.SupervisorId == supervisorId && ss.Status == ApprovalStatus.Approved);
        if (rel is null)
        {
            return Result<bool>.Failure("Supervisor does not have permission to edit this task.");
        }

        return Result<bool>.Success(true);
    }

    private static JsonDocument UpdateTaskObjectivesJson(JsonDocument taskData, IReadOnlyCollection<int> objectiveIds)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            foreach (var property in taskData.RootElement.EnumerateObject())
            {
                if (property.NameEquals("targeted_objectives"))
                {
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WritePropertyName("targeted_objectives");
            writer.WriteStartArray();
            foreach (var objectiveId in objectiveIds)
            {
                writer.WriteNumberValue(objectiveId);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        return JsonDocument.Parse(stream.ToArray());
    }
}
