using Npgsql;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class InvitationRepository(NpgsqlDataSource dataSource)
{
    public async Task<int> CreateAsync(string email, string role, int inviterId, int[] departmentIds,
        string tokenHash, DateTime expiresAt, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var check = new NpgsqlCommand("SELECT COUNT(*) FROM department WHERE id=ANY(@ids);", connection, transaction))
        {
            check.Parameters.AddWithValue("ids", departmentIds);
            if (Convert.ToInt64(await check.ExecuteScalarAsync(cancellationToken)) != departmentIds.Length)
                throw new InvalidOperationException("Ընտրված բաժիններից մեկը կամ մի քանիսը չեն գտնվել։");
        }

        string? inviterRole;
        await using (var check = new NpgsqlCommand("""
            SELECT COUNT(*) FROM admin_department WHERE admin_id=@adminId AND department_id=ANY(@ids);
            """, connection, transaction))
        {
            check.Parameters.AddWithValue("adminId", inviterId);
            check.Parameters.AddWithValue("ids", departmentIds);
            await using (var roleCommand = new NpgsqlCommand("SELECT lower(r.name) FROM \"user\" u JOIN role r ON r.id=u.role_id WHERE u.id=@id;", connection, transaction))
            {
                roleCommand.Parameters.AddWithValue("id", inviterId);
                inviterRole = await roleCommand.ExecuteScalarAsync(cancellationToken) as string;
            }
            var isSuperAdmin = string.Equals(inviterRole, "superadmin", StringComparison.OrdinalIgnoreCase);
            if (!isSuperAdmin && Convert.ToInt64(await check.ExecuteScalarAsync(cancellationToken)) != departmentIds.Length)
                throw new InvalidOperationException("Կարող եք հրավիրել միայն ձեր պատասխանատվության տակ գտնվող բաժիններից։");
        }

        await using (var existing = new NpgsqlCommand("SELECT lower(r.name) FROM \"user\" u LEFT JOIN role r ON r.id=u.role_id WHERE lower(u.email)=@email;", connection, transaction))
        {
            existing.Parameters.AddWithValue("email", email);
            var existingRole = await existing.ExecuteScalarAsync(cancellationToken) as string;
            if (existingRole == "superadmin") throw new InvalidOperationException("SuperAdmin հաշիվը չի կարող վերանշանակվել հրավերով։");
            if ((existingRole is "admin" or "administrator" or "ադմինիստրատոր") && inviterRole != "superadmin")
                throw new InvalidOperationException("Ադմինի հաշիվը կարող է վերանշանակել միայն SuperAdmin-ը։");
        }

        await using (var revoke = new NpgsqlCommand("""
            UPDATE user_invitation SET revoked_at=CURRENT_TIMESTAMP
            WHERE lower(email)=@email AND accepted_at IS NULL AND revoked_at IS NULL;
            """, connection, transaction))
        {
            revoke.Parameters.AddWithValue("email", email);
            await revoke.ExecuteNonQueryAsync(cancellationToken);
        }

        int roleId;
        await using (var getRole = new NpgsqlCommand("SELECT id FROM role WHERE lower(name)=@role ORDER BY id LIMIT 1;", connection, transaction))
        {
            getRole.Parameters.AddWithValue("role", role);
            var value = await getRole.ExecuteScalarAsync(cancellationToken);
            if (value is null) throw new InvalidOperationException($"'{role}' դերը տվյալների բազայում չկա։ Կատարեք Database/001_auth.sql-ը։");
            roleId = (int)value;
        }

        int invitationId;
        await using (var create = new NpgsqlCommand("""
            INSERT INTO user_invitation(email, role_id, invited_by, token_hash, expires_at)
            VALUES(@email,@roleId,@inviterId,@tokenHash,@expiresAt) RETURNING id;
            """, connection, transaction))
        {
            create.Parameters.AddWithValue("email", email);
            create.Parameters.AddWithValue("roleId", roleId);
            create.Parameters.AddWithValue("inviterId", inviterId);
            create.Parameters.AddWithValue("tokenHash", tokenHash);
            create.Parameters.AddWithValue("expiresAt", expiresAt);
            invitationId = (int)(await create.ExecuteScalarAsync(cancellationToken))!;
        }

        foreach (var departmentId in departmentIds)
        {
            await using var link = new NpgsqlCommand("INSERT INTO invitation_department(invitation_id,department_id) VALUES(@invitation,@department);", connection, transaction);
            link.Parameters.AddWithValue("invitation", invitationId);
            link.Parameters.AddWithValue("department", departmentId);
            await link.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return invitationId;
    }

    public async Task<InvitationPreview?> FindValidAsync(string tokenHash, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT i.email, r.name, i.expires_at, COALESCE(string_agg(d.name, ', ' ORDER BY d.name), '')
            FROM user_invitation i JOIN role r ON r.id=i.role_id
            LEFT JOIN invitation_department x ON x.invitation_id=i.id LEFT JOIN department d ON d.id=x.department_id
            WHERE i.token_hash=@hash AND i.accepted_at IS NULL AND i.revoked_at IS NULL AND i.expires_at>(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
            GROUP BY i.id,r.name;
            """);
        command.Parameters.AddWithValue("hash", tokenHash);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var departments = reader.GetString(3).Split(", ", StringSplitOptions.RemoveEmptyEntries);
        return new InvitationPreview(reader.GetString(0), reader.GetString(1), reader.GetDateTime(2), departments);
    }

    public async Task<string?> AcceptAsync(string tokenHash, string name, string surname, string passwordHash,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        string email, role;
        int roleId, inviterId, userId;
        await using (var command = new NpgsqlCommand("""
            SELECT i.email,r.name,i.role_id,i.invited_by FROM user_invitation i JOIN role r ON r.id=i.role_id
            WHERE i.token_hash=@hash AND i.accepted_at IS NULL AND i.revoked_at IS NULL AND i.expires_at>(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
            FOR UPDATE OF i;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("hash", tokenHash);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            email = reader.GetString(0); role = reader.GetString(1); roleId = reader.GetInt32(2); inviterId = reader.GetInt32(3);
        }

        string? inviterRole;
        await using (var command = new NpgsqlCommand("SELECT lower(r.name) FROM \"user\" u JOIN role r ON r.id=u.role_id WHERE u.id=@id;", connection, transaction))
        {
            command.Parameters.AddWithValue("id", inviterId); inviterRole = await command.ExecuteScalarAsync(cancellationToken) as string;
        }

        string? existingRole;
        await using (var command = new NpgsqlCommand("""
            SELECT u.id,lower(r.name) FROM "user" u LEFT JOIN role r ON r.id=u.role_id WHERE lower(u.email)=@email FOR UPDATE OF u;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("email", email);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken)) { userId = reader.GetInt32(0); existingRole = reader.IsDBNull(1) ? null : reader.GetString(1); }
            else { userId = 0; existingRole = null; }
        }
        if (existingRole == "superadmin" || ((existingRole is "admin" or "administrator" or "ադմինիստրատոր") && inviterRole != "superadmin"))
            return null;

        if (existingRole is null)
        {
            await using var insert = new NpgsqlCommand("""
                INSERT INTO "user"(role_id,name,surname,email,password_hash,is_active,email_verified,created_at,updated_at)
                VALUES(@roleId,@name,@surname,@email,@passwordHash,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP)
                RETURNING id;
                """, connection, transaction);
            insert.Parameters.AddWithValue("roleId", roleId); insert.Parameters.AddWithValue("name", name.Trim());
            insert.Parameters.AddWithValue("surname", surname.Trim()); insert.Parameters.AddWithValue("email", email);
            insert.Parameters.AddWithValue("passwordHash", passwordHash);
            userId = (int)(await insert.ExecuteScalarAsync(cancellationToken))!;
        }
        else
        {
            await using var update = new NpgsqlCommand("""
                UPDATE "user" SET role_id=@roleId,name=@name,surname=@surname,password_hash=@passwordHash,
                    is_active=TRUE,email_verified=TRUE,updated_at=CURRENT_TIMESTAMP WHERE id=@id;
                """, connection, transaction);
            update.Parameters.AddWithValue("roleId", roleId); update.Parameters.AddWithValue("name", name.Trim());
            update.Parameters.AddWithValue("surname", surname.Trim()); update.Parameters.AddWithValue("passwordHash", passwordHash);
            update.Parameters.AddWithValue("id", userId); await update.ExecuteNonQueryAsync(cancellationToken);
            await using var clearCodes = new NpgsqlCommand("DELETE FROM email_verification_code WHERE user_id=@id; DELETE FROM login_verification_code WHERE user_id=@id;", connection, transaction);
            clearCodes.Parameters.AddWithValue("id", userId); await clearCodes.ExecuteNonQueryAsync(cancellationToken);
        }

        var mappingTable = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ? "admin_department" : "user_department";
        var userColumn = mappingTable == "admin_department" ? "admin_id" : "user_id";
        await using (var clearLinks = new NpgsqlCommand("DELETE FROM admin_department WHERE admin_id=@id; DELETE FROM user_department WHERE user_id=@id;", connection, transaction))
        {
            clearLinks.Parameters.AddWithValue("id", userId); await clearLinks.ExecuteNonQueryAsync(cancellationToken);
        }
        if (!string.Equals(role, "student", StringComparison.OrdinalIgnoreCase))
        {
            await using var clearStudentGroups = new NpgsqlCommand("DELETE FROM student_group WHERE student_id=@id;", connection, transaction);
            clearStudentGroups.Parameters.AddWithValue("id", userId); await clearStudentGroups.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var links = new NpgsqlCommand($"""
            INSERT INTO {mappingTable}({userColumn},department_id)
            SELECT @userId,department_id FROM invitation_department
            WHERE invitation_id=(SELECT id FROM user_invitation WHERE token_hash=@hash);
            """, connection, transaction))
        {
            links.Parameters.AddWithValue("userId", userId); links.Parameters.AddWithValue("hash", tokenHash);
            await links.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var update = new NpgsqlCommand("UPDATE user_invitation SET accepted_at=CURRENT_TIMESTAMP WHERE token_hash=@hash;", connection, transaction))
        {
            update.Parameters.AddWithValue("hash", tokenHash); await update.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return email;
    }

    public async Task<AdminWorkspaceResponse> GetAdminWorkspaceAsync(int adminId, CancellationToken cancellationToken)
    {
        var departments = new List<ManagedDepartmentRow>();
        await using (var command = dataSource.CreateCommand("""
            SELECT d.id,d.name FROM admin_department a JOIN department d ON d.id=a.department_id WHERE a.admin_id=@id ORDER BY d.name;
            """))
        {
            command.Parameters.AddWithValue("id", adminId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) departments.Add(new(reader.GetInt32(0), reader.GetString(1)));
        }
        var staff = new List<ManagedStaffRow>();
        await using (var command = dataSource.CreateCommand("""
            SELECT DISTINCT u.id,COALESCE(u.name,''),COALESCE(u.surname,''),u.email,r.name,COALESCE(u.is_active,FALSE),
                   COALESCE((SELECT string_agg(d2.name, ', ' ORDER BY d2.name) FROM user_department ud2 JOIN department d2 ON d2.id=ud2.department_id WHERE ud2.user_id=u.id),'')
            FROM "user" u JOIN role r ON r.id=u.role_id JOIN user_department ud ON ud.user_id=u.id
            JOIN admin_department ad ON ad.department_id=ud.department_id
            WHERE ad.admin_id=@id AND lower(r.name) IN ('student','lecturer') ORDER BY 3,2,1;
            """))
        {
            command.Parameters.AddWithValue("id", adminId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) staff.Add(new(reader.GetInt32(0),reader.GetString(1),reader.GetString(2),reader.GetString(3),reader.GetString(4),reader.GetBoolean(5),reader.GetString(6)));
        }
        var invitations = new List<InvitationRow>();
        await using (var command = dataSource.CreateCommand("""
            SELECT i.id,i.email,r.name,i.expires_at,COALESCE(string_agg(d.name, ', ' ORDER BY d.name),'')
            FROM user_invitation i JOIN role r ON r.id=i.role_id JOIN invitation_department x ON x.invitation_id=i.id
            JOIN department d ON d.id=x.department_id JOIN admin_department ad ON ad.department_id=d.id
            WHERE ad.admin_id=@id AND i.accepted_at IS NULL AND i.revoked_at IS NULL AND i.expires_at>(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
            GROUP BY i.id,r.name ORDER BY i.created_at DESC;
            """))
        {
            command.Parameters.AddWithValue("id", adminId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) invitations.Add(new(reader.GetInt32(0),reader.GetString(1),reader.GetString(2),reader.GetDateTime(3),reader.GetString(4)));
        }
        return new(departments,staff,invitations);
    }

    public async Task<bool> SetStaffActiveAsync(int adminId, int userId, bool isActive, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            UPDATE "user" u SET is_active=@active,updated_at=CURRENT_TIMESTAMP
            WHERE u.id=@userId AND EXISTS(
                SELECT 1 FROM user_department ud JOIN admin_department ad ON ad.department_id=ud.department_id
                JOIN role r ON r.id=u.role_id
                WHERE ud.user_id=u.id AND ad.admin_id=@adminId AND lower(r.name) IN ('student','lecturer'));
            """);
        command.Parameters.AddWithValue("active", isActive); command.Parameters.AddWithValue("userId", userId); command.Parameters.AddWithValue("adminId", adminId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
