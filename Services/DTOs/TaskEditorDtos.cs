namespace Services.DTOs;

public class EditTaskObjectivesDto
{
    public List<int> ObjectiveIds { get; set; } = new List<int>();
}

public class EditTaskPrerequisitesDto
{
    public List<int> PrerequisiteObjectiveIds { get; set; } = new List<int>();
}

public class ValidationCriterionDto
{
    public int SkillId { get; set; }
    public string Criterion { get; set; } = string.Empty;
    public int? RelatedLearningObjective { get; set; }
}

public class EditTaskValidationsDto
{
    public List<ValidationCriterionDto> Validations { get; set; } = new List<ValidationCriterionDto>();
}

public class TaskEditResponseDto
{
    public Guid TaskId { get; set; }
    public bool SupervisorEdited { get; set; }
}
