namespace StudentMonitoring.Server.Models;

public sealed class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // PostgreSQL department_type enum values: academic, administrative
    public string? Type { get; set; }
}
