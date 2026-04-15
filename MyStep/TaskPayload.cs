using System.Text.Json.Serialization;

public  class TaskPayload
{
    [JsonPropertyName("task_name")]
    public string TaskName { get; set; } = string.Empty;

    [JsonPropertyName("skill_category")]
    public string SkillCategory { get; set; } = string.Empty;

    [JsonPropertyName("scenario")]
    public object? Scenario { get; set; }

    [JsonPropertyName("targeted_objectives")]
    public List<int> TargetedObjectives { get; set; } = [];

    [JsonPropertyName("additional_skills_required")]
    public List<AdditionalSkillPayload> AdditionalSkillsRequired { get; set; } = [];
}
