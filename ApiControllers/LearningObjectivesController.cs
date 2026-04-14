using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class LearningObjectivesController : ControllerBase
{
    private readonly ILearningObjectiveService _learningObjectiveService;

    public LearningObjectivesController(ILearningObjectiveService learningObjectiveService)
    {
        _learningObjectiveService = learningObjectiveService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _learningObjectiveService.GetAllAsync();
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _learningObjectiveService.GetByIdAsync(id);
        return ToActionResult(result);
    }

    [HttpGet("by-skill/{skillId}")]
    public async Task<IActionResult> GetBySkillId(int skillId)
    {
        var result = await _learningObjectiveService.GetBySkillIdAsync(skillId);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLearningObjectiveDto dto)
    {
        var result = await _learningObjectiveService.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLearningObjectiveDto dto)
    {
        var result = await _learningObjectiveService.UpdateAsync(id, dto);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _learningObjectiveService.DeleteAsync(id);
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