namespace Services.DTOs;

public class CreateTaskPrerequisiteDto
{
    public Guid TaskId { get; set; }
    public int LearningObjectiveId { get; set; }
    public string? Justification { get; set; }
}

public class UpdateTaskPrerequisiteDto
{
    public Guid TaskId { get; set; }
    public int LearningObjectiveId { get; set; }
    public string? Justification { get; set; }
}