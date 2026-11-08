namespace StudentMonitoring.Server.Models;

public sealed class SubjectOffering
{
    public int Id { get; set; }
    public int? SubjectId { get; set; }
    public int? GroupId { get; set; }
    public int? LecturerId { get; set; }
    public string? AcademicYear { get; set; }
    public int? Semester { get; set; }
}
