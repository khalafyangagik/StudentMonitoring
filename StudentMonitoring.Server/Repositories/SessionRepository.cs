using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private const string SelectSql = """
        SELECT s.id, so.id, sub.id, sub.name, g.id, g.name,
               so.academic_year, so.semester, s.session_number, s.type::text,
               to_char(s.session_date, 'YYYY-MM-DD'), to_char(s.start_time, 'HH24:MI'),
               to_char(s.end_time, 'HH24:MI'), s.topic, COALESCE(s.is_completed, FALSE), s.room,
               so.lecturer_id, concat_ws(' ', lecturer.name, lecturer.surname)
        FROM "session" AS s
        JOIN subject_offering AS so ON so.id = s.subject_offering_id
        JOIN subject AS sub ON sub.id = so.subject_id
        JOIN "group" AS g ON g.id = so.group_id
        LEFT JOIN "user" AS lecturer ON lecturer.id = so.lecturer_id
        """;

    private readonly NpgsqlDataSource _dataSource;
    public SessionRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<SessionResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = new List<SessionResponse>();
        await using var command = _dataSource.CreateCommand(SelectSql + " ORDER BY s.session_date DESC, s.start_time, s.id;");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) rows.Add(Map(reader));
        return rows;
    }

    public async Task<IReadOnlyList<SessionOptionResponse>> GetOptionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.id,
                   concat_ws(' · ', sub.name, g.name, s.session_date::text,
                             CASE WHEN s.session_number IS NOT NULL THEN '#' || s.session_number::text END),
                   so.group_id
            FROM "session" AS s
            JOIN subject_offering AS so ON so.id = s.subject_offering_id
            LEFT JOIN subject AS sub ON sub.id = so.subject_id
            LEFT JOIN "group" AS g ON g.id = so.group_id
            ORDER BY s.session_date DESC NULLS LAST, s.session_number, s.id;
            """;
        var rows = new List<SessionOptionResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new SessionOptionResponse { Id = reader.GetInt32(0), Label = reader.IsDBNull(1) ? $"Դասաժամ #{reader.GetInt32(0)}" : reader.GetString(1), GroupId = reader.IsDBNull(2) ? null : reader.GetInt32(2) });
        return rows;
    }

    public async Task<bool> SubjectAndGroupExistAsync(int subjectId, int groupId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (SELECT 1 FROM subject WHERE id=@subjectId)
               AND EXISTS (SELECT 1 FROM "group" WHERE id=@groupId);
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("subjectId", subjectId);
        command.Parameters.AddWithValue("groupId", groupId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<IReadOnlyList<LecturerOptionResponse>> GetLecturersAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT u.id, concat_ws(' ', u.name, u.surname)
            FROM "user" AS u
            JOIN role AS r ON r.id = u.role_id
            WHERE COALESCE(u.is_active, TRUE)
              AND (lower(r.name) IN ('lecturer', 'teacher', 'instructor')
                   OR lower(r.name) LIKE '%lectur%'
                   OR lower(r.name) LIKE '%teacher%'
                   OR lower(r.name) LIKE '%դասախոս%')
            ORDER BY u.surname, u.name, u.id;
            """;
        var rows = new List<LecturerOptionResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new LecturerOptionResponse { Id = reader.GetInt32(0), Name = reader.IsDBNull(1) ? $"Դասախոս #{reader.GetInt32(0)}" : reader.GetString(1) });
        return rows;
    }

    public async Task<bool> LecturerExistsAsync(int lecturerId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM "user" AS u
                JOIN role AS r ON r.id = u.role_id
                WHERE u.id=@id AND COALESCE(u.is_active, TRUE)
                  AND (lower(r.name) IN ('lecturer', 'teacher', 'instructor')
                       OR lower(r.name) LIKE '%lectur%'
                       OR lower(r.name) LIKE '%teacher%'
                       OR lower(r.name) LIKE '%դասախոս%')
            );
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", lecturerId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<SessionResponse> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var offeringId = await GetOrCreateOfferingAsync(connection, transaction, request, cancellationToken);
        var sessionNumber = await GetNextSessionNumberAsync(connection, transaction, offeringId, cancellationToken);
        int sessionId;
        await using (var command = new NpgsqlCommand("""
            INSERT INTO "session" (subject_offering_id, session_number, type, session_date,
                start_time, end_time, topic, is_completed, room)
            VALUES (@offeringId, @sessionNumber, @type::session_type, @sessionDate,
                @startTime, @endTime, @topic, @isCompleted, @room)
            RETURNING id;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("offeringId", offeringId);
            command.Parameters.AddWithValue("sessionNumber", sessionNumber);
            AddSessionParameters(command, request);
            sessionId = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
        }
        var result = await GetByIdAsync(connection, transaction, sessionId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result!;
    }

    public async Task<SessionResponse?> UpdateAsync(int id, CreateSessionRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        int previousOfferingId;
        int previousSessionNumber;
        await using (var existsCommand = new NpgsqlCommand("SELECT subject_offering_id, session_number FROM \"session\" WHERE id=@id;", connection, transaction))
        {
            existsCommand.Parameters.AddWithValue("id", id);
            await using var reader = await existsCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }
            previousOfferingId = reader.GetInt32(0);
            previousSessionNumber = reader.GetInt32(1);
        }
        var offeringId = await GetOrCreateOfferingAsync(connection, transaction, request, cancellationToken);
        var sessionNumber = offeringId == previousOfferingId
            ? previousSessionNumber
            : await GetNextSessionNumberAsync(connection, transaction, offeringId, cancellationToken);
        await using (var command = new NpgsqlCommand("""
            UPDATE "session" SET subject_offering_id=@offeringId, session_number=@sessionNumber,
                type=@type::session_type, session_date=@sessionDate, start_time=@startTime,
                end_time=@endTime, topic=@topic, is_completed=@isCompleted, room=@room
            WHERE id=@id;
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("offeringId", offeringId);
            command.Parameters.AddWithValue("sessionNumber", sessionNumber);
            AddSessionParameters(command, request);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        var result = await GetByIdAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("DELETE FROM \"session\" WHERE id=@id;");
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static async Task<int> GetOrCreateOfferingAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        CreateSessionRequest request, CancellationToken cancellationToken)
    {
        await using (var findCommand = new NpgsqlCommand("""
            SELECT id FROM subject_offering
            WHERE subject_id=@subjectId AND group_id=@groupId AND academic_year=@academicYear AND semester=@semester
            ORDER BY id LIMIT 1;
            """, connection, transaction))
        {
            AddOfferingParameters(findCommand, request.SubjectId, request.GroupId, request.AcademicYear, request.Semester);
            var existing = await findCommand.ExecuteScalarAsync(cancellationToken);
            if (existing is not null)
            {
                var offeringId = (int)existing;
                await using var updateCommand = new NpgsqlCommand("UPDATE subject_offering SET lecturer_id=COALESCE(@lecturerId, lecturer_id) WHERE id=@id;", connection, transaction);
                updateCommand.Parameters.AddWithValue("id", offeringId);
                updateCommand.Parameters.Add("lecturerId", NpgsqlDbType.Integer).Value = (object?)request.LecturerId ?? DBNull.Value;
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
                return offeringId;
            }
        }

        await using var insertCommand = new NpgsqlCommand("""
            INSERT INTO subject_offering (subject_id, group_id, lecturer_id, academic_year, semester)
            VALUES (@subjectId, @groupId, @lecturerId, @academicYear, @semester)
            RETURNING id;
            """, connection, transaction);
        AddOfferingParameters(insertCommand, request.SubjectId, request.GroupId, request.AcademicYear, request.Semester);
        insertCommand.Parameters.Add("lecturerId", NpgsqlDbType.Integer).Value = (object?)request.LecturerId ?? DBNull.Value;
        return (int)(await insertCommand.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<int> GetNextSessionNumberAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        int offeringId, CancellationToken cancellationToken)
    {
        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(73021, @offeringId);", connection, transaction))
        {
            lockCommand.Parameters.AddWithValue("offeringId", offeringId);
            await lockCommand.ExecuteScalarAsync(cancellationToken);
        }
        await using var command = new NpgsqlCommand("SELECT COALESCE(MAX(session_number), 0) + 1 FROM \"session\" WHERE subject_offering_id=@offeringId;", connection, transaction);
        command.Parameters.AddWithValue("offeringId", offeringId);
        return (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<SessionResponse?> GetByIdAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        int id, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SelectSql + " WHERE s.id=@id;", connection, transaction);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static void AddOfferingParameters(NpgsqlCommand command, int subjectId, int groupId, string academicYear, int semester)
    {
        command.Parameters.AddWithValue("subjectId", subjectId);
        command.Parameters.AddWithValue("groupId", groupId);
        command.Parameters.AddWithValue("academicYear", academicYear);
        command.Parameters.AddWithValue("semester", semester);
    }

    private static void AddSessionParameters(NpgsqlCommand command, CreateSessionRequest request)
    {
        command.Parameters.AddWithValue("type", request.Type);
        command.Parameters.Add("sessionDate", NpgsqlDbType.Date).Value = request.SessionDate;
        command.Parameters.Add("startTime", NpgsqlDbType.Time).Value = request.StartTime;
        command.Parameters.Add("endTime", NpgsqlDbType.Time).Value = request.EndTime;
        command.Parameters.Add("topic", NpgsqlDbType.Varchar).Value = (object?)request.Topic ?? DBNull.Value;
        command.Parameters.AddWithValue("isCompleted", request.IsCompleted);
        command.Parameters.Add("room", NpgsqlDbType.Integer).Value = (object?)request.Room ?? DBNull.Value;
    }

    private static SessionResponse Map(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0), SubjectOfferingId = reader.GetInt32(1), SubjectId = reader.GetInt32(2),
        SubjectName = reader.IsDBNull(3) ? null : reader.GetString(3), GroupId = reader.GetInt32(4),
        GroupName = reader.IsDBNull(5) ? null : reader.GetString(5), AcademicYear = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
        Semester = reader.IsDBNull(7) ? 0 : reader.GetInt32(7), SessionNumber = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
        Type = reader.IsDBNull(9) ? string.Empty : reader.GetString(9), SessionDate = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
        StartTime = reader.IsDBNull(11) ? string.Empty : reader.GetString(11), EndTime = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
        Topic = reader.IsDBNull(13) ? null : reader.GetString(13), IsCompleted = reader.GetBoolean(14),
        Room = reader.IsDBNull(15) ? null : reader.GetInt32(15),
        LecturerId = reader.IsDBNull(16) ? null : reader.GetInt32(16), LecturerName = reader.IsDBNull(17) ? null : reader.GetString(17)
    };
}
