namespace StudentMonitoring.Server.Models;

public sealed class Course
{
    public int Id { get; set; }
    public int? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? DurationYears { get; set; }
}
