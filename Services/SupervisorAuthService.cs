using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class SupervisorAuthService : ISupervisorAuthService
{
    private readonly ISupervisorRepo _supervisorRepo;
    private readonly IPathItemRepo _pathItemRepo;
    private readonly JwtOptions _jwtOptions;

    public SupervisorAuthService(
        ISupervisorRepo supervisorRepo,
        IPathItemRepo pathItemRepo,
        JwtOptions jwtOptions)
    {
        _supervisorRepo = supervisorRepo;
        _pathItemRepo = pathItemRepo;
        _jwtOptions = jwtOptions;
    }

    public async Task<Result<SupervisorAuthResponseDto>> SignUpAsync(SignUpSupervisorDto dto)
    {
        if (dto is null)
        {
            return Result<SupervisorAuthResponseDto>.Failure("Signup payload is required.");
        }

        var validationError = await ValidateSupervisorPayloadAsync(dto.FullName, dto.Email, dto.Password, dto.PathId, null);
        if (validationError is not null)
        {
            return Result<SupervisorAuthResponseDto>.Failure(validationError);
        }

        var normalizedEmail = TextNormalizer.NormalizeRequired(dto.Email)!.ToLowerInvariant();
        var existingSupervisor = await _supervisorRepo.GetByEmailAsync(normalizedEmail);
        if (existingSupervisor is not null)
        {
            return Result<SupervisorAuthResponseDto>.Failure("A supervisor with this email already exists.");
        }

        var supervisor = new Supervisor
        {
            Id = Guid.NewGuid(),
            FullName = TextNormalizer.NormalizeRequired(dto.FullName)!,
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(dto.Password!),
            PathId = dto.PathId!.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _supervisorRepo.AddAsync(supervisor);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);
        var token = GenerateToken(supervisor, expiresAt);

        return Result<SupervisorAuthResponseDto>.Success(new SupervisorAuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            Supervisor = MapToResponse(supervisor)
        });
    }

    public async Task<Result<SupervisorAuthResponseDto>> SignInAsync(SignInSupervisorDto dto)
    {
        if (dto is null)
        {
            return Result<SupervisorAuthResponseDto>.Failure("Signin payload is required.");
        }

        var email = TextNormalizer.NormalizeRequired(dto.Email);
        if (email is null)
        {
            return Result<SupervisorAuthResponseDto>.Failure("Email is required.");
        }

        var password = TextNormalizer.NormalizeRequired(dto.Password);
        if (password is null)
        {
            return Result<SupervisorAuthResponseDto>.Failure("Password is required.");
        }

        var supervisor = await _supervisorRepo.GetByEmailAsync(email.ToLowerInvariant());
        if (supervisor is null)
        {
            return Result<SupervisorAuthResponseDto>.Failure("Invalid email or password.");
        }

        if (!PasswordHasher.Verify(password, supervisor.PasswordHash))
        {
            return Result<SupervisorAuthResponseDto>.Failure("Invalid email or password.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);
        var token = GenerateToken(supervisor, expiresAt);

        return Result<SupervisorAuthResponseDto>.Success(new SupervisorAuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            Supervisor = MapToResponse(supervisor)
        });
    }

    public async Task<Result<bool>> SignOutAsync(Guid supervisorId)
    {
        if (supervisorId == Guid.Empty)
        {
            return Result<bool>.Failure("Supervisor id is required.");
        }

        var supervisor = await _supervisorRepo.GetByIdAsync(supervisorId);
        if (supervisor is null)
        {
            return Result<bool>.Failure("Supervisor was not found.");
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<SupervisorResponseDto>> GetByIdAsync(Guid supervisorId)
    {
        if (supervisorId == Guid.Empty)
        {
            return Result<SupervisorResponseDto>.Failure("Supervisor id is required.");
        }

        var supervisor = await _supervisorRepo.GetByIdAsync(supervisorId);
        if (supervisor is null)
        {
            return Result<SupervisorResponseDto>.Failure("Supervisor was not found.");
        }

        return Result<SupervisorResponseDto>.Success(MapToResponse(supervisor));
    }

    private string GenerateToken(Supervisor supervisor, DateTime expiresAt)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, supervisor.Id.ToString()),
            new(ClaimTypes.Name, supervisor.FullName),
            new(ClaimTypes.Email, supervisor.Email),
            new(ClaimTypes.Role, "Supervisor")
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static SupervisorResponseDto MapToResponse(Supervisor supervisor)
    {
        return new SupervisorResponseDto
        {
            Id = supervisor.Id,
            FullName = supervisor.FullName,
            Email = supervisor.Email,
            PathId = supervisor.PathId,
            CreatedAt = supervisor.CreatedAt
        };
    }

    private async Task<string?> ValidateSupervisorPayloadAsync(string? fullName, string? email, string? password, int? pathId, Guid? excludeSupervisorId)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Full name is required.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return "Password is required and must be at least 6 characters.";
        }

        if (pathId is null || pathId <= 0)
        {
            return "Path ID is required and must be greater than 0.";
        }

        var path = await _pathItemRepo.GetByIdAsync(pathId.Value);
        if (path is null)
        {
            return "Selected path does not exist.";
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.ToLowerInvariant();
            var existingSupervisor = await _supervisorRepo.GetByEmailAsync(normalizedEmail);
            if (existingSupervisor is not null && (excludeSupervisorId is null || existingSupervisor.Id != excludeSupervisorId.Value))
            {
                return "A supervisor with this email already exists.";
            }
        }

        return null;
    }
}
