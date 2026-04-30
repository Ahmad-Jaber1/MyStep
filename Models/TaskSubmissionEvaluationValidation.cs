using System;

namespace Models;

public class TaskSubmissionEvaluationValidation
{
    public Guid Id { get; set; }

    public Guid TaskSubmissionEvaluationId { get; set; }

    public int ValidationId { get; set; }

    public int SkillId { get; set; }

    public int ObjectiveId { get; set; }

    public string ValidationString { get; set; } = string.Empty;

    public bool IsPass { get; set; }

    public string WhyNotPass { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TaskSubmissionEvaluation TaskSubmissionEvaluation { get; set; } = null!;
}