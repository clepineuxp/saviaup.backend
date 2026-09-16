using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record OrganizationSettingsDto(
    Guid Id,
    string Name,
    string? ResponsibleName,
    string? Document,
    string? ContactName,
    string? Email,
    string? Address,
    string? Country,
    string? State,
    string? City,
    string? Phone,
    string? Website,
    bool HasLogo,
    long LogoVersion,
    bool CanEditDocument);

public sealed record UpdateOrganizationSettingsRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(160)] string? ResponsibleName,
    [MaxLength(80)] string? Document,
    [MaxLength(160)] string? ContactName,
    [EmailAddress, MaxLength(320)] string? Email,
    [MaxLength(500)] string? Address,
    [MaxLength(100)] string? Country,
    [MaxLength(120)] string? State,
    [MaxLength(120)] string? City,
    [MaxLength(50)] string? Phone,
    [Url, MaxLength(2048)] string? Website);

public sealed record UploadOrganizationLogoRequest(byte[] Content, string ContentType, string? FileName);
public sealed record OrganizationLogoDto(byte[] Content, string ContentType, string FileName);

public sealed record BusinessSettingsDto(
    bool UsesTables,
    bool DeliveryEnabled,
    bool RequiresOpenCashRegister,
    bool EnableCustomSales,
    bool ShowVoluntaryTip,
    string TipMessage,
    int SuggestedTipPercentage);

public sealed record UpdateBusinessSettingsRequest(
    bool UsesTables,
    bool DeliveryEnabled,
    bool RequiresOpenCashRegister,
    bool EnableCustomSales,
    bool ShowVoluntaryTip,
    [Required, MaxLength(200)] string TipMessage,
    [Range(0, 100)] int SuggestedTipPercentage);

public sealed record PaymentMethodDto(
    Guid Id,
    string Name,
    bool IsIncludedInCashOpening,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SavePaymentMethodRequest(
    [Required, MaxLength(120)] string Name,
    bool IsIncludedInCashOpening);

public sealed record SetPaymentMethodStatusRequest(bool IsActive);

public sealed record EnabledPermissionDto(Guid Id, string Code, string Name);
public sealed record EnabledModulePermissionsDto(Guid Id, string Code, string Name, IReadOnlyCollection<EnabledPermissionDto> Permissions);

public sealed record SettingsRoleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    IReadOnlyCollection<string> Permissions);

public sealed record SaveSettingsRoleRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description,
    IReadOnlyCollection<string> Permissions);

public sealed record SetSettingsRoleStatusRequest(bool IsActive);

public sealed record OrganizationUserDto(
    Guid Id,
    Guid? MembershipId,
    Guid? InvitationId,
    string Email,
    string? FirstName,
    string? LastName,
    Guid RoleId,
    string RoleName,
    string Status,
    DateTimeOffset? DisabledUntil,
    DateTimeOffset CreatedAt);

public sealed record InviteOrganizationUserRequest(
    [Required, EmailAddress, MaxLength(320)] string Email,
    Guid RoleId);

public sealed record UpdateOrganizationUserRequest(
    Guid RoleId,
    bool IsActive,
    DateTimeOffset? DisabledUntil);
