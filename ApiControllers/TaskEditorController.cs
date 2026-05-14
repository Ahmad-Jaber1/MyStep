using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;

namespace ApiControllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Roles = "Supervisor")]
public class TaskEditorController : ControllerBase
{
    private readonly ITaskEditService _taskEditService;

    public TaskEditorController(ITaskEditService taskEditService)
    {
        _taskEditService = taskEditService;
    }

    [HttpPut("{taskId:guid}/objectives")]
    public async Task<IActionResult> EditObjectives(Guid taskId, [FromBody] EditTaskObjectivesDto dto)
    {
        var supervisorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId)) return Unauthorized("Invalid auth token.");

        var result = await _taskEditService.EditObjectivesAsync(supervisorId, taskId, dto);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return Ok(result.Data);
    }

    [HttpPut("{taskId:guid}/prerequisites")]
    public async Task<IActionResult> EditPrerequisites(Guid taskId, [FromBody] EditTaskPrerequisitesDto dto)
    {
        var supervisorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId)) return Unauthorized("Invalid auth token.");

        var result = await _taskEditService.EditPrerequisitesAsync(supervisorId, taskId, dto);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return Ok(result.Data);
    }

    [HttpPut("{taskId:guid}/validations")]
    public async Task<IActionResult> EditValidations(Guid taskId, [FromBody] EditTaskValidationsDto dto)
    {
        var supervisorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId)) return Unauthorized("Invalid auth token.");

        var result = await _taskEditService.EditValidationsAsync(supervisorId, taskId, dto);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return Ok(result.Data);
    }
}
