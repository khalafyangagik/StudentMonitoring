using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class AttendanceRepository : IAttendanceRepository
{
    private const string SelectSql = """
        SELECT a.id, a.student_id, concat_ws(' ', u.name, u.surname), a.session_id,
               concat_ws(' · ', sub.name, g.name, s.session_date::text,
                         CASE WHEN s.session_number IS NOT NULL THEN '#' || s.session_number::text END),
               a.status::text, a.comment, a.recorded_at
        FROM attendance AS a
        LEFT JOIN "user" AS u ON u.id = a.student_id
        LEFT JOIN "session" AS s ON s.id = a.session_id
        LEFT JOIN subject_offering AS so ON so.id = s.subject_offering_id
        LEFT JOIN subject AS sub ON sub.id = so.subject_id
        LEFT JOIN "group" AS g ON g.id = so.group_id
        """;

    private readonly NpgsqlDataSource _dataSource;
    public AttendanceRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<AttendanceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = new List<AttendanceResponse>();
        await using var command = _dataSource.CreateCommand(SelectSql + " ORDER BY a.recorded_at DESC NULLS LAST, a.id DESC;");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) rows.Add(Map(reader));
        return rows;
    }

    public async Task<IReadOnlyList<AttendanceResponse>> GetForLecturerAsync(int lecturerId, CancellationToken cancellationToken)
    {
        var rows = new List<AttendanceResponse>();
        await using var command = _dataSource.CreateCommand(SelectSql + " WHERE so.lecturer_id=@lecturerId ORDER BY a.recorded_at DESC NULLS LAST, a.id DESC;");
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) rows.Add(Map(reader));
        return rows;
    }

    public async Task<IReadOnlyList<SessionOptionResponse>> GetSessionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.id,
                   concat_ws(' · ', sub.name, g.name, s.session_date::text,
                             CASE WHEN s.session_number IS NOT NULL THEN '#' || s.session_number::text END)
            FROM "session" AS s
            LEFT JOIN subject_offering AS so ON so.id = s.subject_offering_id
            LEFT JOIN subject AS sub ON sub.id = so.subject_id
            LEFT JOIN "group" AS g ON g.id = so.group_id
            ORDER BY s.session_date DESC NULLS LAST, s.session_number, s.id;
            """;
        var rows = new List<SessionOptionResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new SessionOptionResponse { Id = reader.GetInt32(0), Label = reader.IsDBNull(1) ? $"Դասաժամ #{reader.GetInt32(0)}" : reader.GetString(1) });
        return rows;
    }

    public async Task<IReadOnlyList<SessionOptionResponse>> GetSessionsForLecturerAsync(int lecturerId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.id,
                   concat_ws(' · ', sub.name, g.name, s.session_date::text,
                             CASE WHEN s.session_number IS NOT NULL THEN '#' || s.session_number::text END),
                   so.group_id
            FROM "session" AS s
            JOIN subject_offering AS so ON so.id=s.subject_offering_id
            JOIN subject AS sub ON sub.id=so.subject_id
            JOIN "group" AS g ON g.id=so.group_id
            WHERE so.lecturer_id=@lecturerId
            ORDER BY s.session_date DESC NULLS LAST, s.session_number, s.id;
            """;
        var rows = new List<SessionOptionResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new SessionOptionResponse { Id = reader.GetInt32(0), Label = reader.IsDBNull(1) ? $"Դասաժամ #{reader.GetInt32(0)}" : reader.GetString(1), GroupId = reader.GetInt32(2) });
        return rows;
    }

    public async Task<IReadOnlyList<StudentResponse>> GetStudentsForLecturerAsync(int lecturerId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT ON (u.id, sg.group_id) u.id, u.name, u.surname, u.email, u.phone,
                   COALESCE(u.is_active, TRUE), sg.group_id, g.name
            FROM subject_offering so
            JOIN student_group sg ON sg.group_id=so.group_id AND sg.status='active'::student_group_status
            JOIN "user" u ON u.id=sg.student_id
            JOIN "group" g ON g.id=sg.group_id
            WHERE so.lecturer_id=@lecturerId
            ORDER BY u.id, sg.group_id, so.id;
            """;
        var rows = new List<StudentResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new StudentResponse { Id = reader.GetInt32(0), Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                Surname = reader.IsDBNull(2) ? null : reader.GetString(2), Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                Phone = reader.IsDBNull(4) ? null : reader.GetString(4), IsActive = reader.GetBoolean(5),
                GroupId = reader.GetInt32(6), GroupName = reader.IsDBNull(7) ? null : reader.GetString(7) });
        return rows;
    }

    public async Task<bool> StudentCanAttendSessionAsync(int studentId, int sessionId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM "user" AS u
                JOIN student_group AS sg ON sg.student_id = u.id
                    AND sg.status = 'active'::student_group_status
                JOIN "session" AS s ON s.id = @sessionId
                JOIN subject_offering AS so ON so.id = s.subject_offering_id
                    AND so.group_id = sg.group_id
                WHERE u.id = @studentId
            );
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("studentId", studentId);
        command.Parameters.AddWithValue("sessionId", sessionId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<bool> StudentCanAttendLecturerSessionAsync(int lecturerId, int studentId, int sessionId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM "session" s
                JOIN subject_offering so ON so.id=s.subject_offering_id AND so.lecturer_id=@lecturerId
                JOIN student_group sg ON sg.group_id=so.group_id AND sg.student_id=@studentId
                    AND sg.status='active'::student_group_status
                WHERE s.id=@sessionId
            );
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        command.Parameters.AddWithValue("studentId", studentId);
        command.Parameters.AddWithValue("sessionId", sessionId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<bool> LecturerOwnsAttendanceAsync(int lecturerId, int attendanceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (SELECT 1 FROM attendance a JOIN "session" s ON s.id=a.session_id
                JOIN subject_offering so ON so.id=s.subject_offering_id
                WHERE a.id=@attendanceId AND so.lecturer_id=@lecturerId);
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        command.Parameters.AddWithValue("attendanceId", attendanceId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<bool> AttendanceExistsAsync(int studentId, int sessionId, int? excludingId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM attendance
                WHERE student_id = @studentId AND session_id = @sessionId
                  AND (@excludingId IS NULL OR id <> @excludingId)
            );
            """;
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("studentId", studentId);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.Add("excludingId", NpgsqlDbType.Integer).Value = (object?)excludingId ?? DBNull.Value;
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<AttendanceResponse> CreateAsync(CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH inserted AS (
                INSERT INTO attendance (student_id, session_id, status, comment)
                VALUES (@studentId, @sessionId, @status::attendance_status, @comment)
                RETURNING id
            )
            """ + SelectSql + " JOIN inserted AS i ON i.id = a.id;";
        await using var command = _dataSource.CreateCommand(sql);
        AddParameters(command, request);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return Map(reader);
    }

    public async Task<AttendanceResponse?> UpdateAsync(int id, CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH updated AS (
                UPDATE attendance
                SET student_id=@studentId, session_id=@sessionId,
                    status=@status::attendance_status, comment=@comment
                WHERE id=@id
                RETURNING id
            )
            """ + SelectSql + " JOIN updated AS u2 ON u2.id = a.id;";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        AddParameters(command, request);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("DELETE FROM attendance WHERE id=@id;");
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddParameters(NpgsqlCommand command, CreateAttendanceRequest request)
    {
        command.Parameters.AddWithValue("studentId", request.StudentId);
        command.Parameters.AddWithValue("sessionId", request.SessionId);
        command.Parameters.AddWithValue("status", request.Status);
        command.Parameters.Add("comment", NpgsqlDbType.Text).Value = (object?)request.Comment ?? DBNull.Value;
    }

    private static AttendanceResponse Map(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0), StudentId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
        StudentName = reader.IsDBNull(2) ? null : reader.GetString(2),
        SessionId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
        SessionLabel = reader.IsDBNull(4) ? null : reader.GetString(4),
        Status = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
        Comment = reader.IsDBNull(6) ? null : reader.GetString(6),
        RecordedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    };
}
