using Npgsql;
using StudentMonitoring.Server.Models;

namespace StudentMonitoring.Server.Repositories;

public sealed class AuthRepository(NpgsqlDataSource dataSource) : IAuthRepository
{
    public async Task<int?> CreatePendingAsync(string name, string surname, string email, string passwordHash,
        string codeHash, DateTime expiresAt, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        int userId;
        await using (var command = new NpgsqlCommand("""
            INSERT INTO "user" (role_id, name, surname, email, password_hash, is_active, email_verified)
            VALUES ((SELECT id FROM role WHERE lower(name) IN ('student', 'ուսանող') ORDER BY id LIMIT 1),
                    @name, @surname, @email, @passwordHash, FALSE, FALSE)
            RETURNING id;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("surname", surname);
            command.Parameters.AddWithValue("email", email);
            command.Parameters.AddWithValue("passwordHash", passwordHash);
            try { userId = (int)(await command.ExecuteScalarAsync(cancellationToken))!; }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation) { return null; }
        }

        await using (var command = new NpgsqlCommand("""
            INSERT INTO email_verification_code (user_id, code_hash, expires_at, sent_at, attempts)
            VALUES (@userId, @codeHash, @expiresAt, CURRENT_TIMESTAMP, 0);
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("codeHash", codeHash);
            command.Parameters.AddWithValue("expiresAt", expiresAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return userId;
    }

    public async Task<AuthAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT u.id, COALESCE(u.name, ''), COALESCE(u.surname, ''), u.email, u.password_hash,
                   COALESCE(r.name, ''), COALESCE(u.is_active, FALSE), COALESCE(u.email_verified, FALSE)
            FROM "user" u LEFT JOIN role r ON r.id = u.role_id WHERE lower(u.email) = @email;
            """);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new AuthAccount(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.IsDBNull(4) ? "" : reader.GetString(4), reader.GetString(5), reader.GetBoolean(6), reader.GetBoolean(7));
    }

    public async Task<VerificationRecord?> FindVerificationAsync(string email, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT v.user_id, v.code_hash, v.expires_at, v.sent_at, v.attempts
            FROM email_verification_code v JOIN "user" u ON u.id = v.user_id WHERE lower(u.email) = @email;
            """);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new VerificationRecord(reader.GetInt32(0), reader.GetString(1), reader.GetDateTime(2), reader.GetDateTime(3), reader.GetInt32(4))
            : null;
    }

    public async Task ReplaceVerificationAsync(int userId, string codeHash, DateTime expiresAt, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            INSERT INTO email_verification_code (user_id, code_hash, expires_at, sent_at, attempts)
            VALUES (@userId, @codeHash, @expiresAt, CURRENT_TIMESTAMP, 0)
            ON CONFLICT (user_id) DO UPDATE SET code_hash=EXCLUDED.code_hash, expires_at=EXCLUDED.expires_at,
                sent_at=CURRENT_TIMESTAMP, attempts=0;
            """);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("codeHash", codeHash);
        command.Parameters.AddWithValue("expiresAt", expiresAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task IncrementAttemptsAsync(int userId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("UPDATE email_verification_code SET attempts=attempts+1 WHERE user_id=@userId;");
        command.Parameters.AddWithValue("userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> VerifyEmailAsync(int userId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var update = new NpgsqlCommand("UPDATE \"user\" SET email_verified=TRUE, is_active=TRUE, updated_at=CURRENT_TIMESTAMP WHERE id=@id;", connection, transaction);
        update.Parameters.AddWithValue("id", userId);
        var changed = await update.ExecuteNonQueryAsync(cancellationToken) > 0;
        if (changed)
        {
            await using var delete = new NpgsqlCommand("DELETE FROM email_verification_code WHERE user_id=@id;", connection, transaction);
            delete.Parameters.AddWithValue("id", userId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return changed;
    }

    public async Task<bool> ReplaceLoginCodeAsync(int userId, string codeHash, DateTime expiresAt, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            INSERT INTO login_verification_code(user_id,code_hash,expires_at,sent_at,attempts)
            VALUES(@userId,@codeHash,@expiresAt,CURRENT_TIMESTAMP,0)
            ON CONFLICT(user_id) DO UPDATE SET code_hash=EXCLUDED.code_hash,expires_at=EXCLUDED.expires_at,
                sent_at=CURRENT_TIMESTAMP,attempts=0
            WHERE login_verification_code.sent_at < CURRENT_TIMESTAMP - INTERVAL '60 seconds';
            """);
        command.Parameters.AddWithValue("userId", userId); command.Parameters.AddWithValue("codeHash", codeHash);
        command.Parameters.AddWithValue("expiresAt", expiresAt);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<LoginVerificationRecord?> FindLoginCodeAsync(string email, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT v.user_id,v.code_hash,v.expires_at,v.sent_at,v.attempts
            FROM login_verification_code v JOIN "user" u ON u.id=v.user_id WHERE lower(u.email)=@email;
            """);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new LoginVerificationRecord(reader.GetInt32(0),reader.GetString(1),reader.GetDateTime(2),reader.GetDateTime(3),reader.GetInt32(4))
            : null;
    }

    public async Task IncrementLoginCodeAttemptsAsync(int userId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("UPDATE login_verification_code SET attempts=attempts+1 WHERE user_id=@id;");
        command.Parameters.AddWithValue("id", userId); await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ConsumeLoginCodeAsync(int userId, string codeHash, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("DELETE FROM login_verification_code WHERE user_id=@id AND code_hash=@hash;");
        command.Parameters.AddWithValue("id", userId); command.Parameters.AddWithValue("hash", codeHash);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
