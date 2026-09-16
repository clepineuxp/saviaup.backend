using SaviaUp.Backend.Core.Authentication;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Tenants;

public sealed class CreateTenantUseCase(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    IMeasurementUnitRepository measurementUnitRepository,
    ISettingsRepository settingsRepository,
    IRefreshTokenRepository refreshTokenRepository,
    SessionIssuer sessionIssuer,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ICreateTenantUseCase
{
    public async Task<Result<TenantSessionResponse>> ExecuteAsync(
        Guid userId,
        Guid currentSessionId,
        CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<TenantSessionResponse>.Failure(Errors.Validation);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive) return Result<TenantSessionResponse>.Failure(Errors.AccountDisabled);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var now = dateTimeProvider.UtcNow;
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            var role = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Code = "TENANT_OWNER",
                Name = "Owner",
                Description = "Tenant owner with all enabled permissions.",
                IsSystem = true,
                IsActive = true,
                CreatedAt = now
            };
            var membership = new TenantMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                TenantId = tenant.Id,
                Tenant = tenant,
                RoleId = role.Id,
                IsActive = true,
                CreatedAt = now
            };
            await tenantRepository.AddAsync(tenant, transactionToken);
            await roleRepository.AddAsync(role, transactionToken);
            await tenantRepository.AddMembershipAsync(membership, transactionToken);
            await roleRepository.AssignAllPermissionsAsync(role.Id, transactionToken);
            await settingsRepository.EnableAllPermissionsAsync(tenant.Id, transactionToken);
            await settingsRepository.AddParametersAsync(SettingsDefaults.CreateBusinessParameters(tenant.Id, now), transactionToken);
            foreach (var paymentMethod in SettingsDefaults.CreatePaymentMethods(tenant.Id, now))
                await settingsRepository.AddPaymentMethodAsync(paymentMethod, transactionToken);
            await measurementUnitRepository.AddDefaultsAsync(tenant.Id, now, transactionToken);
            user.LastTenantId = tenant.Id;
            user.UpdatedAt = now;
            await refreshTokenRepository.RevokeSessionAsync(user.Id, currentSessionId, now, transactionToken);
            var issued = await sessionIssuer.IssueAsync(user, currentSessionId, membership, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            var dto = new TenantDto(tenant.Id, tenant.Name, role.Id, role.Name);
            return Result<TenantSessionResponse>.Success(new TenantSessionResponse(dto, SessionIssuer.ToTokenResponse(issued.Session)));
        }, cancellationToken);
    }
}
