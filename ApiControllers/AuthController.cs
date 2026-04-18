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

    public AuthController(IStudentService studentService)
    {
        _studentService = studentService;
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
}
