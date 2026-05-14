using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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
    private readonly ITaskGenerationRequestService _taskGenerationRequestService;
    private readonly IStudentRepo _studentRepo;
    private readonly ISupervisorStudentRepo _supervisorStudentRepo;

    public TaskSearchVectorsController(
        ITaskSearchVectorService taskSearchVectorService,
        ITaskGenerationRequestService taskGenerationRequestService,
        IStudentRepo studentRepo,
        ISupervisorStudentRepo supervisorStudentRepo)
    {
        _taskSearchVectorService = taskSearchVectorService;
        _taskGenerationRequestService = taskGenerationRequestService;
        _studentRepo = studentRepo;
        _supervisorStudentRepo = supervisorStudentRepo;
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
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GenerateTask([FromBody] PrepareTaskGenerationRequestDto dto)
    {
        var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var authenticatedStudentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        if (dto.StudentId != authenticatedStudentId)
        {
            return Forbid("You can only generate tasks for your own account.");
        }

        var student = await _studentRepo.GetByIdAsync(authenticatedStudentId);
        if (student is null)
        {
            return NotFound("Student was not found.");
        }

        if (student.SelectedPathId is int selectedPathId)
        {
            var approvedSupervisor = await _supervisorStudentRepo.GetApprovedByStudentAndPathAsync(authenticatedStudentId, selectedPathId);
            if (approvedSupervisor is not null)
            {
                var requestResult = await _taskGenerationRequestService.CreateAsync(authenticatedStudentId, new CreateTaskGenerationRequestDto
                {
                    MainSkillId = dto.MainSkillId
                });

                if (!requestResult.IsSuccess)
                {
                    return IsNotFound(requestResult.ErrorMessage)
                        ? NotFound(requestResult.ErrorMessage)
                        : BadRequest(requestResult.ErrorMessage);
                }

                return Accepted(new
                {
                    mode = "supervisor_request",
                    message = "Task generation request sent to supervisor for review.",
                    request = requestResult.Data
                });
            }
        }

        var result = await _taskSearchVectorService.GenerateTaskAsync(authenticatedStudentId, dto.MainSkillId);
        if (result.IsSuccess && result.Data is GenerateTaskResponseDto generateResponse)
        {
            return Ok(new { taskId = generateResponse.TaskId, taskData = generateResponse.TaskData });
        }

        return result.IsSuccess 
            ? Ok(result.Data) 
            : (IsNotFound(result.ErrorMessage) ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage));
    }

    [HttpPost("request")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> RequestTask([FromBody] CreateTaskGenerationRequestDto dto)
    {
        var studentIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _taskGenerationRequestService.CreateAsync(studentId, dto);
        return ToActionResult(result);
    }

    [HttpGet("requests")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> GetRequests()
    {
        var supervisorIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _taskGenerationRequestService.GetBySupervisorAsync(supervisorId);
        return ToActionResult(result);
    }

    [HttpGet("requests/{requestId:guid}/preview")]
    [Authorize(Roles = "Supervisor")]
    public IActionResult PreviewRequest(Guid requestId)
    {
        return BadRequest("Preview flow is deprecated. Use /api/task-generation/requests/{requestId}/approve-and-generate for one-step generation and persistence.");
    }

    [HttpPost("requests/{requestId:guid}/approve-and-generate")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> ApproveAndGenerateRequest(Guid requestId)
    {
        var supervisorIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _taskGenerationRequestService.ApproveAndGenerateAsync(supervisorId, requestId);
        return ToActionResult(result);
    }

    [HttpPost("requests/{requestId:guid}/approve-and-persist")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> ApproveAndPersistRequest(Guid requestId, [FromBody] JsonDocument body)
    {
        var supervisorIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId))
        {
            return Unauthorized("Invalid auth token.");
        }

        // Expect body to be { "generatedContent": { ... } } or a raw JSON object
        string generatedContent;
        try
        {
            // If the body has a property named generatedContent, extract it; otherwise take the raw object
            if (body.RootElement.ValueKind == JsonValueKind.Object && body.RootElement.TryGetProperty("generatedContent", out var prop))
            {
                generatedContent = prop.GetRawText();
            }
            else
            {
                generatedContent = body.RootElement.GetRawText();
            }
        }
        catch
        {
            return BadRequest("Invalid generated content payload.");
        }

        var result = await _taskGenerationRequestService.ApproveAndPersistAsync(supervisorId, requestId, generatedContent);
        return ToActionResult(result);
    }

    [HttpPost("requests/{requestId:guid}/approve")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> ApproveRequest(Guid requestId)
    {
        var supervisorIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _taskGenerationRequestService.ApproveAsync(supervisorId, requestId);
        return ToActionResult(result);
    }

    [HttpPost("requests/{requestId:guid}/reject")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> RejectRequest(Guid requestId)
    {
        var supervisorIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _taskGenerationRequestService.RejectAsync(supervisorId, requestId);
        return ToActionResult(result);
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