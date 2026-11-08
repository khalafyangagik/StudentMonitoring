using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin,lecturer,teacher,instructor,դասախոս")]
[Route("api/[controller]")]
public sealed class AttendancesController : ControllerBase
{
    private readonly IAttendanceService _service;
    private readonly ILogger<AttendancesController> _logger;
    public AttendancesController(IAttendanceService service, ILogger<AttendancesController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (IsLecturer())
        {
            if (!TryGetLecturerId(out var lecturerId)) return Unauthorized();
            return Ok(await _service.GetForLecturerAsync(lecturerId, cancellationToken));
        }
        return Ok(await _service.GetAllAsync(cancellationToken));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<SessionOptionResponse>>> GetSessions(CancellationToken cancellationToken)
    {
        if (IsLecturer())
        {
            if (!TryGetLecturerId(out var lecturerId)) return Unauthorized();
            return Ok(await _service.GetSessionsForLecturerAsync(lecturerId, cancellationToken));
        }
        return Ok(await _service.GetSessionsAsync(cancellationToken));
    }

    [HttpGet("students")]
    public async Task<ActionResult<IReadOnlyList<StudentResponse>>> GetStudents(CancellationToken cancellationToken)
    {
        if (!IsLecturer()) return Forbid();
        if (!TryGetLecturerId(out var lecturerId)) return Unauthorized();
        return Ok(await _service.GetStudentsForLecturerAsync(lecturerId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AttendanceResponse>> Create([FromBody] CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            AttendanceResponse attendance;
            if (IsLecturer())
            {
                if (!TryGetLecturerId(out var lecturerId)) return Unauthorized();
                attendance = await _service.CreateForLecturerAsync(lecturerId, request, cancellationToken);
            }
            else attendance = await _service.CreateAsync(request, cancellationToken);
            return Created($"/api/attendances/{attendance.Id}", attendance);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (PostgresException exception) { return HandleDatabaseError(exception, "Հաճախելիությունը չհաջողվեց գրանցել։"); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AttendanceResponse>> Update(int id, [FromBody] CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            AttendanceResponse? attendance;
            if (IsLecturer())
            {
                if (!TryGetLecturerId(out var lecturerId)) return Unauthorized();
                attendance = await _service.UpdateForLecturerAsync(lecturerId, id, request, cancellationToken);
            }
            else attendance = await _service.UpdateAsync(id, request, cancellationToken);
            return attendance is null ? NotFound() : Ok(attendance);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (PostgresException exception) { return HandleDatabaseError(exception, "Հաճախելիությունը չհաջողվեց թարմացնել։"); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (IsLecturer())
        {
            if (!TryGetLecturerId(out var lecturerId)) return Unauthorized();
            return await _service.DeleteForLecturerAsync(lecturerId, id, cancellationToken) ? NoContent() : NotFound();
        }
        return await _service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private bool IsLecturer() => User.IsInRole("lecturer") || User.IsInRole("teacher") || User.IsInRole("instructor") || User.IsInRole("դասախոս");
    private bool TryGetLecturerId(out int lecturerId) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out lecturerId);

    private ObjectResult HandleDatabaseError(PostgresException exception, string fallback)
    {
        _logger.LogError(exception, "Attendance database operation failed with SQL state {SqlState}.", exception.SqlState);
        return exception.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => Conflict(new { message = "Այս ուսանողի հաճախելիությունն արդեն գրանցված է տվյալ դասաժամի համար։" }),
            PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.InvalidTextRepresentation =>
                BadRequest(new { message = "Ստուգեք ընտրված ուսանողի, դասաժամի և կարգավիճակի տվյալները։" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = fallback })
        };
    }
}
