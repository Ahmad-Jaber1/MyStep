using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class TaskTargetsController : ControllerBase
{
    private readonly ITaskTargetService _taskTargetService;

    public TaskTargetsController(ITaskTargetService taskTargetService)
    {
        _taskTargetService = taskTargetService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _taskTargetService.GetAllAsync();
        return ToActionResult(result);
    }

    [HttpGet("{taskId:guid}/{learningObjectiveId:int}")]
    public async Task<IActionResult> GetById(Guid taskId, int learningObjectiveId)
    {
        var result = await _taskTargetService.GetByIdAsync(taskId, learningObjectiveId);
        return ToActionResult(result);
    }

    [HttpGet("by-task/{taskId:guid}")]
    public async Task<IActionResult> GetByTaskId(Guid taskId)
    {
        var result = await _taskTargetService.GetByTaskIdAsync(taskId);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskTargetDto dto)
    {
        var result = await _taskTargetService.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { taskId = result.Data!.TaskId, learningObjectiveId = result.Data.LearningObjectiveId },
            result.Data);
    }

    [HttpPut("{taskId:guid}/{learningObjectiveId:int}")]
    public async Task<IActionResult> Update(Guid taskId, int learningObjectiveId, [FromBody] UpdateTaskTargetDto dto)
    {
        var result = await _taskTargetService.UpdateAsync(taskId, learningObjectiveId, dto);
        return ToActionResult(result);
    }

    [HttpDelete("{taskId:guid}/{learningObjectiveId:int}")]
    public async Task<IActionResult> Delete(Guid taskId, int learningObjectiveId)
    {
        var result = await _taskTargetService.DeleteAsync(taskId, learningObjectiveId);
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