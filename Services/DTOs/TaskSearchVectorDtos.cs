namespace Services.DTOs;

public class TaskSearchVectorRebuildResponseDto
{
    public int TotalTasks { get; set; }

    public int UpdatedTasks { get; set; }

    public List<TaskSearchVectorRebuildFailureDto> Failures { get; set; } = [];
}

public class TaskSearchVectorRebuildFailureDto
{
    public Guid TaskId { get; set; }

    public string ErrorMessage { get; set; } = null!;
}