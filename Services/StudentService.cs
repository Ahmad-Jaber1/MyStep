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

public class StudentService : IStudentService
{
    private readonly IStudentRepo _studentRepo;
    private readonly IPathItemRepo _pathItemRepo;
    private readonly JwtOptions _jwtOptions;

    public StudentService(IStudentRepo studentRepo, IPathItemRepo pathItemRepo, JwtOptions jwtOptions)
    {
        _studentRepo = studentRepo;
        _pathItemRepo = pathItemRepo;
        _jwtOptions = jwtOptions;
    }

    public async Task<Result<List<StudentResponseDto>>> GetAllAsync()
    {
        var students = await _studentRepo.GetAllAsync();
        return Result<List<StudentResponseDto>>.Success(students.Select(MapToResponse).ToList());
    }

    public async Task<Result<StudentResponseDto>> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return Result<StudentResponseDto>.Failure("Student id is required.");
        }

        var student = await _studentRepo.GetByIdAsync(id);
        if (student is null)
        {
            return Result<StudentResponseDto>.Failure($"Student with id {id} was not found.");
        }

        return Result<StudentResponseDto>.Success(MapToResponse(student));
    }

    public async Task<Result<StudentResponseDto>> CreateAsync(CreateStudentDto dto)
    {
        if (dto is null)
        {
            return Result<StudentResponseDto>.Failure("Student payload is required.");
        }

        var validationError = await ValidateStudentPayloadAsync(dto.FullName, dto.Email, dto.Password, dto.SelectedPathId, null);
        if (validationError is not null)
        {
            return Result<StudentResponseDto>.Failure(validationError);
        }

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FullName = TextNormalizer.NormalizeRequired(dto.FullName)!,
            Email = TextNormalizer.NormalizeRequired(dto.Email)!.ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(dto.Password!),
            SelectedPathId = dto.SelectedPathId,
            CreatedAt = DateTime.UtcNow
        };

        await _studentRepo.AddAsync(student);
        return Result<StudentResponseDto>.Success(MapToResponse(student));
    }

    public async Task<Result<StudentResponseDto>> UpdateAsync(Guid id, UpdateStudentDto dto)
    {
        if (id == Guid.Empty)
        {
            return Result<StudentResponseDto>.Failure("Student id is required.");
        }

        if (dto is null)
        {
            return Result<StudentResponseDto>.Failure("Student payload is required.");
        }

        var existingStudent = await _studentRepo.GetByIdAsync(id);
        if (existingStudent is null)
        {
            return Result<StudentResponseDto>.Failure($"Student with id {id} was not found.");
        }

        var validationError = await ValidateStudentPayloadAsync(dto.FullName, dto.Email, dto.Password, dto.SelectedPathId, id, isPasswordRequired: false);
        if (validationError is not null)
        {
            return Result<StudentResponseDto>.Failure(validationError);
        }

        existingStudent.FullName = TextNormalizer.NormalizeRequired(dto.FullName)!;
        existingStudent.Email = TextNormalizer.NormalizeRequired(dto.Email)!.ToLowerInvariant();
        existingStudent.SelectedPathId = dto.SelectedPathId;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            existingStudent.PasswordHash = PasswordHasher.Hash(dto.Password.Trim());
        }

        await _studentRepo.UpdateAsync(existingStudent);
        return Result<StudentResponseDto>.Success(MapToResponse(existingStudent));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return Result<bool>.Failure("Student id is required.");
        }

        var existingStudent = await _studentRepo.GetByIdAsync(id);
        if (existingStudent is null)
        {
            return Result<bool>.Failure($"Student with id {id} was not found.");
        }

        await _studentRepo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    public async Task<Result<StudentResponseDto>> SignUpAsync(SignUpStudentDto dto)
    {
        if (dto is null)
        {
            return Result<StudentResponseDto>.Failure("Signup payload is required.");
        }

        var createResult = await CreateAsync(new CreateStudentDto
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Password = dto.Password,
            SelectedPathId = null
        });

        return createResult;
    }

    public async Task<Result<AuthResponseDto>> SignInAsync(SignInStudentDto dto)
    {
        if (dto is null)
        {
            return Result<AuthResponseDto>.Failure("Signin payload is required.");
        }

        var email = TextNormalizer.NormalizeRequired(dto.Email);
        if (email is null)
        {
            return Result<AuthResponseDto>.Failure("Email is required.");
        }

        var password = TextNormalizer.NormalizeRequired(dto.Password);
        if (password is null)
        {
            return Result<AuthResponseDto>.Failure("Password is required.");
        }

        var student = await _studentRepo.GetByEmailAsync(email.ToLowerInvariant());
        if (student is null)
        {
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        if (!PasswordHasher.Verify(password, student.PasswordHash))
        {
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);
        var token = GenerateToken(student, expiresAt);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            Student = MapToResponse(student)
        });
    }

    public async Task<Result<bool>> SignOutAsync(Guid studentId)
    {
        if (studentId == Guid.Empty)
        {
            return Result<bool>.Failure("Student id is required.");
        }

        var student = await _studentRepo.GetByIdAsync(studentId);
        if (student is null)
        {
            return Result<bool>.Failure("Student was not found.");
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<CurrentStudentDto>> GetCurrentStudentAsync(Guid studentId)
    {
        if (studentId == Guid.Empty)
        {
            return Result<CurrentStudentDto>.Failure("Student id is required.");
        }

        var student = await _studentRepo.GetByIdAsync(studentId);
        if (student is null)
        {
            return Result<CurrentStudentDto>.Failure("Student was not found.");
        }

        return Result<CurrentStudentDto>.Success(new CurrentStudentDto
        {
            Id = student.Id,
            FullName = student.FullName,
            Email = student.Email
        });
    }

    private async Task<string?> ValidateStudentPayloadAsync(
        string? fullName,
        string? email,
        string? password,
        int? selectedPathId,
        Guid? currentStudentId,
        bool isPasswordRequired = true)
    {
        var normalizedFullName = TextNormalizer.NormalizeRequired(fullName);
        if (normalizedFullName is null)
        {
            return "Student full name is required.";
        }

        var normalizedEmail = TextNormalizer.NormalizeRequired(email);
        if (normalizedEmail is null)
        {
            return "Student email is required.";
        }

        var existingStudent = await _studentRepo.GetByEmailAsync(normalizedEmail.ToLowerInvariant());
        if (existingStudent is not null && existingStudent.Id != currentStudentId)
        {
            return "Student email is already registered.";
        }

        if (isPasswordRequired)
        {
            var normalizedPassword = TextNormalizer.NormalizeRequired(password);
            if (normalizedPassword is null)
            {
                return "Student password is required.";
            }

            if (normalizedPassword.Length < 8)
            {
                return "Student password must be at least 8 characters.";
            }
        }
        else if (!string.IsNullOrWhiteSpace(password) && password.Trim().Length < 8)
        {
            return "Student password must be at least 8 characters.";
        }

        if (selectedPathId is not null)
        {
            if (selectedPathId <= 0)
            {
                return "Selected path id must be greater than zero.";
            }

            var path = await _pathItemRepo.GetByIdAsync(selectedPathId.Value);
            if (path is null)
            {
                return $"Path with id {selectedPathId} was not found.";
            }
        }

        return null;
    }

    private string GenerateToken(Student student, DateTime expiresAt)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, student.Id.ToString()),
            new(ClaimTypes.Name, student.FullName),
            new(ClaimTypes.Email, student.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static StudentResponseDto MapToResponse(Student student)
    {
        return new StudentResponseDto
        {
            Id = student.Id,
            FullName = student.FullName,
            Email = student.Email,
            SelectedPathId = student.SelectedPathId,
            CreatedAt = student.CreatedAt
        };
    }
}
