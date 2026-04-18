namespace Services.DTOs;

public class CreateStudentDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int? SelectedPathId { get; set; }
}

public class UpdateStudentDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int? SelectedPathId { get; set; }
}

public class SignUpStudentDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class SignInStudentDto
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}
