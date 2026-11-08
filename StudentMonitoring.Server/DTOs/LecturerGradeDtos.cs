using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class CreateLecturerGradeRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; init; }

    [Range(1, int.MaxValue)]
    public int OfferingId { get; init; }

    [Range(1, int.MaxValue)]
    public int SessionId { get; init; }

    [Required]
    [RegularExpression("^(exam|quiz|homework|project)$")]
    public string Type { get; init; } = "quiz";

    [Range(typeof(decimal), "0", "20")]
    public decimal Value { get; init; }

    [Range(typeof(decimal), "0.01", "100")]
    public decimal Weight { get; init; } = 1;

    [StringLength(1000)]
    public string? Comment { get; init; }
}

public sealed record LecturerStudentOption(int Id, string Name, string Email);
public sealed record LecturerSessionOption(int Id, string Date, string Label);
public sealed record LecturerGradeCreated(int Id, int StudentId, int OfferingId, int SessionId, string Type,
    decimal Value, decimal Weight, string? Comment);
