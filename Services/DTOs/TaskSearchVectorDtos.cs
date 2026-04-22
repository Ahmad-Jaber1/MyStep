namespace Services.DTOs;

using System.Text.Json;

public class TaskSearchVectorRebuildResponseDto
{
    public int TotalTasks { get; set; }

    public int UpdatedTasks { get; set; }

    public List<TaskSearchVectorRebuildFailureDto> Failures { get; set; } = [];
}

public class TaskSearchVectorRebuildFailureDto
{
    public Guid TaskId { get; set; }

    public string ErrorMessage { get; set; } = null!;
}

public class PrepareTaskGenerationRequestDto
{
    public Guid StudentId { get; set; }

    public int MainSkillId { get; set; }
}

public class TaskGenerationPreparationResponseDto
{
    public Guid StudentId { get; set; }

    public int MainSkillId { get; set; }

    public double MasteryThreshold { get; set; }

    public string InputText { get; set; } = null!;

    public float[] QueryVector { get; set; } = [];

    public List<string> PrerequisiteLearningObjectives { get; set; } = [];

    public List<string> TargetLearningObjectives { get; set; } = [];

    public List<StudentTaskMatchResponseDto> TopTaskMatches { get; set; } = [];
}

public class StudentTaskMatchResponseDto
{
    public Guid TaskId { get; set; }

    public int PathId { get; set; }

    public int MainSkillId { get; set; }

    public JsonDocument TaskData { get; set; } = null!;

    public double SimilarityScore { get; set; }
}