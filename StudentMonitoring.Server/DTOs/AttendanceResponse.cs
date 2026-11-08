namespace StudentMonitoring.Server.DTOs;

public sealed class AttendanceResponse
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int SessionId { get; set; }
    public string? SessionLabel { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime? RecordedAt { get; set; }
}
