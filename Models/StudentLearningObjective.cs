using System;

namespace Models
{
    public class StudentLearningObjective
    {
        public Guid StudentId { get; set; }

        public int LearningObjectiveId { get; set; }

        

        public double Score { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public Student Student { get; set; } = null!;

        public LearningObjective LearningObjective { get; set; } = null!;
    }
}