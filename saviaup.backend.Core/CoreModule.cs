using Microsoft.Extensions.DependencyInjection;
using SaviaUp.Backend.Core.Authentication;
using SaviaUp.Backend.Core.Navigation;
using SaviaUp.Backend.Core.Security;
using SaviaUp.Backend.Core.Tenants;
using SaviaUp.Backend.Core.Users;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Core;

public static class CoreModule
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<IRegisterUseCase, RegisterUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
        services.AddScoped<IForgotPasswordUseCase, ForgotPasswordUseCase>();
        services.AddScoped<IResetPasswordUseCase, ResetPasswordUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();
        services.AddScoped<IGetUserTenantsUseCase, GetUserTenantsUseCase>();
        services.AddScoped<ICreateTenantUseCase, CreateTenantUseCase>();
        services.AddScoped<ISelectTenantUseCase, SelectTenantUseCase>();
        services.AddScoped<IGetCurrentUserUseCase, GetCurrentUserUseCase>();
        services.AddScoped<IGetUserInfoUseCase, GetUserInfoUseCase>();
        services.AddScoped<IGetAvailableModulesUseCase, GetAvailableModulesUseCase>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<SessionIssuer>();
        services.AddSingleton<PasswordPolicy>();
        return services;
    }
}
