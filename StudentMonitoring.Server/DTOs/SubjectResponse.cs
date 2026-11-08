namespace StudentMonitoring.Server.DTOs;

public sealed class SubjectResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PrerequisiteSubjectId { get; set; }
    public string? PrerequisiteSubjectName { get; set; }
}
