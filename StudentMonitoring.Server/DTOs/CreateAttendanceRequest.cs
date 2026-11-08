using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class CreateAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }

    [Required]
    [RegularExpression("^(present|absent|late)$")]
    public string Status { get; set; } = "present";

    [StringLength(1000)]
    public string? Comment { get; set; }
}
