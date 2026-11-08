namespace StudentMonitoring.Server.DTOs;

public sealed class SessionOptionResponse
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int? GroupId { get; set; }
}
