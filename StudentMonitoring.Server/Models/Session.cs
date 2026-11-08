namespace StudentMonitoring.Server.Models;

public sealed class Session
{
    public int Id { get; set; }
    public int? SubjectOfferingId { get; set; }
    public int? SessionNumber { get; set; }
    public string? Type { get; set; }
    public DateOnly? SessionDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Topic { get; set; }
    public bool? IsCompleted { get; set; }
    public int? Room { get; set; }
}
