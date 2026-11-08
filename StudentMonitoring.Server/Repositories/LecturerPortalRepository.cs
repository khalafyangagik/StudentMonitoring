using Npgsql;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class LecturerPortalRepository(NpgsqlDataSource dataSource) : ILecturerPortalRepository
{
    public async Task<LecturerPortalSummary?> GetSummaryAsync(int lecturerId, CancellationToken cancellationToken)
    {
        string name, surname, email;
        await using (var command = dataSource.CreateCommand("""
            SELECT COALESCE(u.name,''), COALESCE(u.surname,''), COALESCE(u.email,'')
            FROM "user" u JOIN role r ON r.id=u.role_id
            WHERE u.id=@id AND COALESCE(u.is_active,FALSE) AND COALESCE(u.email_verified,FALSE)
              AND lower(r.name) IN ('lecturer','teacher','instructor','դասախոս');
            """))
        {
            command.Parameters.AddWithValue("id", lecturerId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            name = reader.GetString(0); surname = reader.GetString(1); email = reader.GetString(2);
        }

        int totalCourses, totalGroups, totalStudents, upcomingCount;
        int attendanceTotal, attendancePresent, attendanceAbsent, attendanceLate;
        decimal? attendanceRate, averageGrade;
        await using (var command = dataSource.CreateCommand("""
            SELECT
                (SELECT COUNT(*)::int FROM subject_offering WHERE lecturer_id=@id),
                (SELECT COUNT(DISTINCT group_id)::int FROM subject_offering WHERE lecturer_id=@id),
                (SELECT COUNT(DISTINCT sg.student_id)::int
                 FROM subject_offering so JOIN student_group sg ON sg.group_id=so.group_id
                 WHERE so.lecturer_id=@id AND sg.status='active'::student_group_status),
                (SELECT COUNT(*)::int FROM "session" s JOIN subject_offering so ON so.id=s.subject_offering_id
                 WHERE so.lecturer_id=@id AND s.session_date>=CURRENT_DATE AND COALESCE(s.is_completed,FALSE)=FALSE),
                (SELECT COUNT(*)::int FROM attendance a JOIN "session" s ON s.id=a.session_id
                 JOIN subject_offering so ON so.id=s.subject_offering_id WHERE so.lecturer_id=@id),
                (SELECT COUNT(*) FILTER (WHERE a.status='present'::attendance_status)::int
                 FROM attendance a JOIN "session" s ON s.id=a.session_id JOIN subject_offering so ON so.id=s.subject_offering_id WHERE so.lecturer_id=@id),
                (SELECT COUNT(*) FILTER (WHERE a.status='absent'::attendance_status)::int
                 FROM attendance a JOIN "session" s ON s.id=a.session_id JOIN subject_offering so ON so.id=s.subject_offering_id WHERE so.lecturer_id=@id),
                (SELECT COUNT(*) FILTER (WHERE a.status='late'::attendance_status)::int
                 FROM attendance a JOIN "session" s ON s.id=a.session_id JOIN subject_offering so ON so.id=s.subject_offering_id WHERE so.lecturer_id=@id),
                (SELECT ROUND(100.0 * COUNT(*) FILTER (WHERE a.status='present'::attendance_status) / NULLIF(COUNT(*),0),1)
                 FROM attendance a JOIN "session" s ON s.id=a.session_id JOIN subject_offering so ON so.id=s.subject_offering_id WHERE so.lecturer_id=@id),
                (SELECT ROUND(SUM(g.value*COALESCE(g.weight,1))/NULLIF(SUM(COALESCE(g.weight,1)),0),2)
                 FROM grade g JOIN subject_offering so ON so.id=g.subject_offering_id WHERE so.lecturer_id=@id AND g.value IS NOT NULL);
            """))
        {
            command.Parameters.AddWithValue("id", lecturerId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            totalCourses = reader.GetInt32(0); totalGroups = reader.GetInt32(1); totalStudents = reader.GetInt32(2);
            upcomingCount = reader.GetInt32(3); attendanceTotal = reader.GetInt32(4); attendancePresent = reader.GetInt32(5);
            attendanceAbsent = reader.GetInt32(6); attendanceLate = reader.GetInt32(7);
            attendanceRate = reader.IsDBNull(8) ? null : reader.GetDecimal(8);
            averageGrade = reader.IsDBNull(9) ? null : reader.GetDecimal(9);
        }

        var courses = new List<LecturerPortalCourse>();
        await using (var command = dataSource.CreateCommand("""
            WITH students AS (
                SELECT group_id, COUNT(DISTINCT student_id)::int AS total
                FROM student_group WHERE status='active'::student_group_status GROUP BY group_id
            ), grades AS (
                SELECT subject_offering_id,
                       ROUND(SUM(value*COALESCE(weight,1))/NULLIF(SUM(COALESCE(weight,1)),0),2) AS average
                FROM grade WHERE value IS NOT NULL GROUP BY subject_offering_id
            ), sessions AS (
                SELECT subject_offering_id, COUNT(*)::int AS total FROM "session" GROUP BY subject_offering_id
            )
            SELECT so.id, COALESCE(sub.name,''), COALESCE(g.name,''), c.name,
                   COALESCE(so.academic_year,''), COALESCE(so.semester,0), COALESCE(st.total,0), gr.average, COALESCE(se.total,0)
            FROM subject_offering so JOIN subject sub ON sub.id=so.subject_id
            JOIN "group" g ON g.id=so.group_id LEFT JOIN course c ON c.id=g.course_id
            LEFT JOIN students st ON st.group_id=g.id LEFT JOIN grades gr ON gr.subject_offering_id=so.id
            LEFT JOIN sessions se ON se.subject_offering_id=so.id
            WHERE so.lecturer_id=@id ORDER BY sub.name,g.name,so.academic_year,so.semester,so.id;
            """))
        {
            command.Parameters.AddWithValue("id", lecturerId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                courses.Add(new LecturerPortalCourse(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4), reader.GetInt32(5),
                    reader.GetInt32(6), reader.IsDBNull(7) ? null : reader.GetDecimal(7), reader.GetInt32(8)));
        }

        var groupAnalytics = new List<LecturerPortalGroup>();
        await using (var command = dataSource.CreateCommand("""
            WITH students AS (
                SELECT group_id, COUNT(DISTINCT student_id)::int AS total
                FROM student_group WHERE status='active'::student_group_status GROUP BY group_id
            ), grades AS (
                SELECT subject_offering_id,
                       ROUND(SUM(value*COALESCE(weight,1))/NULLIF(SUM(COALESCE(weight,1)),0),2) AS average
                FROM grade WHERE value IS NOT NULL GROUP BY subject_offering_id
            ), attendance_stats AS (
                SELECT so.id AS offering_id, COUNT(a.id)::int AS total,
                       COUNT(a.id) FILTER (WHERE a.status='absent'::attendance_status)::int AS absences,
                       ROUND(100.0*COUNT(a.id) FILTER (WHERE a.status='absent'::attendance_status)/NULLIF(COUNT(a.id),0),1) AS absence_rate
                FROM subject_offering so JOIN "session" s ON s.subject_offering_id=so.id
                LEFT JOIN attendance a ON a.session_id=s.id
                WHERE so.lecturer_id=@id GROUP BY so.id
            )
            SELECT so.id, COALESCE(sub.name,''), COALESCE(g.name,''), COALESCE(st.total,0), gr.average,
                   COALESCE(a.total,0), COALESCE(a.absences,0), a.absence_rate
            FROM subject_offering so JOIN subject sub ON sub.id=so.subject_id JOIN "group" g ON g.id=so.group_id
            LEFT JOIN students st ON st.group_id=g.id LEFT JOIN grades gr ON gr.subject_offering_id=so.id
            LEFT JOIN attendance_stats a ON a.offering_id=so.id
            WHERE so.lecturer_id=@id ORDER BY g.name,sub.name,so.id;
            """))
        {
            command.Parameters.AddWithValue("id", lecturerId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                groupAnalytics.Add(new LecturerPortalGroup(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                    reader.GetInt32(3), reader.IsDBNull(4) ? null : reader.GetDecimal(4), reader.GetInt32(5),
                    reader.GetInt32(6), reader.IsDBNull(7) ? null : reader.GetDecimal(7)));
        }

        var upcomingSessions = new List<LecturerPortalSession>();
        await using (var command = dataSource.CreateCommand("""
            SELECT s.id, COALESCE(sub.name,''), COALESCE(g.name,''), to_char(s.session_date,'YYYY-MM-DD'),
                   to_char(s.start_time,'HH24:MI'), to_char(s.end_time,'HH24:MI'), s.topic, s.type::text, s.room
            FROM "session" s JOIN subject_offering so ON so.id=s.subject_offering_id
            JOIN subject sub ON sub.id=so.subject_id JOIN "group" g ON g.id=so.group_id
            WHERE so.lecturer_id=@id AND s.session_date>=CURRENT_DATE AND COALESCE(s.is_completed,FALSE)=FALSE
            ORDER BY s.session_date,s.start_time LIMIT 8;
            """))
        {
            command.Parameters.AddWithValue("id", lecturerId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                upcomingSessions.Add(new LecturerPortalSession(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetInt32(8)));
        }

        var recentGrades = new List<LecturerPortalGrade>();
        await using (var command = dataSource.CreateCommand("""
            SELECT gr.id, concat_ws(' ',u.name,u.surname), sub.name, g.name, gr.type::text, gr.value, gr.weight, gr.comment
            FROM grade gr JOIN subject_offering so ON so.id=gr.subject_offering_id
            JOIN "user" u ON u.id=gr.student_id JOIN subject sub ON sub.id=so.subject_id JOIN "group" g ON g.id=so.group_id
            WHERE so.lecturer_id=@id AND gr.value IS NOT NULL
            ORDER BY gr.id DESC LIMIT 8;
            """))
        {
            command.Parameters.AddWithValue("id", lecturerId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                recentGrades.Add(new LecturerPortalGrade(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetDecimal(5), reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7)));
        }

        return new LecturerPortalSummary
        {
            LecturerId = lecturerId, Name = name, Surname = surname, Email = email,
            TotalCourses = totalCourses, TotalGroups = totalGroups, TotalStudents = totalStudents,
            UpcomingSessionCount = upcomingCount, AttendanceTotal = attendanceTotal, AttendancePresent = attendancePresent,
            AttendanceAbsent = attendanceAbsent, AttendanceLate = attendanceLate, AttendanceRate = attendanceRate,
            AverageGrade = averageGrade, Courses = courses, GroupAnalytics = groupAnalytics,
            UpcomingSessions = upcomingSessions, RecentGrades = recentGrades
        };
    }
}
