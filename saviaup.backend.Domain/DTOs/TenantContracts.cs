using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record TenantDto(Guid Id, string Name, Guid RoleId, string RoleName);
public sealed record CreateTenantRequest([Required, MaxLength(120)] string Name);
public sealed record TenantSessionResponse(TenantDto Tenant, TokenResponse Tokens);
