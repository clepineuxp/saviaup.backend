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

public interface IProductRepository
{
    Task<PageData<Product>> GetPageAsync(
        Guid tenantId,
        ProductQueryRequest request,
        ProductType? type,
        CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken);
    Task DisableInventoryTrackingByCategoryAsync(
        Guid tenantId,
        Guid categoryId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    void Remove(Product product);
}

public interface IDiningAreaRepository
{
    Task<IReadOnlyCollection<DiningArea>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DiningArea>> GetForUpdateAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<DiningArea?> GetByIdAsync(Guid tenantId, Guid areaId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(Guid tenantId, string normalizedName, Guid? excludedAreaId, CancellationToken cancellationToken);
    Task<bool> OrderExistsAsync(Guid tenantId, int order, Guid? excludedAreaId, CancellationToken cancellationToken);
    Task<int> GetNextOrderAsync(Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(DiningArea area, CancellationToken cancellationToken);
    Task<bool> IsInUseAsync(Guid tenantId, Guid areaId, CancellationToken cancellationToken);
    void Remove(DiningArea area);
}

public interface IRestaurantTableRepository
{
    Task<IReadOnlyCollection<RestaurantTable>> GetForTenantAsync(Guid tenantId, Guid? areaId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DiningArea>> GetOperationAreasAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<RestaurantTable?> GetByIdAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(Guid tenantId, Guid areaId, string normalizedName, Guid? excludedTableId, CancellationToken cancellationToken);
    Task AddAsync(RestaurantTable table, CancellationToken cancellationToken);
    void Remove(RestaurantTable table);
}

public interface IOrderRepository
{
    Task<PageData<OrderDto>> GetOrdersPageAsync(Guid tenantId, OrderQueryRequest request, CancellationToken cancellationToken);
    Task<PageData<OrderItemReportDto>> GetOrderItemsPageAsync(Guid tenantId, OrderQueryRequest request, CancellationToken cancellationToken);
    Task<Order?> GetActiveByTableIdAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid tenantId, Guid orderId, CancellationToken cancellationToken);
    Task<OrderItem?> GetItemByIdAsync(Guid tenantId, Guid itemId, CancellationToken cancellationToken);
    Task<int> GetNextOrderNumberAsync(Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task AddItemAsync(OrderItem item, CancellationToken cancellationToken);
    void RemoveItem(OrderItem item);
}

public interface ICashRegisterRepository
{
    Task<IReadOnlyCollection<CashRegister>> GetForTenantAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken);
    Task<CashRegister?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(Guid tenantId, string normalizedName, Guid? excludedId, CancellationToken cancellationToken);
    Task<bool> HasOtherActiveAsync(Guid tenantId, Guid? excludedId, CancellationToken cancellationToken);
    Task AddAsync(CashRegister cashRegister, CancellationToken cancellationToken);
    Task<bool> IsInUseAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    void Remove(CashRegister cashRegister);
}

public interface ICashRegisterShiftRepository
{
    Task<bool> HasOpenShiftAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<CashRegisterShift?> GetOpenShiftAsync(Guid tenantId, Guid? cashRegisterId, CancellationToken cancellationToken);
    Task<CashRegisterShift?> GetByIdAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken);
    Task<PageData<CashRegisterShiftDto>> GetShiftsPageAsync(Guid tenantId, CashRegisterShiftQueryRequest request, CancellationToken cancellationToken);
    Task<bool> HasOccupiedTablesOrPendingOrdersAsync(Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(CashRegisterShift shift, CancellationToken cancellationToken);
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

public interface ISettingsRepository
{
    Task<Tenant?> GetTenantForUpdateAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OrganizationParameter>> GetParametersAsync(Guid tenantId, CancellationToken cancellationToken);
    Task AddParametersAsync(IEnumerable<OrganizationParameter> parameters, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaymentMethod>> GetPaymentMethodsAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken);
    Task<PaymentMethod?> GetPaymentMethodAsync(Guid tenantId, Guid paymentMethodId, CancellationToken cancellationToken);
    Task<bool> PaymentMethodNameExistsAsync(Guid tenantId, string normalizedName, Guid? excludedId, CancellationToken cancellationToken);
    Task AddPaymentMethodAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken);
    void RemovePaymentMethod(PaymentMethod paymentMethod);
    Task EnableAllPermissionsAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EnabledModulePermissionsDto>> GetEnabledPermissionCatalogAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetEnabledPermissionCodesAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Role>> GetRolesAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken);
    Task<Role?> GetRoleForUpdateAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken);
    Task<bool> RoleCodeOrNameExistsAsync(Guid tenantId, string code, string name, Guid? excludedId, CancellationToken cancellationToken);
    Task AddRoleAsync(Role role, CancellationToken cancellationToken);
    Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken);
    Task<bool> RoleIsInUseAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken);
    void RemoveRole(Role role);
    Task<IReadOnlyCollection<TenantMembership>> GetMembershipsAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TenantMembership?> GetMembershipForUpdateAsync(Guid tenantId, Guid membershipId, CancellationToken cancellationToken);
    Task<bool> HasAnotherActiveOwnerAsync(Guid tenantId, Guid excludedMembershipId, DateTimeOffset now, CancellationToken cancellationToken);
    void RemoveMembership(TenantMembership membership);
    Task<IReadOnlyCollection<TenantInvitation>> GetInvitationsAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TenantInvitation?> GetInvitationAsync(Guid tenantId, Guid invitationId, CancellationToken cancellationToken);
    Task<TenantInvitation?> GetInvitationByEmailAsync(Guid tenantId, string normalizedEmail, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TenantInvitation>> GetPendingInvitationsAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task AddInvitationAsync(TenantInvitation invitation, CancellationToken cancellationToken);
    void RemoveInvitation(TenantInvitation invitation);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
