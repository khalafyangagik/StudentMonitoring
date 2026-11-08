using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր,lecturer,teacher,instructor,դասախոս")]
[Route("api/[controller]")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionService _service;
    public SessionsController(ISessionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("options")]
    public async Task<ActionResult<IReadOnlyList<SessionOptionResponse>>> GetOptions(CancellationToken cancellationToken) =>
        Ok(await _service.GetOptionsAsync(cancellationToken));

    [HttpGet("lecturers")]
    public async Task<ActionResult<IReadOnlyList<LecturerOptionResponse>>> GetLecturers(CancellationToken cancellationToken) =>
        Ok(await _service.GetLecturersAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր")]
    public async Task<ActionResult<SessionResponse>> Create([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _service.CreateAsync(request, cancellationToken);
            return Created($"/api/sessions/{session.Id}", session);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        { return BadRequest(new { message = "Ընտրված առարկան կամ խումբը այլևս հասանելի չէ։" }); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր")]
    public async Task<ActionResult<SessionResponse>> Update(int id, [FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _service.UpdateAsync(id, request, cancellationToken);
            return session is null ? NotFound() : Ok(session);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        { return BadRequest(new { message = "Ընտրված առարկան կամ խումբը այլևս հասանելի չէ։" }); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try { return await _service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound(); }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        { return Conflict(new { message = "Դասաժամին հաճախելիության գրառումներ կան։ Նախ հեռացրեք դրանք։" }); }
    }
}
