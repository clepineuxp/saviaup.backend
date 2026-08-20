using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record RegisterRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(320)] string Email,
    [Required] string Password);

public sealed record RefreshTokenRequest([Required] string RefreshToken);
public sealed record LogoutRequest(string? RefreshToken);
public sealed record ForgotPasswordRequest([Required, EmailAddress] string Email, string? Language = null);
public sealed record ResetPasswordRequest(
    [Required] string Token,
    [Required] string NewPassword,
    [Required] string ConfirmPassword);

public sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PreferredLanguage,
    ActiveTenantDto? ActiveTenant = null,
    RoleDto? Role = null,
    IReadOnlyCollection<string>? Permissions = null);

public sealed record ActiveTenantDto(Guid Id, string Name);
public sealed record RoleDto(Guid Id, string Code, string Name);

public sealed record AuthSessionDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    bool RequiresTenantSelection,
    UserDto User,
    ActiveTenantDto? ActiveTenant);

public sealed record RegisterResponse(AuthSessionDto Session, string NextStep);

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record MessageResponse(string Message);
