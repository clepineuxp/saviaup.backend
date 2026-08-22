using Microsoft.Extensions.Options;
using Moq;
using SaviaUp.Backend.Core.Authentication;
using SaviaUp.Backend.Core.Security;
using SaviaUp.Backend.Core.Tenants;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class TenantPermissionAndRecoveryTests
{
    [Fact]
    public async Task ForgotPassword_DoesNotRevealMissingEmail()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var email = new Mock<IEmailSender>();
        var useCase = new ForgotPasswordUseCase(
            users.Object,
            Mock.Of<IPasswordResetTokenRepository>(),
            Mock.Of<ITokenGenerator>(),
            email.Object,
            new FixedClock(TestSupport.Now),
            Mock.Of<IUnitOfWork>(),
            Options.Create(new FrontendOptions()));

        var result = await useCase.ExecuteAsync(new ForgotPasswordRequest("missing@test.com"), default);

        Assert.True(result.IsSuccess);
        email.Verify(sender => sender.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SelectTenant_WithMembership_IssuesContextualTokens()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "owner@test.com", FirstName = "Owner", LastName = "Test", IsActive = true };
        var (membership, role) = Membership(user);
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(repository => repository.GetMembershipAsync(user.Id, membership.TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(membership);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(repository => repository.GetByIdAsync(membership.RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var refreshTokens = new Mock<IRefreshTokenRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TestSupport.RunTransaction<Result<TenantSessionResponse>>(unitOfWork);
        var useCase = new SelectTenantUseCase(users.Object, tenants.Object, roles.Object, refreshTokens.Object, TestSupport.SessionIssuer(refreshTokens), new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(user.Id, Guid.NewGuid(), membership.TenantId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(membership.TenantId, result.Value!.Tenant.Id);
        Assert.Equal("access-token", result.Value.Tokens.AccessToken);
    }

    [Fact]
    public async Task SelectTenant_WithoutMembership_IsForbidden()
    {
        var user = new User { Id = Guid.NewGuid(), IsActive = true };
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(repository => repository.GetMembershipAsync(user.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TenantMembership?)null);
        var useCase = new SelectTenantUseCase(users.Object, tenants.Object, Mock.Of<IRoleRepository>(), Mock.Of<IRefreshTokenRepository>(), TestSupport.SessionIssuer(), new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());

        var result = await useCase.ExecuteAsync(user.Id, Guid.NewGuid(), Guid.NewGuid(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.TenantAccessDenied, result.Error!.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PermissionService_UsesRepositoryDecision(bool allowed)
    {
        var repository = new Mock<IPermissionRepository>();
        repository.Setup(value => value.RoleHasPermissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), PermissionCodes.OrdersRead, It.IsAny<CancellationToken>())).ReturnsAsync(allowed);
        var service = new PermissionService(repository.Object);
        Assert.Equal(allowed, await service.IsAllowedAsync(Guid.NewGuid(), Guid.NewGuid(), PermissionCodes.OrdersRead, default));
    }

    private static (TenantMembership Membership, Role Role) Membership(User user)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Secret Garden", IsActive = true };
        var role = new Role { Id = Guid.NewGuid(), TenantId = tenant.Id, Code = "TENANT_OWNER", Name = "Owner", IsActive = true };
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
