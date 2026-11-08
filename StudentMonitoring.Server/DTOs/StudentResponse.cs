namespace StudentMonitoring.Server.DTOs;

public sealed class StudentResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
}
