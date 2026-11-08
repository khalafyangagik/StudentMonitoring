using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class StudentRepository : IStudentRepository
{
    private const string SelectSql = """
        SELECT u.id, u.name, u.surname, u.email, u.phone, COALESCE(u.is_active, TRUE), sg.group_id, g.name
        FROM "user" AS u
        LEFT JOIN LATERAL (
            SELECT student_id, group_id
            FROM student_group
            WHERE student_id = u.id
            ORDER BY joined_at DESC NULLS LAST, group_id
            LIMIT 1
        ) AS sg ON TRUE
        LEFT JOIN "group" AS g ON g.id = sg.group_id
        LEFT JOIN role AS r ON r.id = u.role_id
        WHERE (lower(COALESCE(r.name, '')) IN ('student', 'ուսանող')
           OR EXISTS (SELECT 1 FROM student_group sg2 WHERE sg2.student_id = u.id))
        """;

    private readonly NpgsqlDataSource _dataSource;
    public StudentRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<StudentResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new List<StudentResponse>();
        await using var command = _dataSource.CreateCommand(SelectSql + " ORDER BY u.id;");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Map(reader));
        return result;
    }

    public async Task<bool> GroupExistsAsync(int groupId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("SELECT EXISTS (SELECT 1 FROM \"group\" WHERE id = @id);");
        command.Parameters.AddWithValue("id", groupId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        int studentId;
        await using (var command = new NpgsqlCommand("""
            INSERT INTO "user" (role_id, name, surname, email, phone, is_active)
            VALUES (
                (SELECT id FROM role WHERE lower(name) IN ('student', 'ուսանող')
                 ORDER BY CASE WHEN lower(name) = 'student' THEN 0 ELSE 1 END, id LIMIT 1),
                @name, @surname, @email, @phone, TRUE)
            RETURNING id;
            """, connection, transaction))
        {
            AddParameters(command, request);
            studentId = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
        }
        await AddMembershipAsync(connection, transaction, studentId, request.GroupId, cancellationToken);
        var student = await GetOneAsync(connection, transaction, studentId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return student!;
    }

    public async Task<StudentResponse?> UpdateAsync(int studentId, CreateStudentRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var command = new NpgsqlCommand("""
            UPDATE "user" SET name=@name, surname=@surname, email=@email, phone=@phone, updated_at=CURRENT_TIMESTAMP
            WHERE id=@id;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("id", studentId);
            AddParameters(command, request);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
        }
        await using (var command = new NpgsqlCommand("""
            UPDATE student_group
            SET status = 'inactive'::student_group_status
            WHERE student_id=@id AND status = 'active'::student_group_status;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("id", studentId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await AddMembershipAsync(connection, transaction, studentId, request.GroupId, cancellationToken);
        var student = await GetOneAsync(connection, transaction, studentId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return student;
    }

    public async Task<bool> DeleteAsync(int studentId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("DELETE FROM \"user\" WHERE id=@id;");
        command.Parameters.AddWithValue("id", studentId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static async Task AddMembershipAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        int studentId, int groupId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO student_group (student_id, group_id, joined_at, status)
            VALUES (@studentId, @groupId, CURRENT_TIMESTAMP, 'active'::student_group_status)
            ON CONFLICT (student_id, group_id) DO UPDATE
            SET joined_at = EXCLUDED.joined_at, status = EXCLUDED.status;
            """, connection, transaction);
        command.Parameters.AddWithValue("studentId", studentId);
        command.Parameters.AddWithValue("groupId", groupId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<StudentResponse?> GetOneAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        int studentId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql + " AND u.id=@id;", connection, transaction);
        command.Parameters.AddWithValue("id", studentId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static void AddParameters(NpgsqlCommand command, CreateStudentRequest request)
    {
        command.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, request.Name);
        command.Parameters.AddWithValue("surname", NpgsqlDbType.Varchar, request.Surname);
        command.Parameters.AddWithValue("email", NpgsqlDbType.Varchar, request.Email);
        command.Parameters.Add("phone", NpgsqlDbType.Varchar).Value = (object?)request.Phone ?? DBNull.Value;
    }

    private static StudentResponse Map(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0), Name = reader.IsDBNull(1) ? null : reader.GetString(1),
        Surname = reader.IsDBNull(2) ? null : reader.GetString(2), Email = reader.IsDBNull(3) ? null : reader.GetString(3),
        Phone = reader.IsDBNull(4) ? null : reader.GetString(4), IsActive = reader.GetBoolean(5),
        GroupId = reader.IsDBNull(6) ? null : reader.GetInt32(6), GroupName = reader.IsDBNull(7) ? null : reader.GetString(7)
    };
}
