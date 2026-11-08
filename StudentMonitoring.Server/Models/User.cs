namespace StudentMonitoring.Server.Models;

public sealed class User
{
    public int Id { get; set; }
    public int? RoleId { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? Phone { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
