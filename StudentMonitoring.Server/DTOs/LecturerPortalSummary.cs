namespace StudentMonitoring.Server.DTOs;

public sealed class LecturerPortalSummary
{
    public int LecturerId { get; init; }
    public string Name { get; init; } = "";
    public string Surname { get; init; } = "";
    public string Email { get; init; } = "";
    public int TotalCourses { get; init; }
    public int TotalGroups { get; init; }
    public int TotalStudents { get; init; }
    public int UpcomingSessionCount { get; init; }
    public int AttendanceTotal { get; init; }
    public int AttendancePresent { get; init; }
    public int AttendanceAbsent { get; init; }
    public int AttendanceLate { get; init; }
    public decimal? AttendanceRate { get; init; }
    public decimal? AverageGrade { get; init; }
    public List<LecturerPortalCourse> Courses { get; init; } = [];
    public List<LecturerPortalGroup> GroupAnalytics { get; init; } = [];
    public List<LecturerPortalSession> UpcomingSessions { get; init; } = [];
    public List<LecturerPortalGrade> RecentGrades { get; init; } = [];
}

public sealed record LecturerPortalCourse(int OfferingId, string SubjectName, string GroupName, string? CourseName,
    string AcademicYear, int Semester, int StudentCount, decimal? AverageGrade, int SessionCount);

public sealed record LecturerPortalGroup(int OfferingId, string SubjectName, string GroupName, int StudentCount,
    decimal? AverageGrade, int AttendanceTotal, int Absences, decimal? AbsenceRate);

public sealed record LecturerPortalSession(int Id, string SubjectName, string GroupName, string Date,
    string StartTime, string EndTime, string? Topic, string Type, int? Room);

public sealed record LecturerPortalGrade(int Id, string StudentName, string SubjectName, string GroupName,
    string Type, decimal Value, decimal? Weight, string? Comment);
