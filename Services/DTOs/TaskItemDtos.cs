using System.Text.Json;

namespace Services.DTOs;

public class CreateTaskItemDto
{
    public int PathId { get; set; }
    public int MainSkillId { get; set; }

    public JsonDocument? TaskData { get; set; }

    public float[]? SearchVector { get; set; }
}

public class UpdateTaskItemDto
{
    public int PathId { get; set; }
    public int MainSkillId { get; set; }

    public JsonDocument? TaskData { get; set; }

    public float[]? SearchVector { get; set; }
}