using System;

namespace Models
{
    public class TaskGenerationRequest
    {
        public Guid Id { get; set; }

        public Guid StudentId { get; set; }

        public Guid SupervisorId { get; set; }

        public int PathId { get; set; }

        public int MainSkillId { get; set; }

        public TaskGenerationRequestStatus Status { get; set; } = TaskGenerationRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Student? Student { get; set; }

        public Supervisor? Supervisor { get; set; }

        public PathItem? Path { get; set; }
    }

    public enum TaskGenerationRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
