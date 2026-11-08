namespace StudentMonitoring.Server.DTOs;

public sealed class GroupResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? AcademicYear { get; set; }
    public int? StudentCount { get; set; }
}
