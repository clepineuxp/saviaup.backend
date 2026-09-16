using Microsoft.AspNetCore.Authorization;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Api.Middleware;

public sealed class PermissionAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserContext currentUser,
        IRefreshTokenRepository refreshTokenRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository,
        IPermissionService permissionService,
        IDateTimeProvider dateTimeProvider)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        var permissions = endpoint?.Metadata.GetOrderedMetadata<RequirePermissionAttribute>() ?? [];
        var requiresTenant = endpoint?.Metadata.GetMetadata<RequireTenantAttribute>() is not null || permissions.Count > 0;
        var requiresAuthentication = endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null || requiresTenant;
        if (!requiresAuthentication)
        {
            await next(context);
            return;
        }

        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue || !currentUser.SessionId.HasValue)
        {
            await WriteAsync(context, StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, LocalizationKeys.Unauthenticated);
            return;
        }
        if (!await refreshTokenRepository.HasActiveSessionAsync(currentUser.UserId.Value, currentUser.SessionId.Value, dateTimeProvider.UtcNow, context.RequestAborted))
        {
            await WriteAsync(context, StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, LocalizationKeys.Unauthenticated);
            return;
        }
        if (requiresTenant && (!currentUser.TenantId.HasValue || !currentUser.RoleId.HasValue))
        {
            await WriteAsync(context, StatusCodes.Status403Forbidden, ErrorCodes.TenantRequired, LocalizationKeys.TenantRequired);
            return;
        }
        if (requiresTenant)
        {
            var membership = await tenantRepository.GetMembershipAsync(
                currentUser.UserId.Value,
                currentUser.TenantId!.Value,
                context.RequestAborted);
            var role = membership is not null ? await roleRepository.GetByIdAsync(membership.RoleId, context.RequestAborted) : null;
            if (membership is null
                || !membership.IsEnabledAt(dateTimeProvider.UtcNow)
                || !membership.Tenant.IsActive
                || role is null
                || !role.IsActive
                || membership.RoleId != currentUser.RoleId)
            {
                await WriteAsync(context, StatusCodes.Status403Forbidden, ErrorCodes.TenantAccessDenied, LocalizationKeys.TenantAccessDenied);
                return;
            }
        }

        var requestedTenant = context.Request.Headers[HeaderNames.TenantId].ToString();
        if (!string.IsNullOrWhiteSpace(requestedTenant)
            && currentUser.TenantId.HasValue
            && (!Guid.TryParse(requestedTenant, out var tenantHeader) || tenantHeader != currentUser.TenantId.Value))
        {
            await WriteAsync(context, StatusCodes.Status403Forbidden, ErrorCodes.TenantAccessDenied, LocalizationKeys.TenantAccessDenied);
            return;
        }

        foreach (var permission in permissions)
        {
            var isAllowed = false;
            foreach (var code in permission.PermissionCodes)
            {
                if (await permissionService.IsAllowedAsync(currentUser.TenantId!.Value, currentUser.RoleId!.Value, code, context.RequestAborted))
                {
                    isAllowed = true;
                    break;
                }
            }

            if (!isAllowed)
            {
                await WriteAsync(context, StatusCodes.Status403Forbidden, ErrorCodes.Forbidden, LocalizationKeys.Forbidden);
                return;
            }
        }
        await next(context);
    }

    private static Task WriteAsync(HttpContext context, int status, string code, string key)
        => context.WriteErrorAsync(status, code, TranslationCatalog.Translate(key, context.Request.Headers.AcceptLanguage.ToString()));
}
