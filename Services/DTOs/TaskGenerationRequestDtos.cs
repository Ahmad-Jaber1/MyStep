using Models;
using System.Text.Json;

namespace Services.DTOs;

public class CreateTaskGenerationRequestDto
{
    public int MainSkillId { get; set; }
}

public class TaskGenerationRequestResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public Guid SupervisorId { get; set; }
    public string SupervisorFullName { get; set; } = string.Empty;
    public string SupervisorEmail { get; set; } = string.Empty;
    public int PathId { get; set; }
    public string PathName { get; set; } = string.Empty;
    public int MainSkillId { get; set; }
    public string MainSkillName { get; set; } = string.Empty;
    public TaskGenerationRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ApproveAndGenerateTaskResponseDto
{
    public TaskGenerationRequestResponseDto Request { get; set; } = new();

    public Guid TaskId { get; set; }

    public JsonDocument TaskData { get; set; } = null!;
}
