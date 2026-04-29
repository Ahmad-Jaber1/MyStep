using System.Text.Json;

namespace Services.DTOs;

public class TaskGenerationResponseDto
{
    public TaskGenerationPreparationResponseDto Preparation { get; set; } = null!;

    public string RawContent { get; set; } = null!;

    public JsonDocument GeneratedTask { get; set; } = null!;
}

public class GenerateTaskResponseDto
{
    public Guid TaskId { get; set; }
    
    public JsonDocument TaskData { get; set; } = null!;
}