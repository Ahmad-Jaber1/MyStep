namespace Services.DTOs;

/// <summary>
/// Represents an enriched target learning objective with database ID and description.
/// </summary>
public class EnrichedTargetObjectiveDto
{
    public int Id { get; set; }
    
    public string Description { get; set; } = null!;
}

/// <summary>
/// Represents an enriched additional skill requirement with full details from the database.
/// </summary>
public class EnrichedAdditionalSkillDto
{
    public int SkillId { get; set; }
    
    public string SkillName { get; set; } = null!;
    
    public int LearningGoalId { get; set; }
    
    public string Justification { get; set; } = null!;
}
