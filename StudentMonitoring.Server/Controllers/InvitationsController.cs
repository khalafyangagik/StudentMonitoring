using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Route("api/invitations")]
public sealed class InvitationsController(UserInvitationService invitations, ILogger<InvitationsController> logger) : ControllerBase
{
    [AllowAnonymous, HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest(new { message = "Հրավերի հղումը թերի է։" });
        var preview = await invitations.PreviewAsync(token, cancellationToken);
        return preview is null ? BadRequest(new { message = "Հրավերը անվավեր է, արդեն օգտագործվել է կամ ժամկետանց է։" }) : Ok(preview);
    }

    [AllowAnonymous, HttpPost("accept")]
    public async Task<IActionResult> Accept(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var email = await invitations.AcceptAsync(request, cancellationToken);
            return email is null ? BadRequest(new { message = "Հրավերը անվավեր է, արդեն օգտագործվել է կամ ժամկետանց է։" })
                : Ok(new { email, message = "Հրավերն ընդունվեց։ Այժմ կարող եք մուտք գործել ձեր էլ. հասցեով և գաղտնաբառով։" });
        }
        catch (Exception exception) when (exception is Npgsql.NpgsqlException)
        {
            logger.LogWarning(exception, "Invitation acceptance failed because the email may already have an account.");
            return Conflict(new { message = "Այս էլ. հասցեով հաշիվ արդեն ստեղծվել է։" });
        }
    }
}
