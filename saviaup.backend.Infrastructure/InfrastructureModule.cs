using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Email;
using SaviaUp.Backend.Infrastructure.Persistence;
using SaviaUp.Backend.Infrastructure.Persistence.Repositories;
using SaviaUp.Backend.Infrastructure.Security;
using SaviaUp.Backend.Infrastructure.Time;

namespace SaviaUp.Backend.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SaviaUp")
            ?? throw new InvalidOperationException("ConnectionStrings:SaviaUp is required.");
        services.AddDbContext<SaviaUpDbContext>(options => options.UseNpgsql(connectionString));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));
        services.Configure<PasswordPolicyOptions>(configuration.GetSection(PasswordPolicyOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();
        services.AddScoped<IMeasurementUnitRepository, MeasurementUnitRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IDiningAreaRepository, DiningAreaRepository>();
        services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
        services.AddScoped<ICashRegisterShiftRepository, CashRegisterShiftRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        if (string.Equals(configuration["Email:Mode"], "Smtp", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IEmailSender, EmailSender>();
        else
            services.AddScoped<IEmailSender, DevelopmentEmailSender>();
        return services;
    }
}
