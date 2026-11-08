namespace StudentMonitoring.Server.Models;

public sealed class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PrerequisiteSubjectId { get; set; }
}
