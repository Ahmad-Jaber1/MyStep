namespace Services.DTOs;

public class SignUpSupervisorDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int? PathId { get; set; }
}

public class SignInSupervisorDto
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class SupervisorResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int PathId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupervisorAuthResponseDto
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public SupervisorResponseDto Supervisor { get; set; } = null!;
}
