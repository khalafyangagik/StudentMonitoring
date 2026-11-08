using System.ComponentModel.DataAnnotations;

namespace StudentMonitoring.Server.DTOs;

public sealed class RegisterRequest
{
    [Required, StringLength(100)] public string Name { get; init; } = "";
    [Required, StringLength(100)] public string Surname { get; init; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; init; } = "";
    [Required, MinLength(8), StringLength(128)] public string Password { get; init; } = "";
}

public sealed class VerifyEmailRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; init; } = "";
}

public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required] public string Password { get; init; } = "";
}

public sealed class VerifyLoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required, RegularExpression("^[0-9]{6}$")] public string Code { get; init; } = "";
}

public sealed record AuthUser(int Id, string Name, string Surname, string Email, string Role);
public sealed class AuthPasswordSubject { }
