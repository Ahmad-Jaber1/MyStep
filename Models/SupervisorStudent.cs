using System;

namespace Models
{
    public class SupervisorStudent
    {
        public Guid SupervisorId { get; set; }

        public Guid StudentId { get; set; }

        public int PathId { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public Supervisor? Supervisor { get; set; }

        public Student? Student { get; set; }

        public PathItem? Path { get; set; }
    }

    public enum ApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
