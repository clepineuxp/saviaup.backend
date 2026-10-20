using SaviaUp.Backend.Core.Authentication;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Tenants;

public sealed class SelectTenantUseCase(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    IRefreshTokenRepository refreshTokenRepository,
    SessionIssuer sessionIssuer,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ISelectTenantUseCase
{
    public async Task<Result<TenantSessionResponse>> ExecuteAsync(
        Guid userId,
        Guid currentSessionId,
        Guid tenantId,
        CancellationToken cancellationToken,
        string? supportTicket = null)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive) return Result<TenantSessionResponse>.Failure(Errors.AccountDisabled);
        if (user.IsSupportUser)
        {
            var supportTenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
            var supportRole = await roleRepository.GetOwnerForTenantAsync(tenantId, cancellationToken);
            if (supportTenant is null || !supportTenant.IsActive || supportRole is null)
                return Result<TenantSessionResponse>.Failure(Errors.TenantAccessDenied);
            var supportMembership = new TenantMembership { TenantId = tenantId, RoleId = supportRole.Id, Tenant = supportTenant };
            return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                var now = dateTimeProvider.UtcNow;
                await refreshTokenRepository.RevokeSessionAsync(userId, currentSessionId, now, transactionToken);
                var issued = await sessionIssuer.IssueAsync(user, currentSessionId, supportMembership, transactionToken);
                await tenantRepository.AddSupportAccessAuditAsync(new SupportAccessAudit
                {
                    Id = Guid.NewGuid(),
                    SupportUserId = userId,
                    TenantId = tenantId,
                    SessionId = currentSessionId,
                    Ticket = string.IsNullOrWhiteSpace(supportTicket) ? null : supportTicket.Trim(),
                    AccessedAt = now
                }, transactionToken);
                await unitOfWork.SaveChangesAsync(transactionToken);
                return Result<TenantSessionResponse>.Success(new TenantSessionResponse(
                    new TenantDto(supportTenant.Id, supportTenant.Name, supportRole.Id, "Usuario de soporte", supportTenant.TimeZoneId),
                    SessionIssuer.ToTokenResponse(issued.Session)));
            }, cancellationToken);
        }
        var membership = await tenantRepository.GetMembershipAsync(userId, tenantId, cancellationToken);
        var role = membership is not null ? await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken) : null;

        if (membership is null || !membership.IsEnabledAt(dateTimeProvider.UtcNow) || !membership.Tenant.IsActive || role is null || !role.IsActive)
            return Result<TenantSessionResponse>.Failure(Errors.TenantAccessDenied);

            return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var now = dateTimeProvider.UtcNow;
            user.LastTenantId = tenantId;
            user.UpdatedAt = now;
            await refreshTokenRepository.RevokeSessionAsync(userId, currentSessionId, now, transactionToken);
            var issued = await sessionIssuer.IssueAsync(user, currentSessionId, membership, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            var tenant = new TenantDto(membership.TenantId, membership.Tenant.Name, membership.RoleId, role.Name, membership.Tenant.TimeZoneId);
            return Result<TenantSessionResponse>.Success(new TenantSessionResponse(tenant, SessionIssuer.ToTokenResponse(issued.Session)));
        }, cancellationToken);
    }
}
