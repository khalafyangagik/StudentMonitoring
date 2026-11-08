using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class LecturerGradeRepository(NpgsqlDataSource dataSource) : ILecturerGradeRepository
{
    public async Task<IReadOnlyList<LecturerStudentOption>?> GetStudentsAsync(int lecturerId, int offeringId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT u.id, concat_ws(' ', u.name, u.surname), COALESCE(u.email, '')
            FROM subject_offering so
            JOIN student_group sg ON sg.group_id=so.group_id AND sg.status='active'::student_group_status
            JOIN "user" u ON u.id=sg.student_id AND COALESCE(u.is_active,FALSE)
            JOIN role r ON r.id=u.role_id AND lower(r.name) IN ('student','ուսանող')
            WHERE so.id=@offeringId AND so.lecturer_id=@lecturerId
            ORDER BY u.surname,u.name,u.id;
            """;
        var students = new List<LecturerStudentOption>();
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        command.Parameters.AddWithValue("offeringId", offeringId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            students.Add(new LecturerStudentOption(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        if (students.Count > 0) return students;

        await using var exists = dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM subject_offering WHERE id=@offeringId AND lecturer_id=@lecturerId);");
        exists.Parameters.AddWithValue("lecturerId", lecturerId);
        exists.Parameters.AddWithValue("offeringId", offeringId);
        return (bool)(await exists.ExecuteScalarAsync(cancellationToken) ?? false) ? students : null;
    }

    public async Task<IReadOnlyList<LecturerSessionOption>?> GetSessionsAsync(int lecturerId, int offeringId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.id, to_char(s.session_date,'YYYY-MM-DD'),
                   concat(to_char(s.session_date,'DD.MM.YYYY'), ' · ',
                       COALESCE('#' || s.session_number::text, 'Դասաժամ'),
                       CASE WHEN NULLIF(s.topic,'') IS NOT NULL THEN ' · ' || s.topic ELSE '' END)
            FROM subject_offering so JOIN "session" s ON s.subject_offering_id=so.id
            WHERE so.id=@offeringId AND so.lecturer_id=@lecturerId
            ORDER BY s.session_date DESC, s.start_time DESC, s.id DESC;
            """;
        var sessions = new List<LecturerSessionOption>();
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        command.Parameters.AddWithValue("offeringId", offeringId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            sessions.Add(new LecturerSessionOption(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        if (sessions.Count > 0) return sessions;

        await using var exists = dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM subject_offering WHERE id=@offeringId AND lecturer_id=@lecturerId);");
        exists.Parameters.AddWithValue("lecturerId", lecturerId);
        exists.Parameters.AddWithValue("offeringId", offeringId);
        return (bool)(await exists.ExecuteScalarAsync(cancellationToken) ?? false) ? sessions : null;
    }

    public async Task<LecturerGradeCreated?> CreateAsync(int lecturerId, CreateLecturerGradeRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO grade (student_id, subject_offering_id, session_id, type, value, weight, comment)
            SELECT @studentId, so.id, s.id, @type::grade_type, @value, @weight, @comment
            FROM subject_offering so
            JOIN "session" s ON s.id=@sessionId AND s.subject_offering_id=so.id
            JOIN student_group sg ON sg.group_id=so.group_id AND sg.student_id=@studentId
                AND sg.status='active'::student_group_status
            JOIN "user" u ON u.id=sg.student_id AND COALESCE(u.is_active,FALSE)
            JOIN role r ON r.id=u.role_id AND lower(r.name) IN ('student','ուսանող')
            WHERE so.id=@offeringId AND so.lecturer_id=@lecturerId
            RETURNING id, student_id, subject_offering_id, session_id, type::text, value, weight, comment;
            """;
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        command.Parameters.AddWithValue("studentId", request.StudentId);
        command.Parameters.AddWithValue("offeringId", request.OfferingId);
        command.Parameters.AddWithValue("sessionId", request.SessionId);
        command.Parameters.AddWithValue("type", request.Type);
        command.Parameters.AddWithValue("value", request.Value);
        command.Parameters.AddWithValue("weight", request.Weight);
        command.Parameters.Add("comment", NpgsqlDbType.Text).Value = (object?)Clean(request.Comment) ?? DBNull.Value;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new LecturerGradeCreated(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3),
            reader.GetString(4), reader.GetDecimal(5), reader.GetDecimal(6), reader.IsDBNull(7) ? null : reader.GetString(7));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
