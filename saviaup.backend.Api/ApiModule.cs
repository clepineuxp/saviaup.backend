using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SaviaUp.Backend.Api.Configuration;
using SaviaUp.Backend.Api.Health;
using SaviaUp.Backend.Api.Hubs;
using SaviaUp.Backend.Api.Middleware;
using SaviaUp.Backend.Api.Models;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Api;

public static class ApiModule
{
    private const string CorsPolicy = "SaviaUpFrontend";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());
                var language = context.HttpContext.Request.Headers.AcceptLanguage.ToString();
                return new BadRequestObjectResult(ApiErrorResponse.Create(
                    ErrorCodes.Validation,
                    TranslationCatalog.Translate(LocalizationKeys.Validation, language),
                    details));
            };
        });

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, "Jwt:SigningKey must contain at least 32 bytes.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience), "Jwt issuer and audience are required.")
            .ValidateOnStart();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, configuredJwt) =>
            {
                var jwt = configuredJwt.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrWhiteSpace(accessToken)
                            && context.HttpContext.Request.Path.StartsWithSegments("/hubs/tables"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
        services.AddSignalR();
        services.AddScoped<ITableRealtimeNotifier, TableRealtimeNotifier>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Savia Up API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid access token."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
            });
        });
        services.AddHealthChecks()
            .AddCheck("application", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
            .AddCheck<PostgresHealthCheck>("postgresql");
        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        if (app.Environment.IsProduction()) app.UseHttpsRedirection();
        app.UseCors(CorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<PermissionAuthorizationMiddleware>();
        app.UseAuthorization();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.MapControllers();
        app.MapHub<TablesHub>("/hubs/tables");
        app.MapHealthChecks("/health", new HealthCheckOptions { AllowCachingResponses = false });
        return app;
    }
}
