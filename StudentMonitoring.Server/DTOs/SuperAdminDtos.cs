using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class SuperAdminOverviewResponse
{
    public int ActiveUsers { get; init; }
    public int Students { get; init; }
    public int Administrators { get; init; }
    public int Lecturers { get; init; }
    public int Departments { get; init; }
    public int Groups { get; init; }
    public int UpcomingSessions { get; init; }
    public int AttendanceRecords { get; init; }
    public decimal? AttendanceRate { get; init; }
    public decimal? AverageGrade { get; init; }
    public List<SuperAdminUserRow> AdminList { get; init; } = [];
    public List<SuperAdminUserRow> LecturerList { get; init; } = [];
    public List<DepartmentOptionResponse> DepartmentList { get; init; } = [];
    public List<GroupAnalyticsRow> GroupAnalytics { get; init; } = [];
    public List<InvitationRow> PendingAdminInvitations { get; init; } = [];
}

public sealed record SuperAdminUserRow(int Id, string Name, string Surname, string Email, bool IsActive, string? Departments, int TeachingAssignments);
public sealed record DepartmentOptionResponse(int Id, string Name);
public sealed record GroupAnalyticsRow(int Id, string Name, string CourseName, int Students, decimal? AverageGrade, int GradeCount, int Absences, int AttendanceRecords, decimal? AbsenceRate);

public sealed class AddAdministratorRequest
{
    [Required, EmailAddress, StringLength(150)] public string Email { get; init; } = "";
    [Required, MinLength(1)] public int[] DepartmentIds { get; init; } = [];
}
