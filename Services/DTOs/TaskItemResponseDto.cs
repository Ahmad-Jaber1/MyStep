namespace Services.DTOs;

public class TaskItemResponseDto
{
    public Guid Id { get; set; }
    public int PathId { get; set; }
    public int MainSkillId { get; set; }
    public string TaskData { get; set; } = null!;
    public float[] SearchVector { get; set; } = [];
}