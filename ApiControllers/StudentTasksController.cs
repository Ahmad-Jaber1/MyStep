using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentTasksController : ControllerBase
{
    private readonly IStudentTaskService _studentTaskService;
    private readonly ITaskSubmissionEvaluationService _taskSubmissionEvaluationService;

    public StudentTasksController(
        IStudentTaskService studentTaskService,
        ITaskSubmissionEvaluationService taskSubmissionEvaluationService)
    {
        _studentTaskService = studentTaskService;
        _taskSubmissionEvaluationService = taskSubmissionEvaluationService;
    }

    [HttpGet("by-student/{studentId:guid}")]
    public async Task<IActionResult> GetByStudent(Guid studentId)
    {
        var result = await _studentTaskService.GetByStudentAsync(studentId);
        return ToActionResult(result);
    }

    [HttpGet("{studentId:guid}/{taskId:guid}")]
    public async Task<IActionResult> Get(Guid studentId, Guid taskId)
    {
        var result = await _studentTaskService.GetAsync(studentId, taskId);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentTaskDto dto)
    {
        var result = await _studentTaskService.CreateAsync(dto);
        if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
        return CreatedAtAction(nameof(Get), new { studentId = result.Data!.StudentId, taskId = result.Data.TaskId }, result.Data);
    }

    [HttpPut("{studentId:guid}/{taskId:guid}")]
    public async Task<IActionResult> Update(Guid studentId, Guid taskId, [FromBody] UpdateStudentTaskDto dto)
    {
        var result = await _studentTaskService.UpdateAsync(studentId, taskId, dto);
        return ToActionResult(result);
    }

    [HttpPost("{studentId:guid}/{taskId:guid}/mark-passed")]
    public async Task<IActionResult> MarkAsPassed(Guid studentId, Guid taskId, [FromQuery] double? score = null)
    {
        var result = await _studentTaskService.MarkAsPassedAsync(studentId, taskId, score);
        return ToActionResult(result);
    }

    [HttpPost("{studentId:guid}/{taskId:guid}/evaluate")]
    public async Task<IActionResult> EvaluateSubmission(Guid studentId, Guid taskId, [FromBody] EvaluateTaskSubmissionRequestDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.RepositoryUrl))
        {
            return BadRequest("Repository URL is required.");
        }

        var result = await _taskSubmissionEvaluationService.EvaluateSubmissionAsync(studentId, taskId, dto.RepositoryUrl, dto.Ref);
        return ToActionResult(result);
    }

    [HttpDelete("{studentId:guid}/{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid studentId, Guid taskId)
    {
        var result = await _studentTaskService.DeleteAsync(studentId, taskId);
        return ToActionResult(result);
    }

    [HttpGet("by-student/{studentId:guid}/skill/{skillId:int}")]
    public async Task<IActionResult> GetByStudentAndSkill(Guid studentId, int skillId)
    {
        var result = await _studentTaskService.GetByStudentAndSkillAsync(studentId, skillId);
        return ToActionResult(result);
    }

    [HttpGet("{studentId:guid}/{taskId:guid}/details")]
    public async Task<IActionResult> GetTaskDetails(Guid studentId, Guid taskId)
    {
        var result = await _studentTaskService.GetTaskDetailsAsync(studentId, taskId);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Data);
        return IsNotFound(result.ErrorMessage) ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);
    }

    private static bool IsNotFound(string? message)
    {
        return message?.Contains("was not found", StringComparison.OrdinalIgnoreCase) == true;
    }
}
