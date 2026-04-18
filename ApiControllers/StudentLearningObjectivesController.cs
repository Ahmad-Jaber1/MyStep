using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentLearningObjectivesController : ControllerBase
{
    private readonly IStudentLearningObjectiveService _studentLearningObjectiveService;

    public StudentLearningObjectivesController(IStudentLearningObjectiveService studentLearningObjectiveService)
    {
        _studentLearningObjectiveService = studentLearningObjectiveService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _studentLearningObjectiveService.GetAllAsync();
        return ToActionResult(result);
    }

    [HttpGet("{studentId:guid}/{learningObjectiveId:int}")]
    public async Task<IActionResult> GetById(Guid studentId, int learningObjectiveId)
    {
        var result = await _studentLearningObjectiveService.GetByIdAsync(studentId, learningObjectiveId);
        return ToActionResult(result);
    }

    [HttpGet("by-student/{studentId:guid}")]
    public async Task<IActionResult> GetByStudentId(Guid studentId)
    {
        var result = await _studentLearningObjectiveService.GetByStudentIdAsync(studentId);
        return ToActionResult(result);
    }

    [HttpGet("by-learning-objective/{learningObjectiveId:int}")]
    public async Task<IActionResult> GetByLearningObjectiveId(int learningObjectiveId)
    {
        var result = await _studentLearningObjectiveService.GetByLearningObjectiveIdAsync(learningObjectiveId);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentLearningObjectiveDto dto)
    {
        var result = await _studentLearningObjectiveService.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetById), new { studentId = result.Data!.StudentId, learningObjectiveId = result.Data.LearningObjectiveId }, result.Data);
    }

    [HttpPut("{studentId:guid}/{learningObjectiveId:int}")]
    public async Task<IActionResult> Update(Guid studentId, int learningObjectiveId, [FromBody] UpdateStudentLearningObjectiveDto dto)
    {
        var result = await _studentLearningObjectiveService.UpdateAsync(studentId, learningObjectiveId, dto);
        return ToActionResult(result);
    }

    [HttpDelete("{studentId:guid}/{learningObjectiveId:int}")]
    public async Task<IActionResult> Delete(Guid studentId, int learningObjectiveId)
    {
        var result = await _studentLearningObjectiveService.DeleteAsync(studentId, learningObjectiveId);
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
