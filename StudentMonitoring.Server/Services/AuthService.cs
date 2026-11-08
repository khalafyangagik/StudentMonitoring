using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class AuthService(IAuthRepository repository, IEmailSender emailSender,
    PasswordHasher<AuthPasswordSubject> passwordHasher)
{
    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var passwordHash = passwordHasher.HashPassword(new AuthPasswordSubject(), request.Password);
        var code = CreateCode();
        var id = await repository.CreatePendingAsync(request.Name.Trim(), request.Surname.Trim(), email, passwordHash,
            passwordHasher.HashPassword(new AuthPasswordSubject(), code), DateTime.UtcNow.AddMinutes(10), cancellationToken);
        if (id is null) return (false, "Այս էլ. հասցեն արդեն գրանցված է։ Փորձեք մուտք գործել։");
        await emailSender.SendVerificationCodeAsync(email, code, cancellationToken);
        return (true, "Հաստատման կոդն ուղարկվել է ձեր էլ. հասցեին։");
    }

    public async Task<(bool Success, string Message)> VerifyAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var record = await repository.FindVerificationAsync(NormalizeEmail(request.Email), cancellationToken);
        if (record is null) return (false, "Գրանցման կոդը չի գտնվել։ Կրկին գրանցվեք կամ ուղարկեք նոր կոդ։");
        if (record.Attempts >= 5) return (false, "Կոդի փորձերի սահմանաչափը լրացել է։ Ուղարկեք նոր կոդ։");
        if (record.ExpiresAt <= DateTime.UtcNow) return (false, "Կոդի ժամկետը լրացել է։ Ուղարկեք նոր կոդ։");

        var result = passwordHasher.VerifyHashedPassword(new AuthPasswordSubject(), record.CodeHash, request.Code);
        if (result == PasswordVerificationResult.Failed)
        {
            await repository.IncrementAttemptsAsync(record.UserId, cancellationToken);
            return (false, "Հաստատման կոդը սխալ է։");
        }
        var changed = await repository.VerifyEmailAsync(record.UserId, cancellationToken);
        return changed ? (true, "Էլ. հասցեն հաստատված է։ Կարող եք մուտք գործել։") : (false, "Օգտատերը չի գտնվել։");
    }

    public async Task<(bool Success, string Message)> ResendAsync(string emailInput, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(emailInput);
        var account = await repository.FindByEmailAsync(email, cancellationToken);
        if (account is null || account.EmailVerified) return (false, "Չհաստատված գրանցում այս էլ. հասցեով չի գտնվել։");
        var verification = await repository.FindVerificationAsync(email, cancellationToken);
        if (verification is not null && verification.SentAt > DateTime.UtcNow.AddSeconds(-60))
            return (false, "Սպասեք մեկ րոպե՝ նոր կոդ պահանջելուց առաջ։");
        var code = CreateCode();
        await repository.ReplaceVerificationAsync(account.Id, passwordHasher.HashPassword(new AuthPasswordSubject(), code),
            DateTime.UtcNow.AddMinutes(10), cancellationToken);
        await emailSender.SendVerificationCodeAsync(email, code, cancellationToken);
        return (true, "Նոր հաստատման կոդն ուղարկվել է։");
    }

    public async Task<AuthAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        await repository.FindByEmailAsync(NormalizeEmail(email), cancellationToken);

    public bool VerifyPassword(AuthAccount account, string password) =>
        !string.IsNullOrWhiteSpace(account.PasswordHash) &&
        passwordHasher.VerifyHashedPassword(new AuthPasswordSubject(), account.PasswordHash, password) != PasswordVerificationResult.Failed;

    public static bool RequiresLoginCode(AuthAccount account) =>
        account.Role.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
        account.Role.Equals("superadmin", StringComparison.OrdinalIgnoreCase) ||
        account.Role.Equals("administrator", StringComparison.OrdinalIgnoreCase) ||
        account.Role.Equals("ադմինիստրատոր", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> SendLoginCodeAsync(AuthAccount account, CancellationToken cancellationToken)
    {
        var code = CreateCode();
        var codeHash = passwordHasher.HashPassword(new AuthPasswordSubject(), code);
        var stored = await repository.ReplaceLoginCodeAsync(account.Id, codeHash, DateTime.UtcNow.AddMinutes(10), cancellationToken);
        if (!stored) return false;
        try { await emailSender.SendLoginCodeAsync(account.Email, code, cancellationToken); }
        catch
        {
            await repository.ConsumeLoginCodeAsync(account.Id, codeHash, cancellationToken);
            throw;
        }
        return true;
    }

    public async Task<(bool Success, int? UserId, string Message)> VerifyLoginCodeAsync(string emailInput, string code,
        CancellationToken cancellationToken)
    {
        var record = await repository.FindLoginCodeAsync(NormalizeEmail(emailInput), cancellationToken);
        if (record is null) return (false, null, "Մուտքի կոդը չի գտնվել։ Կրկին մուտք գործեք՝ նոր կոդ ստանալու համար։");
        if (record.Attempts >= 5) return (false, null, "Կոդի փորձերի սահմանաչափը լրացել է։ Կրկին մուտք գործեք՝ նոր կոդ ստանալու համար։");
        if (record.ExpiresAt <= DateTime.UtcNow) return (false, null, "Մուտքի կոդի ժամկետը լրացել է։ Կրկին մուտք գործեք։");
        if (passwordHasher.VerifyHashedPassword(new AuthPasswordSubject(), record.CodeHash, code) == PasswordVerificationResult.Failed)
        {
            await repository.IncrementLoginCodeAttemptsAsync(record.UserId, cancellationToken);
            return (false, null, "Մուտքի կոդը սխալ է։");
        }
        var consumed = await repository.ConsumeLoginCodeAsync(record.UserId, record.CodeHash, cancellationToken);
        return consumed ? (true, record.UserId, "Հաստատված է։") : (false, null, "Կոդն արդեն օգտագործվել է։ Կրկին մուտք գործեք։");
    }

    public static AuthUser ToPublicUser(AuthAccount account) =>
        new(account.Id, account.Name, account.Surname, account.Email, account.Role);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string CreateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
