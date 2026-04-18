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
