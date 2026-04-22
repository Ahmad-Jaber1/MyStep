using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/task-generation")]
public class TaskSearchVectorsController : ControllerBase
{
    private readonly ITaskSearchVectorService _taskSearchVectorService;

    public TaskSearchVectorsController(ITaskSearchVectorService taskSearchVectorService)
    {
        _taskSearchVectorService = taskSearchVectorService;
    }

    [HttpPost("rebuild-all")]
    public async Task<IActionResult> RebuildAll()
    {
        var result = await _taskSearchVectorService.RebuildAllAsync();
        return ToActionResult(result);
    }

    [HttpPost("prepare")]
    public async Task<IActionResult> PrepareTaskGeneration([FromBody] PrepareTaskGenerationRequestDto dto)
    {
        var result = await _taskSearchVectorService.PrepareTaskGenerationAsync(dto.StudentId, dto.MainSkillId);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return IsNotFound(result.ErrorMessage) ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);
    }

    private static bool IsNotFound(string? message)
    {
        return message?.Contains("was not found", StringComparison.OrdinalIgnoreCase) == true;
    }
}