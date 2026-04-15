using System.Text.Json.Serialization;

public  class AdditionalSkillPayload
{
    [JsonPropertyName("skill_name")]
    public string SkillName { get; set; } = string.Empty;

    [JsonPropertyName("used_learning_goal")]
    public int UsedLearningGoal { get; set; }

    [JsonPropertyName("justification")]
    public string Justification { get; set; } = string.Empty;
}