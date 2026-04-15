namespace Services.DTOs;

public class TaskPrerequisiteResponseDto
{
    public Guid TaskId { get; set; }
    public int LearningObjectiveId { get; set; }
    public string Justification { get; set; } = null!;
}