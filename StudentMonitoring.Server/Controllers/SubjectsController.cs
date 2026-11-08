using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր")]
[Route("api/[controller]")]
public sealed class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SubjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var subjects = await _subjectService.GetAllAsync(cancellationToken);
        return Ok(subjects);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubjectResponse>> Create(
        [FromBody] CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = await _subjectService.CreateAsync(request, cancellationToken);
            return Created($"/api/subjects/{subject.Id}", subject);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectResponse>> Update(
        int id,
        [FromBody] CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = await _subjectService.UpdateAsync(id, request, cancellationToken);
            return subject is null ? NotFound() : Ok(subject);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _subjectService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Conflict(new { message = "This subject is used as a prerequisite by another subject and cannot be deleted." });
        }
    }
}
