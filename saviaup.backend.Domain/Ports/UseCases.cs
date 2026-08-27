using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Domain.Ports;

public interface ILoginUseCase
{
    Task<Result<AuthSessionDto>> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken);
}

public interface IRegisterUseCase
{
    Task<Result<RegisterResponse>> ExecuteAsync(RegisterRequest request, CancellationToken cancellationToken);
}

public interface IRefreshTokenUseCase
{
    Task<Result<TokenResponse>> ExecuteAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}

public interface IForgotPasswordUseCase
{
    Task<Result> ExecuteAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
}

public interface IResetPasswordUseCase
{
    Task<Result> ExecuteAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}

public interface ILogoutUseCase
{
    Task<Result> ExecuteAsync(LogoutRequest request, Guid userId, Guid sessionId, CancellationToken cancellationToken);
}

public interface IGetUserTenantsUseCase
{
    Task<Result<IReadOnlyCollection<TenantDto>>> ExecuteAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ICreateTenantUseCase
{
    Task<Result<TenantSessionResponse>> ExecuteAsync(Guid userId, Guid currentSessionId, CreateTenantRequest request, CancellationToken cancellationToken);
}

public interface ISelectTenantUseCase
{
    Task<Result<TenantSessionResponse>> ExecuteAsync(Guid userId, Guid currentSessionId, Guid tenantId, CancellationToken cancellationToken);
}

public interface IGetCurrentUserUseCase
{
    Task<Result<UserDto>> ExecuteAsync(Guid userId, Guid? tenantId, Guid? roleId, CancellationToken cancellationToken);
}

public interface IGetUserInfoUseCase
{
    Task<Result<UserInfoDto>> ExecuteAsync(Guid userId, Guid tenantId, Guid roleId, CancellationToken cancellationToken);
}

public interface IGetAvailableModulesUseCase
{
    Task<Result<AvailableModulesResponse>> ExecuteAsync(
        Guid userId,
        Guid tenantId,
        Guid roleId,
        string? language,
        CancellationToken cancellationToken);
}

public interface IListCategoriesUseCase
{
    Task<Result<IReadOnlyCollection<CategoryDto>>> ExecuteAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken);
}

public interface ICreateCategoryUseCase
{
    Task<Result<CategoryDto>> ExecuteAsync(
        Guid tenantId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken);
}

public interface IUpdateCategoryUseCase
{
    Task<Result<CategoryDto>> ExecuteAsync(
        Guid tenantId,
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);
}

public interface ISetCategoryStatusUseCase
{
    Task<Result<CategoryDto>> ExecuteAsync(
        Guid tenantId,
        Guid categoryId,
        SetCategoryStatusRequest request,
        CancellationToken cancellationToken);
}

public interface IDeleteCategoryUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken);
}

public interface IGetSuppliersUseCase
{
    Task<Result<SupplierPageDto>> ExecuteAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface IGetSupplierLookupUseCase
{
    Task<Result<IReadOnlyCollection<SupplierLookupDto>>> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface ICreateSupplierUseCase
{
    Task<Result<SupplierDto>> ExecuteAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        CreateSupplierRequest request,
        CancellationToken cancellationToken);
}

public interface IUpdateSupplierUseCase
{
    Task<Result<SupplierDto>> ExecuteAsync(
        Guid tenantId,
        Guid supplierId,
        Guid userId,
        string userName,
        UpdateSupplierRequest request,
        CancellationToken cancellationToken);
}

public interface ISetSupplierStatusUseCase
{
    Task<Result<SupplierDto>> ExecuteAsync(
        Guid tenantId,
        Guid supplierId,
        Guid userId,
        string userName,
        SetSupplierStatusRequest request,
        CancellationToken cancellationToken);
}

public interface IGetExpensesUseCase
{
    Task<Result<ExpensePageDto>> ExecuteAsync(
        Guid tenantId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? search,
        Guid? supplierId,
        string? status,
        string? paymentMethod,
        bool? isCashOut,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface ICreateExpenseUseCase
{
    Task<Result<ExpenseDto>> ExecuteAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        CreateExpenseRequest request,
        CancellationToken cancellationToken);
}

public interface IUpdateExpenseUseCase
{
    Task<Result<ExpenseDto>> ExecuteAsync(
        Guid tenantId,
        Guid expenseId,
        Guid userId,
        string userName,
        UpdateExpenseRequest request,
        CancellationToken cancellationToken);
}

public interface IAnnulExpenseUseCase
{
    Task<Result<ExpenseDto>> ExecuteAsync(
        Guid tenantId,
        Guid expenseId,
        Guid userId,
        string userName,
        AnnulExpenseRequest request,
        CancellationToken cancellationToken);
}

public interface IListInventoryUseCase
{
    Task<Result<PagedResponse<InventoryItemDto>>> ExecuteAsync(Guid tenantId, InventoryQueryRequest request, CancellationToken cancellationToken);
}

public interface IListIngredientsUseCase
{
    Task<Result<PagedResponse<IngredientDto>>> ExecuteAsync(Guid tenantId, IngredientQueryRequest request, CancellationToken cancellationToken);
}

public interface ICreateIngredientUseCase
{
    Task<Result<IngredientDto>> ExecuteAsync(Guid tenantId, Guid userId, CreateIngredientRequest request, CancellationToken cancellationToken);
}

public interface IUpdateIngredientUseCase
{
    Task<Result<IngredientDto>> ExecuteAsync(Guid tenantId, Guid ingredientId, UpdateIngredientRequest request, CancellationToken cancellationToken);
}

public interface ISetIngredientStatusUseCase
{
    Task<Result<IngredientDto>> ExecuteAsync(Guid tenantId, Guid ingredientId, SetIngredientStatusRequest request, CancellationToken cancellationToken);
}

public interface IDeleteIngredientUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid ingredientId, CancellationToken cancellationToken);
}

public interface IListInventoryMovementsUseCase
{
    Task<Result<PagedResponse<InventoryMovementDto>>> ExecuteAsync(Guid tenantId, InventoryMovementQueryRequest request, CancellationToken cancellationToken);
}

public interface ICreateInventoryMovementUseCase
{
    Task<Result<InventoryMovementDto>> ExecuteAsync(Guid tenantId, Guid userId, CreateInventoryMovementRequest request, CancellationToken cancellationToken);
}

public interface IListMeasurementUnitsUseCase
{
    Task<Result<PagedResponse<MeasurementUnitDto>>> ExecuteAsync(Guid tenantId, MeasurementUnitQueryRequest request, CancellationToken cancellationToken);
}

public interface ICreateMeasurementUnitUseCase
{
    Task<Result<MeasurementUnitDto>> ExecuteAsync(Guid tenantId, CreateMeasurementUnitRequest request, CancellationToken cancellationToken);
}

public interface IUpdateMeasurementUnitUseCase
{
    Task<Result<MeasurementUnitDto>> ExecuteAsync(Guid tenantId, Guid unitId, UpdateMeasurementUnitRequest request, CancellationToken cancellationToken);
}

public interface ISetMeasurementUnitStatusUseCase
{
    Task<Result<MeasurementUnitDto>> ExecuteAsync(Guid tenantId, Guid unitId, SetMeasurementUnitStatusRequest request, CancellationToken cancellationToken);
}

public interface IDeleteMeasurementUnitUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid unitId, CancellationToken cancellationToken);
}

public interface IListProductsUseCase
{
    Task<Result<PagedResponse<ProductDto>>> ExecuteAsync(Guid tenantId, ProductQueryRequest request, CancellationToken cancellationToken);
}

public interface ICreateProductUseCase
{
    Task<Result<ProductDto>> ExecuteAsync(Guid tenantId, Guid userId, string userName, CreateProductRequest request, CancellationToken cancellationToken);
}

public interface IUpdateProductUseCase
{
    Task<Result<ProductDto>> ExecuteAsync(Guid tenantId, Guid productId, Guid userId, string userName, UpdateProductRequest request, CancellationToken cancellationToken);
}

public interface ISetProductStatusUseCase
{
    Task<Result<ProductDto>> ExecuteAsync(Guid tenantId, Guid productId, SetProductStatusRequest request, CancellationToken cancellationToken);
}

public interface IDeleteProductUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken);
}

public interface IListDiningAreasUseCase
{
    Task<Result<IReadOnlyCollection<DiningAreaDto>>> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface ICreateDiningAreaUseCase
{
    Task<Result<DiningAreaDto>> ExecuteAsync(Guid tenantId, CreateDiningAreaRequest request, CancellationToken cancellationToken);
}

public interface IUpdateDiningAreaUseCase
{
    Task<Result<DiningAreaDto>> ExecuteAsync(Guid tenantId, Guid areaId, UpdateDiningAreaRequest request, CancellationToken cancellationToken);
}

public interface IReorderDiningAreasUseCase
{
    Task<Result<IReadOnlyCollection<DiningAreaDto>>> ExecuteAsync(Guid tenantId, ReorderDiningAreasRequest request, CancellationToken cancellationToken);
}

public interface IDeleteDiningAreaUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid areaId, CancellationToken cancellationToken);
}

public interface IListRestaurantTablesUseCase
{
    Task<Result<IReadOnlyCollection<RestaurantTableDto>>> ExecuteAsync(Guid tenantId, Guid? areaId, CancellationToken cancellationToken);
}

public interface ICreateRestaurantTableUseCase
{
    Task<Result<RestaurantTableDto>> ExecuteAsync(Guid tenantId, CreateRestaurantTableRequest request, CancellationToken cancellationToken);
}

public interface IUpdateRestaurantTableUseCase
{
    Task<Result<RestaurantTableDto>> ExecuteAsync(Guid tenantId, Guid tableId, UpdateRestaurantTableRequest request, CancellationToken cancellationToken);
}

public interface IDeleteRestaurantTableUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken);
}

public interface IGetTableOperationUseCase
{
    Task<Result<TableOperationSnapshotDto>> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface ISetTableOperationUseCase
{
    Task<Result<RestaurantTableDto>> ExecuteAsync(Guid tenantId, Guid tableId, SetTableOperationRequest request, CancellationToken cancellationToken);
}

public interface IUpdateTableOrderUseCase
{
    Task<Result<RestaurantTableDto>> ExecuteAsync(Guid tenantId, Guid tableId, UpdateTableOrderRequest request, CancellationToken cancellationToken);
}

public interface IGetOrdersPageUseCase
{
    Task<Result<PagedResponse<OrderDto>>> ExecuteAsync(Guid tenantId, OrderQueryRequest request, CancellationToken cancellationToken);
}

public interface IGetOrderItemsPageUseCase
{
    Task<Result<PagedResponse<OrderItemReportDto>>> ExecuteAsync(Guid tenantId, OrderQueryRequest request, CancellationToken cancellationToken);
}

public interface IGetActiveTableOrderUseCase
{
    Task<Result<OrderDto>> ExecuteAsync(Guid tenantId, Guid tableId, CancellationToken cancellationToken);
}

public interface IAddTableOrderItemsUseCase
{
    Task<Result<OrderDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        Guid userId,
        string userName,
        AddOrderItemsRequest request,
        CancellationToken cancellationToken);
}

public interface IMoveTableOrderUseCase
{
    Task<Result<RestaurantTableDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        MoveTableOrderRequest request,
        CancellationToken cancellationToken);
}

public interface ICancelOrderItemUseCase
{
    Task<Result<OrderDto>> ExecuteAsync(
        Guid tenantId,
        Guid itemId,
        Guid userId,
        string userName,
        CancelOrderItemRequest request,
        CancellationToken cancellationToken);
}

public interface IPayAndCloseTableOrderUseCase
{
    Task<Result<OrderDto>> ExecuteAsync(
        Guid tenantId,
        Guid tableId,
        Guid userId,
        string userName,
        CheckoutOrderRequest request,
        CancellationToken cancellationToken);
}

public interface IGenerateSummaryReceiptUseCase
{
    Task<Result<OrderReceiptDto>> ExecuteAsync(
        Guid tenantId,
        Guid orderId,
        Guid userId,
        string userName,
        CancellationToken cancellationToken);
}

public interface IGetOrderReceiptsUseCase
{
    Task<Result<IReadOnlyCollection<OrderReceiptDto>>> ExecuteAsync(
        Guid tenantId,
        Guid orderId,
        CancellationToken cancellationToken);
}

public interface IListCashRegistersUseCase
{
    Task<Result<IReadOnlyCollection<CashRegisterDto>>> ExecuteAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken);
}

public interface ICreateCashRegisterUseCase
{
    Task<Result<CashRegisterDto>> ExecuteAsync(
        Guid tenantId,
        CreateCashRegisterRequest request,
        CancellationToken cancellationToken);
}

public interface IUpdateCashRegisterUseCase
{
    Task<Result<CashRegisterDto>> ExecuteAsync(
        Guid tenantId,
        Guid cashRegisterId,
        UpdateCashRegisterRequest request,
        CancellationToken cancellationToken);
}

public interface ISetCashRegisterStatusUseCase
{
    Task<Result<CashRegisterDto>> ExecuteAsync(
        Guid tenantId,
        Guid cashRegisterId,
        SetCashRegisterStatusRequest request,
        CancellationToken cancellationToken);
}

public interface IDeleteCashRegisterUseCase
{
    Task<Result> ExecuteAsync(
        Guid tenantId,
        Guid cashRegisterId,
        CancellationToken cancellationToken);
}

public interface IOpenCashRegisterShiftUseCase
{
    Task<Result<CashRegisterShiftDto>> ExecuteAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        OpenCashRegisterShiftRequest request,
        CancellationToken cancellationToken);
}

public interface ICloseCashRegisterShiftUseCase
{
    Task<Result<CashRegisterShiftDto>> ExecuteAsync(
        Guid tenantId,
        Guid shiftId,
        Guid userId,
        string userName,
        CloseCashRegisterShiftRequest request,
        CancellationToken cancellationToken);
}

public interface IGetCashRegisterShiftSummaryUseCase
{
    Task<Result<CashRegisterShiftSummaryDto>> ExecuteAsync(
        Guid tenantId,
        Guid shiftId,
        CancellationToken cancellationToken);
}

public interface IListCashRegisterShiftsUseCase
{
    Task<Result<PagedResponse<CashRegisterShiftDto>>> ExecuteAsync(
        Guid tenantId,
        CashRegisterShiftQueryRequest request,
        CancellationToken cancellationToken);
}

public interface IPermissionService
{
    Task<bool> IsAllowedAsync(Guid tenantId, Guid roleId, string permissionCode, CancellationToken cancellationToken);
}

public interface IOrganizationSettingsUseCase
{
    Task<Result<OrganizationSettingsDto>> GetAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken);
    Task<Result<OrganizationSettingsDto>> UpdateAsync(Guid tenantId, Guid roleId, UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken);
    Task<Result<OrganizationLogoDto>> GetLogoAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Result> UploadLogoAsync(Guid tenantId, UploadOrganizationLogoRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteLogoAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface IBusinessSettingsUseCase
{
    Task<Result<BusinessSettingsDto>> GetAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Result<BusinessSettingsDto>> UpdateAsync(Guid tenantId, UpdateBusinessSettingsRequest request, CancellationToken cancellationToken);
}

public interface IPaymentMethodsSettingsUseCase
{
    Task<Result<IReadOnlyCollection<PaymentMethodDto>>> ListAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken);
    Task<Result<PaymentMethodDto>> CreateAsync(Guid tenantId, SavePaymentMethodRequest request, CancellationToken cancellationToken);
    Task<Result<PaymentMethodDto>> UpdateAsync(Guid tenantId, Guid paymentMethodId, SavePaymentMethodRequest request, CancellationToken cancellationToken);
    Task<Result<PaymentMethodDto>> SetStatusAsync(Guid tenantId, Guid paymentMethodId, SetPaymentMethodStatusRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Guid tenantId, Guid paymentMethodId, CancellationToken cancellationToken);
}

public interface IAccessSettingsUseCase
{
    Task<Result<IReadOnlyCollection<EnabledModulePermissionsDto>>> GetPermissionsAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<SettingsRoleDto>>> ListRolesAsync(Guid tenantId, bool includeInactive, CancellationToken cancellationToken);
    Task<Result<SettingsRoleDto>> CreateRoleAsync(Guid tenantId, SaveSettingsRoleRequest request, CancellationToken cancellationToken);
    Task<Result<SettingsRoleDto>> UpdateRoleAsync(Guid tenantId, Guid roleId, SaveSettingsRoleRequest request, CancellationToken cancellationToken);
    Task<Result<SettingsRoleDto>> SetRoleStatusAsync(Guid tenantId, Guid roleId, SetSettingsRoleStatusRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteRoleAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<OrganizationUserDto>>> ListUsersAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Result<OrganizationUserDto>> InviteUserAsync(Guid tenantId, Guid invitedByUserId, InviteOrganizationUserRequest request, CancellationToken cancellationToken);
    Task<Result<OrganizationUserDto>> UpdateUserAsync(Guid tenantId, Guid membershipId, UpdateOrganizationUserRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteUserAsync(Guid tenantId, Guid currentUserId, Guid entryId, CancellationToken cancellationToken);
}

public interface IGetStatisticsUseCase
{
    Task<Result<StatisticsDashboardDto>> ExecuteAsync(
        Guid tenantId,
        string? period,
        bool? includeTips,
        CancellationToken cancellationToken);
}

public interface IGetBillingReceiptsUseCase
{
    Task<Result<PagedResponse<BillingReceiptItemDto>>> ExecuteAsync(
        Guid tenantId,
        BillingReceiptQueryRequest request,
        CancellationToken cancellationToken);
}

public interface IGetBillingOrdersUseCase
{
    Task<Result<PagedResponse<BillingOrderDto>>> ExecuteAsync(
        Guid tenantId,
        BillingReceiptQueryRequest request,
        CancellationToken cancellationToken);
}

public interface IGetBillingReceiptByIdUseCase
{
    Task<Result<OrderReceiptDto>> ExecuteAsync(
        Guid tenantId,
        Guid receiptId,
        CancellationToken cancellationToken);
}

public interface IUploadImageUseCase
{
    Task<Result<StoredImageDto>> ExecuteAsync(Guid tenantId, UploadImageRequest request, CancellationToken cancellationToken);
}

public interface IGetImageByIdUseCase
{
    Task<Result<StoredImageDto>> ExecuteAsync(Guid tenantId, Guid imageId, CancellationToken cancellationToken);
}

public interface IGetImagesByEntityUseCase
{
    Task<Result<IReadOnlyCollection<StoredImageSummaryDto>>> ExecuteAsync(Guid tenantId, string module, string entityId, CancellationToken cancellationToken);
}

public interface IDeleteImageUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid imageId, CancellationToken cancellationToken);
}
