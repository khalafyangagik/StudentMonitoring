using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class CreateDepartmentRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [RegularExpression("^(academic|administrative)$", ErrorMessage = "Type must be 'academic' or 'administrative'.")]
    public string? Type { get; set; }
}
