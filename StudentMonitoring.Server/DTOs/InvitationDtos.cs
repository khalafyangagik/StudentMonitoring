using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class CreateStaffInvitationRequest
{
    [Required, EmailAddress, StringLength(150)] public string Email { get; init; } = "";
    [Required, RegularExpression("^(lecturer|student)$")] public string Role { get; init; } = "";
    [Required, MinLength(1)] public int[] DepartmentIds { get; init; } = [];
}

public sealed class AcceptInvitationRequest
{
    [Required, StringLength(100)] public string Token { get; init; } = "";
    [Required, StringLength(100)] public string Name { get; init; } = "";
    [Required, StringLength(100)] public string Surname { get; init; } = "";
    [Required, MinLength(8), StringLength(128)] public string Password { get; init; } = "";
}

public sealed class SetStaffActiveRequest
{
    public bool IsActive { get; init; }
}

public sealed record InvitationPreview(string Email, string Role, DateTime ExpiresAt, IReadOnlyList<string> Departments);
public sealed record InvitationRow(int Id, string Email, string Role, DateTime ExpiresAt, string Departments);
public sealed record ManagedStaffRow(int Id, string Name, string Surname, string Email, string Role, bool IsActive, string Departments);
public sealed record ManagedDepartmentRow(int Id, string Name);
public sealed record AdminWorkspaceResponse(IReadOnlyList<ManagedDepartmentRow> Departments,
    IReadOnlyList<ManagedStaffRow> Staff, IReadOnlyList<InvitationRow> PendingInvitations);
