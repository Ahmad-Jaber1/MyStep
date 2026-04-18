namespace Services.DTOs;

public class CreateStudentLearningObjectiveDto
{
    public Guid StudentId { get; set; }
    public int LearningObjectiveId { get; set; }
    public double Score { get; set; }
}

public class UpdateStudentLearningObjectiveDto
{
    public double Score { get; set; }
}

public class SubmitWelcomeAssessmentDto
{
    public List<WelcomeAssessmentItemDto> Objectives { get; set; } = new();
}

public class WelcomeAssessmentItemDto
{
    public int LearningObjectiveId { get; set; }
    public int Score { get; set; }
}
