namespace StudentMonitoring.Server.DTOs;

public sealed class SessionResponse
{
    public int Id { get; set; }
    public int SubjectOfferingId { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? LecturerId { get; set; }
    public string? LecturerName { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public int Semester { get; set; }
    public int SessionNumber { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SessionDate { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public bool IsCompleted { get; set; }
    public int? Room { get; set; }
}
