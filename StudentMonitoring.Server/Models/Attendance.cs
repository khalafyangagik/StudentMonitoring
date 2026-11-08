namespace StudentMonitoring.Server.Models;

public sealed class Attendance
{
    public int Id { get; set; }
    public int? StudentId { get; set; }
    public int? SessionId { get; set; }
    public string? Status { get; set; }
    public string? Comment { get; set; }
    public DateTime? RecordedAt { get; set; }
}
