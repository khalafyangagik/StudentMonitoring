using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "student,ուսանող")]
[Route("api/student-portal")]
public sealed class StudentPortalController(StudentPortalService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StudentPortalSummary>> Get(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId)) return Unauthorized();
        var summary = await service.GetSummaryAsync(studentId, cancellationToken);
        return summary is null ? Unauthorized() : Ok(summary);
    }
}
