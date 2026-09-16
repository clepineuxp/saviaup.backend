using Moq;
using SaviaUp.Backend.Core.Authentication;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class LoginAndRegisterUseCaseTests
{
    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsSession()
    {
        var user = User();
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByNormalizedEmailAsync("ANA@TEST.COM", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(service => service.Verify(user.PasswordHash, "Secure123!*")).Returns(true);
        var unitOfWork = new Mock<IUnitOfWork>();
        var useCase = new LoginUseCase(users.Object, Mock.Of<ITenantRepository>(), Mock.Of<IRoleRepository>(), hasher.Object, TestSupport.SessionIssuer(), new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(new LoginRequest("ana@test.com", "Secure123!*"), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.RequiresTenantSelection);
        Assert.Equal("access-token", result.Value.AccessToken);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsSameInvalidCredentialsError()
    {
        var useCase = LoginUseCaseWith(null, true);
        var result = await useCase.ExecuteAsync(new LoginRequest("missing@test.com", "Secure123!*"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.InvalidCredentials, result.Error!.Code);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsSameInvalidCredentialsError()
    {
        var useCase = LoginUseCaseWith(User(), false);
        var result = await useCase.ExecuteAsync(new LoginRequest("ana@test.com", "wrong"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.InvalidCredentials, result.Error!.Code);
    }

    [Fact]
    public async Task Login_WithDisabledUser_IsRejected()
    {
        var user = User();
        user.IsActive = false;
        var useCase = LoginUseCaseWith(user, true);
        var result = await useCase.ExecuteAsync(new LoginRequest("ana@test.com", "Secure123!*"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.AccountDisabled, result.Error!.Code);
    }

    [Fact]
    public async Task Register_WithValidInput_CreatesUserAndSession()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(service => service.Hash(It.IsAny<string>())).Returns("HASHED-PASSWORD");
        var unitOfWork = new Mock<IUnitOfWork>();
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(value => value.GetPendingInvitationsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var useCase = new RegisterUseCase(users.Object, Mock.Of<ITenantRepository>(), settings.Object, hasher.Object, TestSupport.PasswordPolicy(), TestSupport.SessionIssuer(), new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(new RegisterRequest("Ana", "Test", "ana@test.com", "Secure123!*"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("tenant-selection", result.Value!.NextStep);
        users.Verify(repository => repository.AddAsync(It.Is<User>(user => user.NormalizedEmail == "ANA@TEST.COM" && user.PasswordHash == "HASHED-PASSWORD"), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(User());
        var useCase = new RegisterUseCase(users.Object, Mock.Of<ITenantRepository>(), Mock.Of<ISettingsRepository>(), Mock.Of<IPasswordHasher>(), TestSupport.PasswordPolicy(), TestSupport.SessionIssuer(), new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());
        var result = await useCase.ExecuteAsync(new RegisterRequest("Ana", "Test", "ana@test.com", "Secure123!*"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.EmailAlreadyExists, result.Error!.Code);
    }

    private static LoginUseCase LoginUseCaseWith(User? user, bool passwordValid)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(service => service.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(passwordValid);
        return new LoginUseCase(users.Object, Mock.Of<ITenantRepository>(), Mock.Of<IRoleRepository>(), hasher.Object, TestSupport.SessionIssuer(), new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());
    }

    private static User User() => new()
    {
        Id = Guid.NewGuid(),
        Email = "ana@test.com",
        NormalizedEmail = "ANA@TEST.COM",
        PasswordHash = "HASH",
        FirstName = "Ana",
        LastName = "Test",
        IsActive = true
    };
}
