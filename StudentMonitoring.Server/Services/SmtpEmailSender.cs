using System.Net;
using System.Net.Mail;

namespace StudentMonitoring.Server.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendVerificationCodeAsync(string recipient, string code, CancellationToken cancellationToken)
        => await SendCodeAsync(recipient, code, "Էլ. հասցեի հաստատում", cancellationToken);

    public async Task SendLoginCodeAsync(string recipient, string code, CancellationToken cancellationToken)
        => await SendCodeAsync(recipient, code, "Մուտքի հաստատման կոդ", cancellationToken);

    public async Task SendInvitationAsync(string recipient, string role, string link, CancellationToken cancellationToken)
    {
        var (host, username, password, from, port, ssl, displayName) = ReadSettings();
        var roleName = role switch { "admin" => "ադմինիստրատոր", "lecturer" => "դասախոս", _ => "ուսանող" };
        using var message = new MailMessage(from, recipient)
        {
            Subject = "Հրավեր՝ ուսանողական առաջադիմության համակարգում",
            IsBodyHtml = true,
            Body = $"<p>Դուք հրավիրված եք համակարգ՝ որպես <strong>{WebUtility.HtmlEncode(roleName)}</strong>։</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">Ընդունել հրավերը և ստեղծել հաշիվ</a></p><p>Հրավերը վավեր է 7 օր։ Եթե չեք սպասել այս նամակին, կարող եք անտեսել այն։</p>"
        };
        if (!string.IsNullOrWhiteSpace(displayName)) message.From = new MailAddress(from, displayName);
        using var client = new SmtpClient(host, port) { EnableSsl = ssl, Credentials = new NetworkCredential(username, password) };
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }

    private async Task SendCodeAsync(string recipient, string code, string subject, CancellationToken cancellationToken)
    {
        var (host, username, password, from, port, ssl, displayName) = ReadSettings();

        using var message = new MailMessage(from, recipient)
        {
            Subject = subject,
            IsBodyHtml = true,
            Body = $"<p>Ձեր մեկանգամյա կոդն է՝ <strong style='font-size:24px'>{WebUtility.HtmlEncode(code)}</strong></p><p>Կոդը վավեր է 10 րոպե։ Եթե դուք չեք կատարել այս գործողությունը, անտեսեք նամակը։</p>"
        };
        if (!string.IsNullOrWhiteSpace(displayName)) message.From = new MailAddress(from, displayName);

        using var client = new SmtpClient(host, port) { EnableSsl = ssl, Credentials = new NetworkCredential(username, password) };
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }

    private (string Host, string Username, string Password, string From, int Port, bool Ssl, string? DisplayName) ReadSettings()
    {
        var settings = configuration.GetSection("Email");
        var host = settings["SmtpHost"]; var username = settings["Username"]; var password = settings["Password"]; var from = settings["FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from) || !int.TryParse(settings["SmtpPort"], out var port))
            throw new InvalidOperationException("Email SMTP կարգավորումները դեռ լրացված չեն։");
        var ssl = !bool.TryParse(settings["EnableSsl"], out var enabled) || enabled;
        return (host, username, password, from, port, ssl, settings["FromName"]);
    }
}
