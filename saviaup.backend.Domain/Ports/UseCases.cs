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

public interface IPermissionService
{
    Task<bool> IsAllowedAsync(Guid tenantId, Guid roleId, string permissionCode, CancellationToken cancellationToken);
}
