using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Results;

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

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<Category>> GetForTenantAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludedCategoryId,
        CancellationToken cancellationToken);
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task<bool> IsInUseAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
    void Remove(Category category);
}

public interface IIngredientRepository
{
    Task<PageData<Ingredient>> GetPageAsync(
        Guid tenantId,
        IngredientQueryRequest request,
        CancellationToken cancellationToken);
    Task<PageData<InventoryItemDto>> GetInventoryPageAsync(
        Guid tenantId,
        InventoryQueryRequest request,
        CancellationToken cancellationToken);
    Task<Ingredient?> GetByIdAsync(Guid tenantId, Guid ingredientId, CancellationToken cancellationToken);
    Task<Ingredient?> GetForStockUpdateAsync(Guid tenantId, Guid ingredientId, CancellationToken cancellationToken);
    Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken);
    Task<bool> HasMovementsAsync(Guid tenantId, Guid ingredientId, CancellationToken cancellationToken);
    void Remove(Ingredient ingredient);
}

public interface IInventoryMovementRepository
{
    Task<PageData<InventoryMovement>> GetPageAsync(
        Guid tenantId,
        InventoryMovementQueryRequest request,
        CancellationToken cancellationToken);
    Task AddAsync(InventoryMovement movement, CancellationToken cancellationToken);
}

public interface IMeasurementUnitRepository
{
    Task<PageData<MeasurementUnit>> GetPageAsync(
        Guid tenantId,
        MeasurementUnitQueryRequest request,
        CancellationToken cancellationToken);
    Task<MeasurementUnit?> GetByIdAsync(Guid tenantId, Guid unitId, CancellationToken cancellationToken);
    Task<bool> CodeOrNameExistsAsync(
        Guid tenantId,
        string normalizedCode,
        string normalizedName,
        Guid? excludedUnitId,
        CancellationToken cancellationToken);
    Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken);
    Task AddDefaultsAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> IsInUseAsync(Guid tenantId, Guid unitId, CancellationToken cancellationToken);
    void Remove(MeasurementUnit unit);
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
