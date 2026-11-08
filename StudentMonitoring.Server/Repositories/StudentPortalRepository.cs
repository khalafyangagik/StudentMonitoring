using Npgsql;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class StudentPortalRepository(NpgsqlDataSource dataSource) : IStudentPortalRepository
{
    public async Task<StudentPortalSummary?> GetSummaryAsync(int studentId, CancellationToken cancellationToken)
    {
        string name, surname, email, role;
        await using (var command = dataSource.CreateCommand("""
            SELECT COALESCE(u.name, ''), COALESCE(u.surname, ''), COALESCE(u.email, ''), COALESCE(r.name, 'student')
            FROM "user" u LEFT JOIN role r ON r.id=u.role_id
            WHERE u.id=@id AND COALESCE(u.is_active, FALSE) AND COALESCE(u.email_verified, FALSE);
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            name = reader.GetString(0); surname = reader.GetString(1); email = reader.GetString(2); role = reader.GetString(3);
        }

        var groups = new List<string>();
        await using (var command = dataSource.CreateCommand("""
            SELECT DISTINCT g.name
            FROM student_group sg JOIN "group" g ON g.id=sg.group_id
            WHERE sg.student_id=@id AND sg.status='active'::student_group_status
            ORDER BY g.name;
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) if (!reader.IsDBNull(0)) groups.Add(reader.GetString(0));
        }

        var subjects = new List<StudentPortalSubject>();
        await using (var command = dataSource.CreateCommand("""
            WITH grade_stats AS (
                SELECT subject_offering_id,
                       ROUND(SUM(value*COALESCE(weight,1))/NULLIF(SUM(COALESCE(weight,1)),0),2) AS average,
                       COUNT(*)::int AS grade_count
                FROM grade WHERE student_id=@id AND value IS NOT NULL GROUP BY subject_offering_id
            ), attendance_stats AS (
                SELECT s.subject_offering_id, COUNT(a.id)::int AS total,
                       COUNT(a.id) FILTER (WHERE a.status='present'::attendance_status)::int AS present,
                       COUNT(a.id) FILTER (WHERE a.status='absent'::attendance_status)::int AS absent,
                       COUNT(a.id) FILTER (WHERE a.status='late'::attendance_status)::int AS late
                FROM "session" s LEFT JOIN attendance a ON a.session_id=s.id AND a.student_id=@id
                GROUP BY s.subject_offering_id
            )
            SELECT so.id, sub.name, c.name, COALESCE(so.academic_year, ''), COALESCE(so.semester, 0),
                   g.name, concat_ws(' ', lecturer.name, lecturer.surname), gs.average, COALESCE(gs.grade_count,0),
                   COALESCE(ast.total,0), COALESCE(ast.present,0), COALESCE(ast.absent,0), COALESCE(ast.late,0)
            FROM student_group sg
            JOIN "group" g ON g.id=sg.group_id
            JOIN subject_offering so ON so.group_id=g.id
            JOIN subject sub ON sub.id=so.subject_id
            LEFT JOIN course c ON c.id=g.course_id
            LEFT JOIN "user" lecturer ON lecturer.id=so.lecturer_id
            LEFT JOIN grade_stats gs ON gs.subject_offering_id=so.id
            LEFT JOIN attendance_stats ast ON ast.subject_offering_id=so.id
            WHERE sg.student_id=@id AND sg.status='active'::student_group_status
            ORDER BY sub.name, g.name, COALESCE(so.academic_year, ''), COALESCE(so.semester, 0), so.id;
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                subjects.Add(new StudentPortalSubject
                {
                    OfferingId = reader.GetInt32(0), SubjectName = reader.GetString(1),
                    CourseName = reader.IsDBNull(2) ? null : reader.GetString(2), AcademicYear = reader.GetString(3),
                    Semester = reader.GetInt32(4), GroupName = reader.GetString(5),
                    LecturerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    AverageGrade = reader.IsDBNull(7) ? null : reader.GetDecimal(7), GradeCount = reader.GetInt32(8),
                    AttendanceTotal = reader.GetInt32(9), AttendancePresent = reader.GetInt32(10),
                    AttendanceAbsent = reader.GetInt32(11), AttendanceLate = reader.GetInt32(12)
                });
        }

        var subjectsByOffering = subjects.ToDictionary(subject => subject.OfferingId);
        await using (var command = dataSource.CreateCommand("""
            SELECT so.id, gr.type::text, gr.value, gr.weight, gr.comment, to_char(s.session_date,'YYYY-MM-DD')
            FROM grade gr JOIN subject_offering so ON so.id=gr.subject_offering_id
            LEFT JOIN "session" s ON s.id=gr.session_id
            WHERE gr.student_id=@id AND gr.value IS NOT NULL
            ORDER BY s.session_date DESC NULLS LAST, gr.id DESC;
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                if (subjectsByOffering.TryGetValue(reader.GetInt32(0), out var subject))
                    subject.Grades.Add(new StudentPortalSubjectGrade(reader.IsDBNull(1) ? "" : reader.GetString(1),
                        reader.GetDecimal(2), reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                        reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        var monthlyAverages = new List<StudentPortalMonthlyAverage>();
        await using (var command = dataSource.CreateCommand("""
            SELECT to_char(date_trunc('month', s.session_date), 'YYYY-MM'),
                   ROUND(SUM(gr.value*COALESCE(gr.weight,1))/NULLIF(SUM(COALESCE(gr.weight,1)),0),2),
                   COUNT(*)::int
            FROM grade gr JOIN "session" s ON s.id=gr.session_id
            WHERE gr.student_id=@id AND gr.value IS NOT NULL
              AND s.session_date >= date_trunc('month', CURRENT_DATE) - INTERVAL '5 months'
              AND s.session_date < date_trunc('month', CURRENT_DATE) + INTERVAL '1 month'
            GROUP BY date_trunc('month', s.session_date)
            ORDER BY date_trunc('month', s.session_date);
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                monthlyAverages.Add(new StudentPortalMonthlyAverage(reader.GetString(0), reader.GetDecimal(1), reader.GetInt32(2)));
        }

        int attendanceTotal, present, absent, late;
        await using (var command = dataSource.CreateCommand("""
            SELECT COUNT(*), COUNT(*) FILTER (WHERE a.status='present'::attendance_status),
                   COUNT(*) FILTER (WHERE a.status='absent'::attendance_status),
                   COUNT(*) FILTER (WHERE a.status='late'::attendance_status)
            FROM attendance a WHERE a.student_id=@id;
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            attendanceTotal = Convert.ToInt32(reader.GetInt64(0)); present = Convert.ToInt32(reader.GetInt64(1));
            absent = Convert.ToInt32(reader.GetInt64(2)); late = Convert.ToInt32(reader.GetInt64(3));
        }

        decimal? average;
        await using (var command = dataSource.CreateCommand("""
            SELECT SUM(value * COALESCE(weight, 1)) / NULLIF(SUM(COALESCE(weight, 1)), 0)
            FROM grade WHERE student_id=@id AND value IS NOT NULL;
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            average = result is null or DBNull ? null : Convert.ToDecimal(result);
        }

        var upcoming = new List<StudentPortalSession>();
        await using (var command = dataSource.CreateCommand("""
            SELECT s.id, sub.name, concat_ws(' ', lecturer.name, lecturer.surname),
                   to_char(s.session_date, 'YYYY-MM-DD'), to_char(s.start_time, 'HH24:MI'),
                   to_char(s.end_time, 'HH24:MI'), s.topic, s.room, s.type::text
            FROM student_group sg
            JOIN subject_offering so ON so.group_id=sg.group_id
            JOIN "session" s ON s.subject_offering_id=so.id
            JOIN subject sub ON sub.id=so.subject_id
            LEFT JOIN "user" lecturer ON lecturer.id=so.lecturer_id
            WHERE sg.student_id=@id AND sg.status='active'::student_group_status
              AND s.session_date >= CURRENT_DATE AND COALESCE(s.is_completed, FALSE)=FALSE
            ORDER BY s.session_date, s.start_time
            LIMIT 6;
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                upcoming.Add(new StudentPortalSession(reader.GetInt32(0), reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetInt32(7), reader.IsDBNull(8) ? "" : reader.GetString(8)));
        }

        var grades = new List<StudentPortalGrade>();
        await using (var command = dataSource.CreateCommand("""
            SELECT sub.name, g.type::text, g.value, g.weight, g.comment, to_char(s.session_date,'YYYY-MM-DD')
            FROM grade g
            JOIN subject_offering so ON so.id=g.subject_offering_id
            JOIN subject sub ON sub.id=so.subject_id
            LEFT JOIN "session" s ON s.id=g.session_id
            WHERE g.student_id=@id AND g.value IS NOT NULL
            ORDER BY s.session_date DESC NULLS LAST, g.id DESC
            LIMIT 8;
            """))
        {
            command.Parameters.AddWithValue("id", studentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                grades.Add(new StudentPortalGrade(reader.GetString(0), reader.IsDBNull(1) ? "" : reader.GetString(1),
                    reader.GetDecimal(2), reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        return new StudentPortalSummary
        {
            StudentId = studentId, Name = name, Surname = surname, Email = email, Role = role,
            Groups = groups, Subjects = subjects, MonthlyAverages = monthlyAverages,
            UpcomingSessions = upcoming, RecentGrades = grades,
            AttendanceTotal = attendanceTotal, AttendancePresent = present, AttendanceAbsent = absent,
            AttendanceLate = late, AverageGrade = average
        };
    }
}
