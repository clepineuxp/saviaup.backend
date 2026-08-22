using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Middleware;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class PermissionAuthorizationMiddlewareTests
{
    [Fact]
    public async Task MissingJwt_Returns401()
    {
        var context = Context(new AuthorizeAttribute());
        var middleware = new PermissionAuthorizationMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            Mock.Of<ICurrentUserContext>(),
            Mock.Of<IRefreshTokenRepository>(),
            Mock.Of<ITenantRepository>(),
            Mock.Of<IRoleRepository>(),
            Mock.Of<IPermissionService>(),
            Mock.Of<IDateTimeProvider>());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("AUTH_UNAUTHENTICATED", await ErrorCode(context));
    }

    [Fact]
    public async Task AuthenticatedUserWithoutPermission_Returns403()
    {
        var user = AuthenticatedUser();
        var sessions = new Mock<IRefreshTokenRepository>();
        sessions.Setup(repository => repository.HasActiveSessionAsync(user.Object.UserId!.Value, user.Object.SessionId!.Value, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var permissions = new Mock<IPermissionService>();
        permissions.Setup(service => service.IsAllowedAsync(user.Object.TenantId!.Value, user.Object.RoleId!.Value, "orders.create", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var context = Context(new AuthorizeAttribute(), new RequireTenantAttribute(), new RequirePermissionAttribute("orders.create"));
        var middleware = new PermissionAuthorizationMiddleware(_ => Task.CompletedTask);

        var (tenants, roles) = ActiveTenant(user);
        await middleware.InvokeAsync(context, user.Object, sessions.Object, tenants, roles, permissions.Object, Clock());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("AUTH_FORBIDDEN", await ErrorCode(context));
    }

    [Fact]
    public async Task AuthenticatedUserWithPermission_ContinuesPipeline()
    {
        var continued = false;
        var user = AuthenticatedUser();
        var sessions = new Mock<IRefreshTokenRepository>();
        sessions.Setup(repository => repository.HasActiveSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var permissions = new Mock<IPermissionService>();
        permissions.Setup(service => service.IsAllowedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), "orders.create", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var context = Context(new AuthorizeAttribute(), new RequireTenantAttribute(), new RequirePermissionAttribute("orders.create"));
        var middleware = new PermissionAuthorizationMiddleware(_ => { continued = true; return Task.CompletedTask; });

        var (tenants, roles) = ActiveTenant(user);
        await middleware.InvokeAsync(context, user.Object, sessions.Object, tenants, roles, permissions.Object, Clock());

        Assert.True(continued);
    }

    [Fact]
    public async Task InactiveTenantMembership_Returns403()
    {
        var user = AuthenticatedUser();
        var sessions = new Mock<IRefreshTokenRepository>();
        sessions.Setup(repository => repository.HasActiveSessionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var (tenants, roles) = ActiveTenant(user, isMembershipActive: false);
        var context = Context(new AuthorizeAttribute(), new RequireTenantAttribute());
        var middleware = new PermissionAuthorizationMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            user.Object,
            sessions.Object,
            tenants,
            roles,
            Mock.Of<IPermissionService>(),
            Clock());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("TENANT_ACCESS_DENIED", await ErrorCode(context));
    }

    private static DefaultHttpContext Context(params object[] metadata)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "test"));
        return context;
    }

    private static Mock<ICurrentUserContext> AuthenticatedUser()
    {
        var user = new Mock<ICurrentUserContext>();
        user.SetupGet(value => value.IsAuthenticated).Returns(true);
        user.SetupGet(value => value.UserId).Returns(Guid.NewGuid());
        user.SetupGet(value => value.SessionId).Returns(Guid.NewGuid());
        user.SetupGet(value => value.TenantId).Returns(Guid.NewGuid());
        user.SetupGet(value => value.RoleId).Returns(Guid.NewGuid());
        return user;
    }

    private static (ITenantRepository Tenants, IRoleRepository Roles) ActiveTenant(
        Mock<ICurrentUserContext> user,
        bool isMembershipActive = true)
    {
        var tenant = new Tenant { Id = user.Object.TenantId!.Value, IsActive = true };
        var role = new Role
        {
            Id = user.Object.RoleId!.Value,
            TenantId = tenant.Id,
            IsActive = true
        };
        var membership = new TenantMembership
        {
            UserId = user.Object.UserId!.Value,
            TenantId = tenant.Id,
            Tenant = tenant,
            RoleId = role.Id,
            IsActive = isMembershipActive
        };
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(value => value.GetMembershipAsync(
                user.Object.UserId.Value,
                tenant.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(value => value.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        return (tenantRepo.Object, roleRepo.Object);
    }

    private static IDateTimeProvider Clock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(value => value.UtcNow).Returns(DateTimeOffset.UtcNow);
        return clock.Object;
    }

    private static async Task<string> ErrorCode(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        var json = await JsonDocument.ParseAsync(context.Response.Body);
        return json.RootElement.GetProperty("error").GetProperty("code").GetString()!;
    }
}
