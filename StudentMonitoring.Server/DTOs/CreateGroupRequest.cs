using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class CreateGroupRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public int? CourseId { get; set; }

    [StringLength(20)]
    public string? AcademicYear { get; set; }

    [Range(0, 5000)]
    public int? StudentCount { get; set; }
}
