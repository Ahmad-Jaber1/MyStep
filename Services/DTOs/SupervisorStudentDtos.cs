using Models;

namespace Services.DTOs;

public class AddSupervisorStudentDto
{
    public string? StudentEmail { get; set; }
}

public class SupervisorStudentResponseDto
{
    public Guid SupervisorId { get; set; }
    public string SupervisorFullName { get; set; } = string.Empty;
    public string SupervisorEmail { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int PathId { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
