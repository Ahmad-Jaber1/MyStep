namespace Services.DTOs;

public class SkillResponseDto
{
    public int Id { get; set; }
    public int PathId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}