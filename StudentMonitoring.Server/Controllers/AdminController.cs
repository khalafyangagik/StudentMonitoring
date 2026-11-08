using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "admin,administrator,ադմինիստրատոր")]
[Route("api/admin")]
public sealed class AdminController(UserInvitationService invitations, ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("workspace")]
    public async Task<IActionResult> GetWorkspace(CancellationToken cancellationToken) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? Ok(await invitations.GetAdminWorkspaceAsync(id, cancellationToken)) : Unauthorized();

    [HttpPost("invitations")]
    public async Task<IActionResult> Invite(CreateStaffInvitationRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        try
        {
            await invitations.CreateAsync(request.Email, request.Role, request.DepartmentIds, id, cancellationToken);
            return Ok(new { message = "7 օր վավեր հրավերն ուղարկվել է։" });
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("SMTP", StringComparison.OrdinalIgnoreCase))
        { return StatusCode(503, new { message = "Նամակը չուղարկվեց։ Ստուգեք SMTP կարգավորումները։" }); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        catch (System.Net.Mail.SmtpException exception)
        {
            logger.LogError(exception, "Staff invitation email could not be sent.");
            return StatusCode(503, new { message = "Նամակը չուղարկվեց։ Ստուգեք SMTP կարգավորումները և կրկին փորձեք։" });
        }
    }

    [HttpPatch("staff/{userId:int}/active")]
    public async Task<IActionResult> SetStaffActive(int userId, SetStaffActiveRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        return await invitations.SetStaffActiveAsync(id, userId, request.IsActive, cancellationToken)
            ? Ok(new { message = request.IsActive ? "Հաշիվն ակտիվացվեց։" : "Հաշիվն անջատվեց։" })
            : NotFound(new { message = "Օգտատերը ձեր պատասխանատվության տակ գտնվող բաժիններում չի գտնվել։" });
    }
}
