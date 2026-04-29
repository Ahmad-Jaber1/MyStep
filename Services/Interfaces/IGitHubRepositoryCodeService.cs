using Services.DTOs;
using Shared.Results;

namespace Services.Interfaces;

public interface IGitHubRepositoryCodeService
{
    Task<Result<FlattenGitHubRepositoryResponseDto>> FlattenRepositoryCodeAsync(string repositoryUrl, string? reference = null);
}