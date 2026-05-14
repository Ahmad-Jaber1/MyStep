using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Supervisor")]
public class SupervisorController : ControllerBase
{
    private readonly ISupervisorService _supervisorService;
    private readonly IStudentTaskService _studentTaskService;
    private readonly ISupervisorStudentRepo _supervisorStudentRepo;
    private readonly IStudentRepo _studentRepo;

    public SupervisorController(
        ISupervisorService supervisorService,
        IStudentTaskService studentTaskService,
        ISupervisorStudentRepo supervisorStudentRepo,
        IStudentRepo studentRepo)
    {
        _supervisorService = supervisorService;
        _studentTaskService = studentTaskService;
        _supervisorStudentRepo = supervisorStudentRepo;
        _studentRepo = studentRepo;
    }

    [HttpPost("add-student")]
    public async Task<IActionResult> AddStudent([FromBody] AddSupervisorStudentDto dto)
    {
        var supervisorId = GetSupervisorId();
        if (supervisorId is null)
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _supervisorService.AddStudentAsync(supervisorId.Value, dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpGet("my-students")]
    public async Task<IActionResult> GetMyStudents()
    {
        var supervisorId = GetSupervisorId();
        if (supervisorId is null)
        {
            return Unauthorized("Invalid auth token.");
        }

        var result = await _supervisorService.GetMyStudentsAsync(supervisorId.Value);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    [HttpGet("students/{studentId:guid}/skills/{skillId:int}/history")]
    public async Task<IActionResult> GetStudentSkillHistory(Guid studentId, int skillId)
    {
        var supervisorId = GetSupervisorId();
        if (supervisorId is null)
        {
            return Unauthorized("Invalid auth token.");
        }

        // Verify that supervisor is authorized to view this student's history
        var student = await _studentRepo.GetByIdAsync(studentId);
        if (student is null)
        {
            return NotFound("Student not found.");
        }

        if (student.SelectedPathId is null)
        {
            return BadRequest("Student has not selected a path.");
        }

        var supervisorRelation = await _supervisorStudentRepo.GetApprovedByStudentAndPathAsync(studentId, student.SelectedPathId.Value);
        if (supervisorRelation is null || supervisorRelation.SupervisorId != supervisorId)
        {
            return Forbid("You are not authorized to view this student's history.");
        }

        var result = await _studentTaskService.GetByStudentAndSkillAsync(studentId, skillId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Data);
    }

    private Guid? GetSupervisorId()
    {
        var supervisorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(supervisorIdClaim, out var supervisorId) ? supervisorId : null;
    }
}
