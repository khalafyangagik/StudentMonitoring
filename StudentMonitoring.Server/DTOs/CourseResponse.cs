namespace StudentMonitoring.Server.DTOs;

public sealed class CourseResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? DurationYears { get; set; }
}
