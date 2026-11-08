using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class CreateSubjectRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? PrerequisiteSubjectId { get; set; }
}
