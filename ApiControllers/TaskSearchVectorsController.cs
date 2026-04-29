using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/task-generation")]
[Authorize]
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

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateTask([FromBody] PrepareTaskGenerationRequestDto dto)
    {
        var result = await _taskSearchVectorService.GenerateTaskAsync(dto.StudentId, dto.MainSkillId);
        if (result.IsSuccess && result.Data is GenerateTaskResponseDto generateResponse)
        {
            return Ok(new { taskId = generateResponse.TaskId, taskData = generateResponse.TaskData });
        }

        return result.IsSuccess 
            ? Ok(result.Data) 
            : (IsNotFound(result.ErrorMessage) ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage));
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            if (result.Data is JsonDocument jsonDocument)
            {
                return Content(jsonDocument.RootElement.GetRawText(), "application/json");
            }

            return Ok(result.Data);
        }

        return IsNotFound(result.ErrorMessage) ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);
    }

    private static bool IsNotFound(string? message)
    {
        return message?.Contains("was not found", StringComparison.OrdinalIgnoreCase) == true;
    }
}