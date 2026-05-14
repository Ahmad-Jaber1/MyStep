using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface ISupervisorAuthService
{
    Task<Result<SupervisorAuthResponseDto>> SignUpAsync(SignUpSupervisorDto dto);
    Task<Result<SupervisorAuthResponseDto>> SignInAsync(SignInSupervisorDto dto);
    Task<Result<bool>> SignOutAsync(Guid supervisorId);
    Task<Result<SupervisorResponseDto>> GetByIdAsync(Guid supervisorId);
}
