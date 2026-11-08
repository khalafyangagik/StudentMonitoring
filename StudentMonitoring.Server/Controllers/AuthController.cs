using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        return StatusCode(StatusCodes.Status403Forbidden,
            new { message = "Գրանցումը կատարվում է հրավերով։ Խնդրեք ձեր բաժնի ադմինիստրատորին հրավեր ուղարկել ձեր էլ. հասցեին։" });
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.VerifyAsync(request, cancellationToken);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.ResendAsync(request.Email, cancellationToken);
            return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Email delivery is not configured for resend.");
            return StatusCode(503, new { message = "Էլ. նամակ ուղարկելու կարգավորումները բացակայում են։ Կարգավորեք SMTP-ը և կրկին փորձեք։" });
        }
        catch (System.Net.Mail.SmtpException exception)
        {
            logger.LogError(exception, "Verification email resend failed.");
            return StatusCode(503, new { message = "Նամակը չհաջողվեց ուղարկել։ Ստուգեք SMTP կարգավորումները և կրկին փորձեք։" });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var account = await authService.FindByEmailAsync(request.Email, cancellationToken);
        if (account is null || !authService.VerifyPassword(account, request.Password))
            return Unauthorized(new { message = "Էլ. հասցեն կամ գաղտնաբառը սխալ է։" });
        if (!account.EmailVerified) return StatusCode(403, new { message = "Նախ հաստատեք ձեր էլ. հասցեն։" });
        if (!account.IsActive) return StatusCode(403, new { message = "Օգտահաշիվն անջատված է։ Դիմեք ադմինիստրատորին։" });

        if (AuthService.RequiresLoginCode(account))
        {
            try
            {
                var sent = await authService.SendLoginCodeAsync(account, cancellationToken);
                return sent ? StatusCode(202, new { requiresEmailCode = true, email = account.Email, message = "Մուտքի հաստատման կոդն ուղարկվել է ձեր էլ. հասցեին։" })
                    : StatusCode(429, new { message = "Նոր մուտքի կոդ կարող եք պահանջել մեկ րոպե անց։" });
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.Net.Mail.SmtpException)
            {
                logger.LogError(exception, "Privileged login verification email could not be sent.");
                return StatusCode(503, new { message = "Մուտքի կոդը չհաջողվեց ուղարկել։ Ստուգեք SMTP կարգավորումները և կրկին փորձեք։" });
            }
        }

        await SignInAsync(account);
        return Ok(AuthService.ToPublicUser(account));
    }

    [AllowAnonymous]
    [HttpPost("verify-login")]
    public async Task<IActionResult> VerifyLogin(VerifyLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.VerifyLoginCodeAsync(request.Email, request.Code, cancellationToken);
        if (!result.Success || result.UserId is null) return BadRequest(new { message = result.Message });
        var account = await authService.FindByEmailAsync(request.Email, cancellationToken);
        if (account is null || account.Id != result.UserId || !account.IsActive || !account.EmailVerified || !AuthService.RequiresLoginCode(account))
            return Unauthorized(new { message = "Օգտահաշիվը հաստատված կամ ակտիվ չէ։" });
        await SignInAsync(account);
        return Ok(AuthService.ToPublicUser(account));
    }

    private async Task SignInAsync(AuthAccount account)
    {
        var user = AuthService.ToPublicUser(account);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.Name} {user.Surname}".Trim()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();
        var account = await authService.FindByEmailAsync(email, cancellationToken);
        return account is null || !account.IsActive || !account.EmailVerified
            ? Unauthorized()
            : Ok(AuthService.ToPublicUser(account));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Դուք դուրս եք եկել համակարգից։" });
    }
}

public sealed class ResendVerificationRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; init; } = "";
}
