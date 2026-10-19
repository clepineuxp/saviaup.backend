using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record OrganizationAccessLinkDto(
    string Code,
    string Kind,
    string? OrganizationName,
    string? RoleName,
    string? RequesterName,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    bool IsAccepted,
    IReadOnlyCollection<TenantDto>? AvailableOrganizations = null);

public sealed record CreatedOrganizationAccessLinkDto(
    string Code,
    string Url,
    string Kind,
    string? OrganizationName,
    string? RoleName,
    DateTimeOffset ExpiresAt);

public sealed record OrganizationAccessLinkSummaryDto(
    Guid Id,
    string Kind,
    string? OrganizationName,
    string? RoleName,
    DateTimeOffset ExpiresAt,
    string? Url);

public sealed record CreateJoinRequestResponse(CreatedOrganizationAccessLinkDto Link);

public sealed record CreateOrganizationInvitationLinkRequest(Guid RoleId);

public sealed record AcceptOrganizationAccessLinkRequest(Guid? TenantId = null, Guid? RoleId = null);

public sealed record AcceptOrganizationAccessLinkResponse(
    string Kind,
    Guid TenantId,
    string OrganizationName,
    string RoleName,
    bool MembershipCreated);
