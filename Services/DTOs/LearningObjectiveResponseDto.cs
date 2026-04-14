namespace Services.DTOs;

public class LearningObjectiveResponseDto
{
    public int Id { get; set; }
    public int SkillId { get; set; }
    public string Description { get; set; } = null!;
}