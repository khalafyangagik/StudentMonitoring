using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class UserInvitationService(InvitationRepository repository, IEmailSender emailSender,
    PasswordHasher<AuthPasswordSubject> passwordHasher, IConfiguration configuration)
{
    public async Task CreateAsync(string emailInput, string role, int[] departments, int inviterId, CancellationToken cancellationToken)
    {
        var normalizedEmail = emailInput.Trim().ToLowerInvariant();
        var normalizedRole = role.Trim().ToLowerInvariant();
        if (normalizedRole is not ("admin" or "lecturer" or "student"))
            throw new InvalidOperationException("Հրավիրելի դերը պետք է լինի ադմին, դասախոս կամ ուսանող։");
        var departmentIds = departments.Distinct().ToArray();
        if (departmentIds.Length == 0) throw new InvalidOperationException("Ընտրեք առնվազն մեկ բաժին։");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var expiresAt = DateTime.UtcNow.AddDays(7);
        await repository.CreateAsync(normalizedEmail, normalizedRole, inviterId, departmentIds, tokenHash, expiresAt, cancellationToken);
        var frontendBase = configuration["Email:InvitationBaseUrl"] ?? configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var link = $"{frontendBase.TrimEnd('/')}/accept-invitation?token={Uri.EscapeDataString(token)}";
        await emailSender.SendInvitationAsync(normalizedEmail, normalizedRole, link, cancellationToken);
    }

    public Task<InvitationPreview?> PreviewAsync(string token, CancellationToken cancellationToken) =>
        repository.FindValidAsync(HashToken(token), cancellationToken);

    public async Task<string?> AcceptAsync(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var passwordHash = passwordHasher.HashPassword(new AuthPasswordSubject(), request.Password);
        return await repository.AcceptAsync(HashToken(request.Token), request.Name, request.Surname, passwordHash, cancellationToken);
    }

    public Task<AdminWorkspaceResponse> GetAdminWorkspaceAsync(int adminId, CancellationToken cancellationToken) =>
        repository.GetAdminWorkspaceAsync(adminId, cancellationToken);

    public Task<bool> SetStaffActiveAsync(int adminId, int userId, bool isActive, CancellationToken cancellationToken) =>
        repository.SetStaffActiveAsync(adminId, userId, isActive, cancellationToken);

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
}
