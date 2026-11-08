using Microsoft.AspNetCore.Identity;
using Npgsql;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Services;

public sealed class SuperAdminBootstrapper(NpgsqlDataSource dataSource,
    PasswordHasher<AuthPasswordSubject> passwordHasher, IConfiguration configuration, ILogger<SuperAdminBootstrapper> logger)
{
    public async Task PromoteConfiguredAccountAsync(CancellationToken cancellationToken)
    {
        var email = configuration["BootstrapSuperAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["BootstrapSuperAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new InvalidOperationException("BootstrapSuperAdmin:Email and a password of at least 8 characters must both be configured.");

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(73025, 1);", connection, transaction))
            await lockCommand.ExecuteScalarAsync(cancellationToken);

        int? existingUserId = null;
        bool emailVerified = false;
        await using (var command = new NpgsqlCommand("""
            SELECT id, COALESCE(email_verified,FALSE) FROM "user" WHERE lower(email)=@email FOR UPDATE;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("email", email);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            { existingUserId = reader.GetInt32(0); emailVerified = reader.GetBoolean(1); }
        }

        int roleId;
        await using (var command = new NpgsqlCommand("SELECT id FROM role WHERE lower(name)='superadmin' ORDER BY id LIMIT 1;", connection, transaction))
        {
            var found = await command.ExecuteScalarAsync(cancellationToken);
            if (found is null) throw new InvalidOperationException("Run Database/001_auth.sql before bootstrapping SuperAdmin.");
            roleId = (int)found;
        }

        if (existingUserId is null)
        {
            await using (var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"user\" WHERE role_id=@roleId);", connection, transaction))
            {
                command.Parameters.AddWithValue("roleId", roleId);
                if ((bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false))
                    throw new InvalidOperationException("A different SuperAdmin account already exists; bootstrap was refused.");
            }
            var hashForNewAccount = passwordHasher.HashPassword(new AuthPasswordSubject(), password);
            await using (var command = new NpgsqlCommand("""
                INSERT INTO "user"(role_id,name,surname,email,password_hash,is_active,email_verified,created_at,updated_at)
                VALUES(@roleId,'Գլխավոր','Ադմինիստրատոր',@email,@passwordHash,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)
                RETURNING id;
                """, connection, transaction))
            {
                command.Parameters.AddWithValue("roleId", roleId); command.Parameters.AddWithValue("email", email);
                command.Parameters.AddWithValue("passwordHash", hashForNewAccount);
                existingUserId = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
            }
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Created the initial SuperAdmin account from the configured bootstrap credentials for {Email}.", email);
            return;
        }

        if (!emailVerified)
        {
            logger.LogWarning("SuperAdmin bootstrap skipped: {Email} has not verified its email. Verify it, then restart the API.", email);
            return;
        }

        await using (var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM "user" WHERE role_id=@roleId AND id<>@userId);
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("roleId", roleId);
            command.Parameters.AddWithValue("userId", existingUserId.Value);
            if ((bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false))
                throw new InvalidOperationException("A different SuperAdmin account already exists; bootstrap was refused.");
        }

        var hash = passwordHasher.HashPassword(new AuthPasswordSubject(), password);
        await using (var command = new NpgsqlCommand("""
            UPDATE "user" SET role_id=@roleId, password_hash=@passwordHash, is_active=TRUE, updated_at=CURRENT_TIMESTAMP
            WHERE id=@userId;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("roleId", roleId);
            command.Parameters.AddWithValue("passwordHash", hash);
            command.Parameters.AddWithValue("userId", existingUserId.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Bootstrapped the configured SuperAdmin account {Email}.", email);
    }
}
