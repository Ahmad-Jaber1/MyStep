namespace Services.DTOs;

public class CreateTaskTargetDto
{
    public Guid TaskId { get; set; }
    public int LearningObjectiveId { get; set; }
}

public class UpdateTaskTargetDto
{
    public Guid TaskId { get; set; }
    public int LearningObjectiveId { get; set; }
}