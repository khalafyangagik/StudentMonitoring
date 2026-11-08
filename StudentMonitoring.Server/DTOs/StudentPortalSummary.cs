namespace StudentMonitoring.Server.DTOs;

public sealed class StudentPortalSummary
{
    public int StudentId { get; init; }
    public string Name { get; init; } = "";
    public string Surname { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "student";
    public List<string> Groups { get; init; } = [];
    public List<StudentPortalSubject> Subjects { get; init; } = [];
    public List<StudentPortalMonthlyAverage> MonthlyAverages { get; init; } = [];
    public List<StudentPortalSession> UpcomingSessions { get; init; } = [];
    public List<StudentPortalGrade> RecentGrades { get; init; } = [];
    public int AttendanceTotal { get; init; }
    public int AttendancePresent { get; init; }
    public int AttendanceAbsent { get; init; }
    public int AttendanceLate { get; init; }
    public decimal? AverageGrade { get; init; }
}

public sealed class StudentPortalSubject
{
    public int OfferingId { get; init; }
    public string SubjectName { get; init; } = "";
    public string? CourseName { get; init; }
    public string AcademicYear { get; init; } = "";
    public int Semester { get; init; }
    public string GroupName { get; init; } = "";
    public string? LecturerName { get; init; }
    public decimal? AverageGrade { get; init; }
    public int GradeCount { get; init; }
    public int AttendanceTotal { get; init; }
    public int AttendancePresent { get; init; }
    public int AttendanceAbsent { get; init; }
    public int AttendanceLate { get; init; }
    public List<StudentPortalSubjectGrade> Grades { get; init; } = [];
}
public sealed record StudentPortalSession(int Id, string SubjectName, string? LecturerName, string Date,
    string StartTime, string EndTime, string? Topic, int? Room, string Type);
public sealed record StudentPortalGrade(string SubjectName, string Type, decimal Value, decimal? Weight, string? Comment, string? Date);
public sealed record StudentPortalSubjectGrade(string Type, decimal Value, decimal? Weight, string? Comment, string? Date);
public sealed record StudentPortalMonthlyAverage(string Month, decimal AverageGrade, int GradeCount);
