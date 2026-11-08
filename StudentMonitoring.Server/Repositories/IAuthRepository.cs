namespace StudentMonitoring.Server.Repositories;

public sealed record AuthAccount(int Id, string Name, string Surname, string Email, string PasswordHash,
    string Role, bool IsActive, bool EmailVerified);
public sealed record VerificationRecord(int UserId, string CodeHash, DateTime ExpiresAt, DateTime SentAt, int Attempts);
public sealed record LoginVerificationRecord(int UserId, string CodeHash, DateTime ExpiresAt, DateTime SentAt, int Attempts);

public interface IAuthRepository
{
    Task<int?> CreatePendingAsync(string name, string surname, string email, string passwordHash,
        string codeHash, DateTime expiresAt, CancellationToken cancellationToken);
    Task<AuthAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<VerificationRecord?> FindVerificationAsync(string email, CancellationToken cancellationToken);
    Task ReplaceVerificationAsync(int userId, string codeHash, DateTime expiresAt, CancellationToken cancellationToken);
    Task IncrementAttemptsAsync(int userId, CancellationToken cancellationToken);
    Task<bool> VerifyEmailAsync(int userId, CancellationToken cancellationToken);
    Task<bool> ReplaceLoginCodeAsync(int userId, string codeHash, DateTime expiresAt, CancellationToken cancellationToken);
    Task<LoginVerificationRecord?> FindLoginCodeAsync(string email, CancellationToken cancellationToken);
    Task IncrementLoginCodeAttemptsAsync(int userId, CancellationToken cancellationToken);
    Task<bool> ConsumeLoginCodeAsync(int userId, string codeHash, CancellationToken cancellationToken);
}
