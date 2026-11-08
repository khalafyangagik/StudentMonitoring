namespace StudentMonitoring.Server.Models;

public sealed class StudentGroup
{
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public DateTime? JoinedAt { get; set; }
    public string? Status { get; set; }
}
