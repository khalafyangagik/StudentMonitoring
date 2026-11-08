namespace StudentMonitoring.Server.Models;

public sealed class Grade
{
    public int Id { get; set; }
    public int? StudentId { get; set; }
    public int? SubjectOfferingId { get; set; }
    public int? SessionId { get; set; }
    public string? Type { get; set; }
    public decimal? Value { get; set; }
    public decimal? Weight { get; set; }
    public string? Comment { get; set; }
}
