using System;
using System.Text.Json;

namespace Services.DTOs
{
    public class CreateStudentTaskDto
    {
        public Guid StudentId { get; set; }
        public Guid TaskId { get; set; }
    }

    public class UpdateStudentTaskDto
    {
        public bool? Passed { get; set; }
        public double? Score { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class StudentTaskResponseDto
    {
        public Guid StudentId { get; set; }
        public Guid TaskId { get; set; }
        public int NumberInMainSkill { get; set; }
        public bool Passed { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double? Score { get; set; }
    }

    // Task History DTOs
    public class TaskHistorySummaryDto
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public int NumberInSkill { get; set; }
        public bool Passed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double? Score { get; set; }
        // Score calculation: (passed validations / total validations) * 100
        public int PassedValidations { get; set; }
        public int TotalValidations { get; set; }
    }

    public class TaskDetailsResponseDto
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public int NumberInSkill { get; set; }
        public bool Passed { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double? Score { get; set; }
        public string RepositoryUrl { get; set; } = string.Empty;
        public string? RepositoryRef { get; set; }
        public DateTime? EvaluatedAt { get; set; }
        public string OverallSummary { get; set; } = string.Empty;
        public List<TaskValidationDetailDto> ValidationResults { get; set; } = [];
        public List<string> StudentGoodPoints { get; set; } = [];
        public List<string> StudentWeaknesses { get; set; } = [];
        public List<string> TopicsToRead { get; set; } = [];
    }

    public class TaskValidationDetailDto
    {
        public int ValidationId { get; set; }
        public int SkillId { get; set; }
        public int ObjectiveId { get; set; }
        public string ValidationString { get; set; } = string.Empty;
        public bool IsPass { get; set; }
        public string WhyNotPass { get; set; } = string.Empty;
    }
}
