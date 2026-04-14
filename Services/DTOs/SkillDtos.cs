namespace Services.DTOs;

public class CreateSkillDto
{
    public int PathId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateSkillDto
{
    public int PathId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}