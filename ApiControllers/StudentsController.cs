using Microsoft.AspNetCore.Mvc;
using Services.DTOs;
using Services.Interfaces;
using Shared.Results;

namespace ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _studentService.GetAllAsync();
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _studentService.GetByIdAsync(id);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
    {
        var result = await _studentService.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentDto dto)
    {
        var result = await _studentService.UpdateAsync(id, dto);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _studentService.DeleteAsync(id);
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
