using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class CreateCourseRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    [Range(1, 20)]
    public int? DurationYears { get; set; }
}
