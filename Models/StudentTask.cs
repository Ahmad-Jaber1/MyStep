using System;
using System.Collections.Generic;

namespace Models
{
    public class StudentTask
    {
        public Guid StudentId { get; set; }

        public Guid TaskId { get; set; }

        // Number of this task for the student within the task's main skill (1-based)
        public int NumberInMainSkill { get; set; }

        // Whether the student has passed this task
        public bool Passed { get; set; } = false;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public double? Score { get; set; }

        // Navigation
        public Student Student { get; set; } = null!;

        public TaskItem Task { get; set; } = null!;
    }
}
