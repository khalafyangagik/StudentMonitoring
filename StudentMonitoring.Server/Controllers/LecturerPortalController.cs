using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "lecturer,teacher,instructor,դասախոս")]
[Route("api/lecturer-portal")]
public sealed class LecturerPortalController(LecturerPortalService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LecturerPortalSummary>> Get(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var lecturerId)) return Unauthorized();
        var summary = await service.GetSummaryAsync(lecturerId, cancellationToken);
        return summary is null ? Unauthorized() : Ok(summary);
    }

    [HttpGet("offerings/{offeringId:int}/students")]
    public async Task<IActionResult> GetStudents(int offeringId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var lecturerId)) return Unauthorized();
        var students = await service.GetStudentsAsync(lecturerId, offeringId, cancellationToken);
        return students is null ? NotFound(new { message = "Առարկան չի գտնվել կամ կցված չէ այս դասախոսին։" }) : Ok(students);
    }

    [HttpGet("offerings/{offeringId:int}/sessions")]
    public async Task<IActionResult> GetSessions(int offeringId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var lecturerId)) return Unauthorized();
        var sessions = await service.GetSessionsAsync(lecturerId, offeringId, cancellationToken);
        return sessions is null ? NotFound(new { message = "Առարկան չի գտնվել կամ կցված չէ այս դասախոսին։" }) : Ok(sessions);
    }

    [HttpPost("grades")]
    public async Task<IActionResult> CreateGrade([FromBody] CreateLecturerGradeRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var lecturerId)) return Unauthorized();
        var grade = await service.CreateGradeAsync(lecturerId, request, cancellationToken);
        return grade is null
            ? BadRequest(new { message = "Ստուգեք՝ ուսանողը ակտիվ գրանցվա՞ծ է խմբում, դասաժամը պատկանո՞ւմ է առարկային, և առարկան կցվա՞ծ է ձեզ։" })
            : StatusCode(StatusCodes.Status201Created, grade);
    }
}
