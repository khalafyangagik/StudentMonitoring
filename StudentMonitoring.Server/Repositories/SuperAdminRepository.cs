using Npgsql;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class SuperAdminRepository(NpgsqlDataSource dataSource) : ISuperAdminRepository
{
    public async Task<SuperAdminOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken)
    {
        int activeUsers, students, admins, lecturers, departments, groups, sessions, attendanceRecords;
        decimal? attendanceRate, averageGrade;
        await using (var command = dataSource.CreateCommand("""
            SELECT COUNT(*) FILTER (WHERE COALESCE(u.is_active, FALSE)),
                   COUNT(*) FILTER (WHERE lower(COALESCE(r.name,'')) IN ('student','ուսանող')),
                   COUNT(*) FILTER (WHERE lower(COALESCE(r.name,'')) IN ('admin','administrator','ադմինիստրատոր')),
                   COUNT(*) FILTER (WHERE lower(COALESCE(r.name,'')) IN ('lecturer','teacher','instructor','դասախոս')),
                   (SELECT COUNT(*) FROM department), (SELECT COUNT(*) FROM "group"),
                   (SELECT COUNT(*) FROM "session" WHERE session_date >= CURRENT_DATE AND COALESCE(is_completed,FALSE)=FALSE),
                   (SELECT COUNT(*) FROM attendance),
                   (SELECT ROUND(100.0 * COUNT(*) FILTER (WHERE status='present'::attendance_status) / NULLIF(COUNT(*),0), 1) FROM attendance),
                   (SELECT ROUND(SUM(value * COALESCE(weight,1)) / NULLIF(SUM(COALESCE(weight,1)),0), 2)
                    FROM grade WHERE value IS NOT NULL)
            FROM "user" u LEFT JOIN role r ON r.id=u.role_id;
            """))
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            activeUsers = Convert.ToInt32(reader.GetInt64(0)); students = Convert.ToInt32(reader.GetInt64(1));
            admins = Convert.ToInt32(reader.GetInt64(2)); lecturers = Convert.ToInt32(reader.GetInt64(3));
            departments = Convert.ToInt32(reader.GetInt64(4)); groups = Convert.ToInt32(reader.GetInt64(5));
            sessions = Convert.ToInt32(reader.GetInt64(6)); attendanceRecords = Convert.ToInt32(reader.GetInt64(7));
            attendanceRate = reader.IsDBNull(8) ? null : reader.GetDecimal(8);
            averageGrade = reader.IsDBNull(9) ? null : reader.GetDecimal(9);
        }

        var adminList = await ReadUsersAsync("""
            SELECT u.id, COALESCE(u.name,''), COALESCE(u.surname,''), COALESCE(u.email,''),
                   COALESCE(u.is_active,FALSE), string_agg(DISTINCT d.name, ', '), 0::bigint
            FROM "user" u JOIN role r ON r.id=u.role_id
            LEFT JOIN admin_department ad ON ad.admin_id=u.id
            LEFT JOIN department d ON d.id=ad.department_id
            WHERE lower(r.name) IN ('admin','administrator','ադմինիստրատոր')
            GROUP BY u.id ORDER BY u.surname,u.name,u.id;
            """, cancellationToken);
        var lecturerList = await ReadUsersAsync("""
            SELECT u.id, COALESCE(u.name,''), COALESCE(u.surname,''), COALESCE(u.email,''),
                   COALESCE(u.is_active,FALSE), NULL::text, COUNT(DISTINCT so.id)
            FROM "user" u JOIN role r ON r.id=u.role_id
            LEFT JOIN subject_offering so ON so.lecturer_id=u.id
            WHERE lower(r.name) IN ('lecturer','teacher','instructor','դասախոս')
            GROUP BY u.id ORDER BY u.surname,u.name,u.id;
            """, cancellationToken);

        var departmentList = new List<DepartmentOptionResponse>();
        await using (var command = dataSource.CreateCommand("SELECT id,name FROM department ORDER BY name,id;"))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken)) departmentList.Add(new DepartmentOptionResponse(reader.GetInt32(0), reader.GetString(1)));

        var groupAnalytics = new List<GroupAnalyticsRow>();
        await using (var command = dataSource.CreateCommand("""
            WITH group_students AS (
                SELECT group_id, COUNT(DISTINCT student_id)::int AS students
                FROM student_group WHERE status='active'::student_group_status GROUP BY group_id
            ), group_grades AS (
                SELECT so.group_id,
                       ROUND(SUM(g.value * COALESCE(g.weight,1)) / NULLIF(SUM(COALESCE(g.weight,1)),0), 2) AS average_grade,
                       COUNT(*)::int AS grade_count
                FROM grade g JOIN subject_offering so ON so.id=g.subject_offering_id
                JOIN student_group sg ON sg.student_id=g.student_id AND sg.group_id=so.group_id
                    AND sg.status='active'::student_group_status
                WHERE g.value IS NOT NULL GROUP BY so.group_id
            ), group_attendance AS (
                SELECT so.group_id,
                       COUNT(*) FILTER (WHERE a.status='absent'::attendance_status)::int AS absences,
                       COUNT(*)::int AS attendance_records,
                       ROUND(100.0 * COUNT(*) FILTER (WHERE a.status='absent'::attendance_status) / NULLIF(COUNT(*),0), 1) AS absence_rate
                FROM attendance a JOIN "session" s ON s.id=a.session_id
                JOIN subject_offering so ON so.id=s.subject_offering_id
                JOIN student_group sg ON sg.student_id=a.student_id AND sg.group_id=so.group_id
                    AND sg.status='active'::student_group_status
                GROUP BY so.group_id
            )
            SELECT g.id, g.name, COALESCE(c.name,''), COALESCE(gs.students,0), gg.average_grade,
                   COALESCE(gg.grade_count,0), COALESCE(ga.absences,0), COALESCE(ga.attendance_records,0), ga.absence_rate
            FROM "group" g LEFT JOIN course c ON c.id=g.course_id
            LEFT JOIN group_students gs ON gs.group_id=g.id
            LEFT JOIN group_grades gg ON gg.group_id=g.id
            LEFT JOIN group_attendance ga ON ga.group_id=g.id
            ORDER BY g.name,g.id;
            """))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                groupAnalytics.Add(new GroupAnalyticsRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                    reader.GetInt32(3), reader.IsDBNull(4) ? null : reader.GetDecimal(4), reader.GetInt32(5),
                    reader.GetInt32(6), reader.GetInt32(7), reader.IsDBNull(8) ? null : reader.GetDecimal(8)));

        var pendingAdminInvitations = new List<InvitationRow>();
        await using (var command = dataSource.CreateCommand("""
            SELECT i.id,i.email,r.name,i.expires_at,COALESCE(string_agg(d.name, ', ' ORDER BY d.name),'')
            FROM user_invitation i JOIN role r ON r.id=i.role_id
            LEFT JOIN invitation_department x ON x.invitation_id=i.id LEFT JOIN department d ON d.id=x.department_id
            WHERE lower(r.name)='admin' AND i.accepted_at IS NULL AND i.revoked_at IS NULL AND i.expires_at>(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
            GROUP BY i.id,r.name ORDER BY i.created_at DESC;
            """))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken)) pendingAdminInvitations.Add(new(reader.GetInt32(0),reader.GetString(1),reader.GetString(2),reader.GetDateTime(3),reader.GetString(4)));

        return new SuperAdminOverviewResponse
        {
            ActiveUsers = activeUsers, Students = students, Administrators = admins, Lecturers = lecturers,
            Departments = departments, Groups = groups, UpcomingSessions = sessions, AttendanceRecords = attendanceRecords,
            AttendanceRate = attendanceRate, AverageGrade = averageGrade,
            AdminList = adminList, LecturerList = lecturerList, DepartmentList = departmentList,
            GroupAnalytics = groupAnalytics,
            PendingAdminInvitations = pendingAdminInvitations
        };
    }

    private async Task<List<SuperAdminUserRow>> ReadUsersAsync(string sql, CancellationToken cancellationToken)
    {
        var rows = new List<SuperAdminUserRow>();
        await using var command = dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new SuperAdminUserRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetBoolean(4), reader.IsDBNull(5) ? null : reader.GetString(5), Convert.ToInt32(reader.GetInt64(6))));
        return rows;
    }

}
