using System;
using System.Collections.Generic;

namespace Models
{
    public class Student
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public int? SelectedPathId { get; set; }

        public bool HasCompletedWelcomeAssessment { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public PathItem? SelectedPath { get; set; }

        public ICollection<StudentLearningObjective> StudentLearningObjectives { get; set; }
            = new List<StudentLearningObjective>();

        public ICollection<StudentTask> StudentTasks { get; set; } = new List<StudentTask>();
    }
}