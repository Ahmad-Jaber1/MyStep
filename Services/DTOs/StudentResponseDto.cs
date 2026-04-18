namespace Services.DTOs;

public class StudentResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public int? SelectedPathId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CurrentStudentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
}

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public StudentResponseDto Student { get; set; } = null!;
}
