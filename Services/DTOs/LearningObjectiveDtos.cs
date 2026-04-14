namespace Services.DTOs;

public class CreateLearningObjectiveDto
{
    public int SkillId { get; set; }
    public string? Description { get; set; }
}

public class UpdateLearningObjectiveDto
{
    public int SkillId { get; set; }
    public string? Description { get; set; }
}