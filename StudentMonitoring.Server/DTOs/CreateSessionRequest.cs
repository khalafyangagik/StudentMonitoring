using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class CreateSessionRequest
{
    [Range(1, int.MaxValue)]
    public int SubjectId { get; set; }

    [Range(1, int.MaxValue)]
    public int GroupId { get; set; }

    [Required, StringLength(20, MinimumLength = 4)]
    public string AcademicYear { get; set; } = string.Empty;

    [Range(1, 2)]
    public int Semester { get; set; }

    public int? LecturerId { get; set; }

    [Required]
    [RegularExpression("^(lecture|practice|lab)$")]
    public string Type { get; set; } = "lecture";

    public DateOnly SessionDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    [StringLength(255)]
    public string? Topic { get; set; }

    [Range(1, 9999)]
    public int? Room { get; set; }

    public bool IsCompleted { get; set; }
}
