namespace StudentMonitoring.Server.Services;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(string recipient, string code, CancellationToken cancellationToken);
    Task SendLoginCodeAsync(string recipient, string code, CancellationToken cancellationToken);
    Task SendInvitationAsync(string recipient, string role, string link, CancellationToken cancellationToken);
}
