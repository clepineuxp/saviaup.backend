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
    Task<Result<ProductDto>> ExecuteAsync(Guid tenantId, CreateProductRequest request, CancellationToken cancellationToken);
}

public interface IUpdateProductUseCase
{
    Task<Result<ProductDto>> ExecuteAsync(Guid tenantId, Guid productId, UpdateProductRequest request, CancellationToken cancellationToken);
}

public interface ISetProductStatusUseCase
{
    Task<Result<ProductDto>> ExecuteAsync(Guid tenantId, Guid productId, SetProductStatusRequest request, CancellationToken cancellationToken);
}

public interface IDeleteProductUseCase
{
    Task<Result> ExecuteAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken);
}

public interface IPermissionService
{
    Task<bool> IsAllowedAsync(Guid tenantId, Guid roleId, string permissionCode, CancellationToken cancellationToken);
}
