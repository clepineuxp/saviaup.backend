using Moq;
using Microsoft.Extensions.Options;
using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class SettingsUseCaseTests
{
    [Fact]
    public async Task Organization_NonOwnerCannotChangeDocument()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Savia", Document = "900", UpdatedAt = TestSupport.Now };
        var role = new Role { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "MANAGER" };
        var repository = new Mock<ISettingsRepository>();
        repository.Setup(value => value.GetTenantForUpdateAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(value => value.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var useCase = new OrganizationSettingsUseCase(repository.Object, roles.Object, new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.UpdateAsync(tenant.Id, role.Id,
            new UpdateOrganizationSettingsRequest("Savia", null, "901", null, null, null, null, null, null, null, null), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.OrganizationDocumentOwnerOnly, result.Error!.Code);
    }

    [Fact]
    public async Task Organization_OwnerUpdatesNameAndDocument()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Old", UpdatedAt = TestSupport.Now.AddDays(-1) };
        var role = new Role { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "TENANT_OWNER" };
        var repository = new Mock<ISettingsRepository>();
        repository.Setup(value => value.GetTenantForUpdateAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        var roles = new Mock<IRoleRepository>(); roles.Setup(value => value.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var useCase = new OrganizationSettingsUseCase(repository.Object, roles.Object, new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.UpdateAsync(tenant.Id, role.Id,
            new UpdateOrganizationSettingsRequest("  Mi   Organización ", "Ana", "900", null, null, null, null, null, null, null, null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Mi Organización", tenant.Name);
        Assert.Equal("900", tenant.Document);
        Assert.True(result.Value!.CanEditDocument);
    }

    [Fact]
    public async Task Business_UpdatePersistsTypedParametersAndCashRule()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Savia" };
        var parameters = SettingsDefaults.CreateBusinessParameters(tenant.Id, TestSupport.Now).ToArray();
        var repository = new Mock<ISettingsRepository>();
        repository.Setup(value => value.GetTenantForUpdateAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repository.Setup(value => value.GetParametersAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(parameters);
        repository.Setup(value => value.GetEnabledPermissionCodesAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([PermissionCodes.CashRegistersManage]);
        var useCase = new BusinessSettingsUseCase(repository.Object, new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.UpdateAsync(tenant.Id, new UpdateBusinessSettingsRequest(true, true, true, true, true, "Propina voluntaria", 12), default);

        Assert.True(result.IsSuccess);
        Assert.True(tenant.RequiresOpenCashRegister);
        Assert.True(result.Value!.EnableCustomSales);
        Assert.Equal("12", parameters.Single(item => item.Key == SettingsDefaults.SuggestedTipPercentage).Value);
    }

    [Fact]
    public async Task Business_UpdateWhenCashRegisterModuleNotEnabled_ForcesCashRuleToFalse()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Savia", RequiresOpenCashRegister = true };
        var parameters = SettingsDefaults.CreateBusinessParameters(tenant.Id, TestSupport.Now).ToArray();
        var repository = new Mock<ISettingsRepository>();
        repository.Setup(value => value.GetTenantForUpdateAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repository.Setup(value => value.GetParametersAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(parameters);
        repository.Setup(value => value.GetEnabledPermissionCodesAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([PermissionCodes.TablesRead]);
        var useCase = new BusinessSettingsUseCase(repository.Object, new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.UpdateAsync(tenant.Id, new UpdateBusinessSettingsRequest(true, true, true, false, true, "Propina voluntaria", 10), default);

        Assert.True(result.IsSuccess);
        Assert.False(tenant.RequiresOpenCashRegister);
        Assert.False(result.Value!.RequiresOpenCashRegister);
        Assert.False(result.Value!.EnableCustomSales);
    }

    [Fact]
    public async Task PaymentMethod_DuplicateNameIsRejectedCaseInsensitive()
    {
        var repository = new Mock<ISettingsRepository>();
        repository.Setup(value => value.PaymentMethodNameExistsAsync(It.IsAny<Guid>(), "EFECTIVO", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var useCase = new PaymentMethodsSettingsUseCase(repository.Object, new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.CreateAsync(Guid.NewGuid(), new SavePaymentMethodRequest(" efectivo ", true), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.PaymentMethodAlreadyExists, result.Error!.Code);
    }

    [Fact]
    public async Task Role_CannotReceivePermissionDisabledForOrganization()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ISettingsRepository>();
        repository.Setup(value => value.RoleCodeOrNameExistsAsync(tenantId, It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(value => value.GetEnabledPermissionCodesAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync([PermissionCodes.ProductsRead]);
        var useCase = new AccessSettingsUseCase(repository.Object, Mock.Of<IUserRepository>(), Mock.Of<ITenantRepository>(), Mock.Of<IEmailSender>(),
            Options.Create(new FrontendOptions()), new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.CreateRoleAsync(tenantId, new SaveSettingsRoleRequest("Mesero", null, [PermissionCodes.SettingsManage]), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.PermissionNotEnabled, result.Error!.Code);
    }
}
