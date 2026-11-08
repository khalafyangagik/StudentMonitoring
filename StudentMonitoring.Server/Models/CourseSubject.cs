namespace StudentMonitoring.Server.Models;

public sealed class CourseSubject
{
    public int SubjectId { get; set; }
    public int CourseId { get; set; }
    public int? Semester { get; set; }
    public int? Credits { get; set; }
}
