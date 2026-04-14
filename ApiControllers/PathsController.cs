using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class PathsController : ControllerBase
{
    private readonly IPathItemService _pathItemService;

    public PathsController(IPathItemService pathItemService)
    {
        _pathItemService = pathItemService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _pathItemService.GetAllAsync();
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _pathItemService.GetByIdAsync(id);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePathItemDto dto)
    {
        var result = await _pathItemService.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePathItemDto dto)
    {
        var result = await _pathItemService.UpdateAsync(id, dto);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _pathItemService.DeleteAsync(id);
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