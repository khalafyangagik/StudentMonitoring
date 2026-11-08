using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin,lecturer,teacher,instructor,դասախոս")]
[Route("api/[controller]")]
public sealed class StudentsController : ControllerBase
{
    private readonly IStudentService _service;
    public StudentsController(IStudentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<StudentResponse>> Create([FromBody] CreateStudentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _service.CreateAsync(request, cancellationToken);
            return Created($"/api/students/{student.Id}", student);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Conflict(new { message = "Այս էլ․ հասցեով օգտատեր արդեն գոյություն ունի։" }); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<StudentResponse>> Update(int id, [FromBody] CreateStudentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _service.UpdateAsync(id, request, cancellationToken);
            return student is null ? NotFound() : Ok(student);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Conflict(new { message = "Այս էլ․ հասցեով օգտատեր արդեն գոյություն ունի։" }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try { return await _service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound(); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        { return Conflict(new { message = "Ուսանողն ունի գնահատականներ կամ հաճախելիության գրառումներ, այդ պատճառով չի կարող ջնջվել։" }); }
    }
}
