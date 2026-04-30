using System;
using System.Collections.Generic;

namespace Models;

public class TaskSubmissionEvaluation
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid TaskId { get; set; }

    public string RepositoryUrl { get; set; } = string.Empty;

    public string? Reference { get; set; }

    public string OverallSummary { get; set; } = string.Empty;

    public string RawModelResponseJson { get; set; } = string.Empty;

    public int ValidationCount { get; set; }

    public int PassedValidationCount { get; set; }

    public int FailedValidationCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;

    public TaskItem Task { get; set; } = null!;

    public ICollection<TaskSubmissionEvaluationValidation> ValidationResults { get; set; } = new List<TaskSubmissionEvaluationValidation>();
}