using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface IPathItemService
{
    Task<Result<List<PathItemResponseDto>>> GetAllAsync();
    Task<Result<PathItemResponseDto>> GetByIdAsync(int id);
    Task<Result<PathItemResponseDto>> CreateAsync(CreatePathItemDto dto);
    Task<Result<PathItemResponseDto>> UpdateAsync(int id, UpdatePathItemDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}