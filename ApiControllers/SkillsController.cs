using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;

    public SkillsController(ISkillService skillService)
    {
        _skillService = skillService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _skillService.GetAllAsync();
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _skillService.GetByIdAsync(id);
        return ToActionResult(result);
    }

    [HttpGet("by-path/{pathId}")]
    public async Task<IActionResult> GetByPathId(int pathId)
    {
        var result = await _skillService.GetByPathIdAsync(pathId);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSkillDto dto)
    {
        var result = await _skillService.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSkillDto dto)
    {
        var result = await _skillService.UpdateAsync(id, dto);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _skillService.DeleteAsync(id);
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