using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ISupervisorAuthService _supervisorAuthService;
    private readonly ISupervisorService _supervisorService;

    public AuthController(
        IStudentService studentService,
        ISupervisorAuthService supervisorAuthService,
        ISupervisorService supervisorService)
    {
        _studentService = studentService;
        _supervisorAuthService = supervisorAuthService;
        _supervisorService = supervisorService;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> SignUp([FromBody] SignUpStudentDto dto)
    {
        var result = await _studentService.SignUpAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpPost("signin")]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn([FromBody] SignInStudentDto dto)
    {
        var result = await _studentService.SignInAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpPost("supervisor-signup")]
    [AllowAnonymous]
    public async Task<IActionResult> SupervisorSignUp([FromBody] SignUpSupervisorDto dto)
    {
        var result = await _supervisorAuthService.SignUpAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpPost("supervisor-signin")]
    [AllowAnonymous]
    public async Task<IActionResult> SupervisorSignIn([FromBody] SignInSupervisorDto dto)
    {
        var result = await _supervisorAuthService.SignInAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpPost("signout")]
    [Authorize]
    public async Task<IActionResult> SignOutStudent()
    {
        var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _studentService.SignOutAsync(studentId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(new { success = true });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _studentService.GetCurrentStudentAsync(studentId);
        if (!result.IsSuccess)
        {
            return NotFound(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpGet("supervisor-me")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> SupervisorMe()
    {
        var supervisorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _supervisorAuthService.GetByIdAsync(supervisorId);
        if (!result.IsSuccess)
        {
            return NotFound(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpPost("welcome-assessment")]
    [Authorize]
    public async Task<IActionResult> SubmitWelcomeAssessment([FromBody] SubmitWelcomeAssessmentDto dto)
    {
        var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _studentService.SubmitWelcomeAssessmentAsync(studentId, dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(new { success = true });
    }

    [HttpPost("supervisor-signout")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> SignOutSupervisor()
    {
        var supervisorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(supervisorIdClaim, out var supervisorId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _supervisorAuthService.SignOutAsync(supervisorId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(new { success = true });
    }

    [HttpGet("supervisor-requests")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetSupervisorRequests()
    {
        var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _supervisorService.GetPendingRequestsAsync(studentId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpPost("supervisor-requests/{supervisorId:guid}/{pathId:int}/approve")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ApproveSupervisorRequest(Guid supervisorId, int pathId)
    {
        var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _supervisorService.ApproveAsync(studentId, supervisorId, pathId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpPost("supervisor-requests/{supervisorId:guid}/{pathId:int}/reject")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> RejectSupervisorRequest(Guid supervisorId, int pathId)
    {
        var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _supervisorService.RejectAsync(studentId, supervisorId, pathId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpPut("selected-path")]
    [Authorize]
    public async Task<IActionResult> SelectPath([FromBody] SelectPathDto dto)
    {
        var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(studentIdClaim, out var studentId))
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _studentService.SelectPathAsync(studentId, dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }
}
