namespace Services.DTOs;

public class EvaluateTaskSubmissionRequestDto
{
    public string RepositoryUrl { get; set; } = string.Empty;

    public string? Ref { get; set; }
}

public class TaskValidationEvaluationDto
{
    public int ValidationId { get; set; }

    public int SkillId { get; set; }

    public int ObjectiveId { get; set; }

    public string ValidationString { get; set; } = string.Empty;

    public bool IsPass { get; set; }

    public string WhyNotPass { get; set; } = string.Empty;
}

public class TaskSubmissionEvaluationResponseDto
{
    public Guid EvaluationId { get; set; }

    public Guid StudentId { get; set; }

    public Guid TaskId { get; set; }

    public DateTime EvaluatedAt { get; set; }

    public string OverallSummary { get; set; } = string.Empty;

    public List<TaskValidationEvaluationDto> ValidationResults { get; set; } = [];

    public List<string> StudentGoodPoints { get; set; } = [];

    public List<string> StudentWeaknesses { get; set; } = [];

    public List<string> TopicsToRead { get; set; } = [];
}
