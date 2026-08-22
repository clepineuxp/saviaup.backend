using Moq;
using SaviaUp.Backend.Core.Navigation;
using SaviaUp.Backend.Core.Users;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class NavigationAndUserInfoTests
{
    [Fact]
    public async Task AvailableModules_GroupsLocalizesAndOrdersConfiguredModules()
    {
        var repository = new Mock<IModuleRepository>();
        repository.Setup(value => value.GetAvailableForRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AvailableModuleReference(Guid.NewGuid(), "kitchen"),
                new AvailableModuleReference(Guid.NewGuid(), "billing"),
                new AvailableModuleReference(Guid.NewGuid(), "tables"),
                new AvailableModuleReference(Guid.NewGuid(), "categories"),
                new AvailableModuleReference(Guid.NewGuid(), "reports"),
                new AvailableModuleReference(Guid.NewGuid(), "settings"),
                new AvailableModuleReference(Guid.NewGuid(), "products"),
                new AvailableModuleReference(Guid.NewGuid(), "orders"),
                new AvailableModuleReference(Guid.NewGuid(), "inventory")
            ]);
        var (useCase, membership, _) = AvailableModulesUseCase(repository);

        var result = await useCase.ExecuteAsync(
            membership.UserId,
            membership.TenantId,
            membership.RoleId,
            "es-CO",
            default);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value!.Sections,
            section =>
            {
                Assert.Equal(("sales", "Ventas", 1, false),
                    (section.Code, section.Name, section.Order, section.IsGrouped));
                var module = Assert.Single(section.Modules);
                Assert.Equal(("tables", "Mesas", 1), (module.Code, module.Name, module.Order));
            },
            section =>
            {
                Assert.Equal(("operation", "Operación", 2, true),
                    (section.Code, section.Name, section.Order, section.IsGrouped));
                Assert.Equal(
                    [("orders", 1), ("reports", 2), ("billing", 3)],
                    section.Modules.Select(module => (module.Code, module.Order)));
            },
            section =>
            {
                Assert.Equal(("inventory", "Inventario", 3, true),
                    (section.Code, section.Name, section.Order, section.IsGrouped));
                Assert.Equal(
                    [("products", 1), ("categories", 2), ("inventory", 3), ("kitchen", 4)],
                    section.Modules.Select(module => (module.Code, module.Order)));
                Assert.Equal("Categorías", section.Modules.ElementAt(1).Name);
            },
            section =>
            {
                Assert.Equal(("configuration", "Configuración", 4, false),
                    (section.Code, section.Name, section.Order, section.IsGrouped));
                Assert.Equal("settings", Assert.Single(section.Modules).Code);
            });
        Assert.Null(result.Value.EmptyStateMessage);
    }

    [Fact]
    public async Task AvailableModules_WithOnlyOneVisibleModuleInASection_DoesNotCreateGroup()
    {
        var repository = new Mock<IModuleRepository>();
        repository.Setup(value => value.GetAvailableForRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AvailableModuleReference(Guid.NewGuid(), "orders")]);
        var (useCase, membership, _) = AvailableModulesUseCase(repository);

        var result = await useCase.ExecuteAsync(
            membership.UserId,
            membership.TenantId,
            membership.RoleId,
            "es",
            default);

        var section = Assert.Single(result.Value!.Sections);
        Assert.Equal("operation", section.Code);
        Assert.False(section.IsGrouped);
        Assert.Equal("orders", Assert.Single(section.Modules).Code);
    }

    [Fact]
    public async Task AvailableModules_WithModuleMissingFromCatalog_FailsExplicitly()
    {
        var repository = new Mock<IModuleRepository>();
        repository.Setup(value => value.GetAvailableForRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AvailableModuleReference(Guid.NewGuid(), "future-module")]);
        var (useCase, membership, _) = AvailableModulesUseCase(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            membership.UserId,
            membership.TenantId,
            membership.RoleId,
            "es",
            default));

        Assert.Contains("must define its navigation section and order", exception.Message);
    }

    [Theory]
    [InlineData("es", "Habla con el administrador")]
    [InlineData("en-US", "Contact your organization administrator")]
    public async Task AvailableModules_WithoutPermissions_ReturnsLocalizedAdministratorMessage(
        string language,
        string expectedText)
    {
        var repository = new Mock<IModuleRepository>();
        repository.Setup(value => value.GetAvailableForRoleAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var (useCase, membership, _) = AvailableModulesUseCase(repository);

        var result = await useCase.ExecuteAsync(
            membership.UserId,
            membership.TenantId,
            membership.RoleId,
            language,
            default);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Sections);
        Assert.Contains(expectedText, result.Value.EmptyStateMessage);
    }

    [Fact]
    public async Task UserInfo_WithActiveMembership_ReturnsUserOrganizationAndRole()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Prueba",
            IsActive = true
        };
        var (membership, role) = Membership(user);
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(value => value.GetMembershipAsync(user.Id, membership.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(value => value.GetByIdAsync(membership.RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var useCase = new GetUserInfoUseCase(users.Object, tenants.Object, roles.Object, new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(user.Id, membership.TenantId, membership.RoleId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(("Ana", "Prueba"), (result.Value!.FirstName, result.Value.LastName));
        Assert.Equal("Secret Garden", result.Value.Organization.Name);
        Assert.Equal(("TENANT_OWNER", "Owner"), (result.Value.Role.Code, result.Value.Role.Name));
    }

    [Fact]
    public async Task UserInfo_WhenTokenRoleDoesNotMatchMembership_IsForbidden()
    {
        var user = new User { Id = Guid.NewGuid(), IsActive = true };
        var (membership, role) = Membership(user);
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(value => value.GetMembershipAsync(user.Id, membership.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(value => value.GetByIdAsync(membership.RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var useCase = new GetUserInfoUseCase(users.Object, tenants.Object, roles.Object, new FixedClock(TestSupport.Now));

        var result = await useCase.ExecuteAsync(user.Id, membership.TenantId, Guid.NewGuid(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.TenantAccessDenied, result.Error!.Code);
    }

    private static (GetAvailableModulesUseCase UseCase, TenantMembership Membership, Role Role) AvailableModulesUseCase(
        Mock<IModuleRepository> modules)
    {
        var (membership, role) = Membership(new User { Id = Guid.NewGuid(), IsActive = true });
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(value => value.GetMembershipAsync(
                membership.UserId,
                membership.TenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(value => value.GetByIdAsync(
                membership.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        var permissions = new Mock<IPermissionRepository>();
        permissions.Setup(value => value.GetForRoleAsync(
                membership.TenantId,
                membership.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return (new GetAvailableModulesUseCase(modules.Object, tenants.Object, roles.Object, permissions.Object, new FixedClock(TestSupport.Now)), membership, role);
    }

    private static (TenantMembership Membership, Role Role) Membership(User user)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Secret Garden", IsActive = true };
        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Code = "TENANT_OWNER",
            Name = "Owner",
            IsActive = true
        };
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TenantId = tenant.Id,
            Tenant = tenant,
            RoleId = role.Id,
            IsActive = true
        };
        return (membership, role);
    }
}
