using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;

namespace SaviaUp.Backend.Domain.Ports;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TenantMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TenantDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken);
    Task AddMembershipAsync(TenantMembership membership, CancellationToken cancellationToken);
}

public interface IRoleRepository
{
    Task AddAsync(Role role, CancellationToken cancellationToken);
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken);
    Task AssignAllPermissionsAsync(Guid roleId, CancellationToken cancellationToken);
}

public interface IPermissionRepository
{
    Task<bool> RoleHasPermissionAsync(Guid tenantId, Guid roleId, string permissionCode, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetForRoleAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken);
}

public interface IModuleRepository
{
    Task<IReadOnlyCollection<AvailableModuleReference>> GetAvailableForRoleAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
    Task<bool> TryRevokeAsync(Guid tokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken);
    Task SetReplacementAsync(Guid tokenId, Guid replacementId, CancellationToken cancellationToken);
    Task RevokeSessionAsync(Guid userId, Guid sessionId, DateTimeOffset revokedAt, CancellationToken cancellationToken);
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken);
    Task<bool> HasActiveSessionAsync(Guid userId, Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);
    Task<bool> TryMarkUsedAsync(Guid tokenId, DateTimeOffset usedAt, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
