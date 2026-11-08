namespace StudentMonitoring.Server.Models;

public sealed class Group
{
    public int Id { get; set; }
    public int? CourseId { get; set; }
    public string? Name { get; set; }
    public string? AcademicYear { get; set; }
    public int? StudentCount { get; set; }
}
