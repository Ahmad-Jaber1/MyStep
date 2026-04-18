namespace Services.DTOs;

public class StudentLearningObjectiveResponseDto
{
    public Guid StudentId { get; set; }
    public int LearningObjectiveId { get; set; }
    public double Score { get; set; }
    public DateTime LastUpdated { get; set; }
}
