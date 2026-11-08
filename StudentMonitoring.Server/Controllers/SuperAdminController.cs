using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin")]
[Route("api/superadmin")]
public sealed class SuperAdminController(SuperAdminService service, UserInvitationService invitations, ILogger<SuperAdminController> logger) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<SuperAdminOverviewResponse>> GetOverview(CancellationToken cancellationToken) =>
        Ok(await service.GetOverviewAsync(cancellationToken));

    [HttpPost("invitations/admin")]
    public async Task<IActionResult> InviteAdministrator(AddAdministratorRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var inviterId)) return Unauthorized();
        try
        {
            await invitations.CreateAsync(request.Email, "admin", request.DepartmentIds, inviterId, cancellationToken);
            return Ok(new { message = "Ադմինի 7 օր վավեր հրավերն ուղարկվել է նշված բաժինների համար։" });
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("SMTP", StringComparison.OrdinalIgnoreCase))
        { return StatusCode(503, new { message = "Նամակը չուղարկվեց։ Ստուգեք SMTP կարգավորումները։" }); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        catch (System.Net.Mail.SmtpException exception)
        {
            logger.LogError(exception, "Administrator invitation email could not be sent.");
            return StatusCode(503, new { message = "Նամակը չուղարկվեց։ Ստուգեք SMTP կարգավորումները և կրկին փորձեք։" });
        }
    }
}
