using Models;
using Services.Common;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace Services;

public class PathItemService : IPathItemService
{
    private readonly IPathItemRepo _pathItemRepo;

    public PathItemService(IPathItemRepo pathItemRepo)
    {
        _pathItemRepo = pathItemRepo;
    }

    public async Task<Result<List<PathItemResponseDto>>> GetAllAsync()
    {
        var paths = await _pathItemRepo.GetAllAsync();
        return Result<List<PathItemResponseDto>>.Success(paths.Select(MapToResponse).ToList());
    }

    public async Task<Result<PathItemResponseDto>> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return Result<PathItemResponseDto>.Failure("Path id must be greater than zero.");
        }

        var pathItem = await _pathItemRepo.GetByIdAsync(id);
        if (pathItem is null)
        {
            return Result<PathItemResponseDto>.Failure($"Path with id {id} was not found.");
        }

        return Result<PathItemResponseDto>.Success(MapToResponse(pathItem));
    }

    public async Task<Result<PathItemResponseDto>> CreateAsync(CreatePathItemDto dto)
    {
        if (dto is null)
        {
            return Result<PathItemResponseDto>.Failure("Path payload is required.");
        }

        var normalizedName = TextNormalizer.NormalizeRequired(dto.Name);
        if (normalizedName is null)
        {
            return Result<PathItemResponseDto>.Failure("Path name is required.");
        }

        var entity = new PathItem
        {
            Name = normalizedName,
            Description = TextNormalizer.NormalizeOptional(dto.Description)
        };

        await _pathItemRepo.AddAsync(entity);
        return Result<PathItemResponseDto>.Success(MapToResponse(entity));
    }

    public async Task<Result<PathItemResponseDto>> UpdateAsync(int id, UpdatePathItemDto dto)
    {
        if (id <= 0)
        {
            return Result<PathItemResponseDto>.Failure("Path id must be greater than zero.");
        }

        if (dto is null)
        {
            return Result<PathItemResponseDto>.Failure("Path payload is required.");
        }

        var existingPath = await _pathItemRepo.GetByIdAsync(id);
        if (existingPath is null)
        {
            return Result<PathItemResponseDto>.Failure($"Path with id {id} was not found.");
        }

        var normalizedName = TextNormalizer.NormalizeRequired(dto.Name);
        if (normalizedName is null)
        {
            return Result<PathItemResponseDto>.Failure("Path name is required.");
        }

        existingPath.Name = normalizedName;
        existingPath.Description = TextNormalizer.NormalizeOptional(dto.Description);

        await _pathItemRepo.UpdateAsync(existingPath);
        return Result<PathItemResponseDto>.Success(MapToResponse(existingPath));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            return Result<bool>.Failure("Path id must be greater than zero.");
        }

        var existingPath = await _pathItemRepo.GetByIdAsync(id);
        if (existingPath is null)
        {
            return Result<bool>.Failure($"Path with id {id} was not found.");
        }

        await _pathItemRepo.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    private static PathItemResponseDto MapToResponse(PathItem path)
    {
        return new PathItemResponseDto
        {
            Id = path.Id,
            Name = path.Name,
            Description = path.Description
        };
    }
}