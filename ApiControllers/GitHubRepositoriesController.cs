using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/github-repositories")]
[Authorize]
public class GitHubRepositoriesController : ControllerBase
{
    private readonly IGitHubRepositoryCodeService _gitHubRepositoryCodeService;

    public GitHubRepositoriesController(IGitHubRepositoryCodeService gitHubRepositoryCodeService)
    {
        _gitHubRepositoryCodeService = gitHubRepositoryCodeService;
    }

    [HttpPost("flatten")]
    public async Task<IActionResult> FlattenRepository([FromBody] FlattenGitHubRepositoryRequestDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.RepositoryUrl))
        {
            return BadRequest("Repository URL is required.");
        }

        var result = await _gitHubRepositoryCodeService.FlattenRepositoryCodeAsync(dto.RepositoryUrl, dto.Ref);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return BadRequest(result.ErrorMessage);
    }
}