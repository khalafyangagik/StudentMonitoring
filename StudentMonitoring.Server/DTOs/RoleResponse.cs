namespace StudentMonitoring.Server.DTOs;

public sealed class RoleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
    public DateTime? CreatedAt { get; set; }
}
