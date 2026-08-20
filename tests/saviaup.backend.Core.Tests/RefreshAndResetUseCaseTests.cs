using Moq;
using SaviaUp.Backend.Core.Authentication;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class RefreshAndResetUseCaseTests
{
    [Fact]
    public async Task Refresh_WithActiveToken_RotatesIt()
    {
        var token = RefreshToken();
        var tokens = new Mock<IRefreshTokenRepository>();
        tokens.Setup(repository => repository.GetByHashAsync("INPUT-HASH", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        tokens.Setup(repository => repository.TryRevokeAsync(token.Id, TestSupport.Now, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var generator = new Mock<ITokenGenerator>();
        generator.Setup(service => service.Hash("raw-input")).Returns("INPUT-HASH");
        generator.Setup(service => service.Hash("raw-refresh-token")).Returns("NEW-HASH");
        generator.Setup(service => service.Generate()).Returns("raw-refresh-token");
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(User(token.UserId));
        var unitOfWork = new Mock<IUnitOfWork>();
        TestSupport.RunTransaction<Result<TokenResponse>>(unitOfWork);
        var useCase = new RefreshTokenUseCase(generator.Object, tokens.Object, users.Object, Mock.Of<ITenantRepository>(), TestSupport.SessionIssuer(tokens, generator), new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(new RefreshTokenRequest("raw-input"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("raw-refresh-token", result.Value!.RefreshToken);
        tokens.Verify(repository => repository.SetReplacementAsync(token.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_IsRejected()
    {
        var token = RefreshToken();
        token.ExpiresAt = TestSupport.Now.AddSeconds(-1);
        var result = await RefreshUseCase(token, true).ExecuteAsync(new RefreshTokenRequest("raw-input"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.RefreshExpired, result.Error!.Code);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_IsRejected()
    {
        var token = RefreshToken();
        token.RevokedAt = TestSupport.Now.AddMinutes(-1);
        var result = await RefreshUseCase(token, true).ExecuteAsync(new RefreshTokenRequest("raw-input"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.RefreshInvalid, result.Error!.Code);
    }

    [Fact]
    public async Task Refresh_WhenTokenWasConsumedConcurrently_IsRejected()
    {
        var result = await RefreshUseCase(RefreshToken(), false).ExecuteAsync(new RefreshTokenRequest("raw-input"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.RefreshInvalid, result.Error!.Code);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesHashAndRevokesSessions()
    {
        var token = ResetToken();
        var user = User(token.UserId);
        var resetTokens = new Mock<IPasswordResetTokenRepository>();
        resetTokens.Setup(repository => repository.GetByHashAsync("TOKEN-HASH", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        resetTokens.Setup(repository => repository.TryMarkUsedAsync(token.Id, TestSupport.Now, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var generator = new Mock<ITokenGenerator>();
        generator.Setup(service => service.Hash("reset-token")).Returns("TOKEN-HASH");
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(service => service.Hash("NewSecure123!*")).Returns("NEW-PASSWORD-HASH");
        var refreshTokens = new Mock<IRefreshTokenRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        TestSupport.RunTransaction<Result>(unitOfWork);
        var useCase = new ResetPasswordUseCase(resetTokens.Object, refreshTokens.Object, users.Object, generator.Object, hasher.Object, TestSupport.PasswordPolicy(), new FixedClock(TestSupport.Now), unitOfWork.Object);

        var result = await useCase.ExecuteAsync(new ResetPasswordRequest("reset-token", "NewSecure123!*", "NewSecure123!*"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("NEW-PASSWORD-HASH", user.PasswordHash);
        refreshTokens.Verify(repository => repository.RevokeAllForUserAsync(user.Id, TestSupport.Now, It.IsAny<CancellationToken>()));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    [InlineData("used")]
    public async Task ResetPassword_WithUnusableToken_IsRejected(string condition)
    {
        PasswordResetToken? token = condition == "missing" ? null : ResetToken();
        if (condition == "expired") token!.ExpiresAt = TestSupport.Now.AddSeconds(-1);
        if (condition == "used") token!.UsedAt = TestSupport.Now.AddMinutes(-1);
        var resetTokens = new Mock<IPasswordResetTokenRepository>();
        resetTokens.Setup(repository => repository.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        var generator = new Mock<ITokenGenerator>();
        generator.Setup(service => service.Hash(It.IsAny<string>())).Returns("TOKEN-HASH");
        var useCase = new ResetPasswordUseCase(resetTokens.Object, Mock.Of<IRefreshTokenRepository>(), Mock.Of<IUserRepository>(), generator.Object, Mock.Of<IPasswordHasher>(), TestSupport.PasswordPolicy(), new FixedClock(TestSupport.Now), Mock.Of<IUnitOfWork>());
        var result = await useCase.ExecuteAsync(new ResetPasswordRequest("reset-token", "NewSecure123!*", "NewSecure123!*"), default);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.PasswordResetInvalid, result.Error!.Code);
    }

    private static RefreshTokenUseCase RefreshUseCase(RefreshToken token, bool revokeSucceeds)
    {
        var tokens = new Mock<IRefreshTokenRepository>();
        tokens.Setup(repository => repository.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        tokens.Setup(repository => repository.TryRevokeAsync(token.Id, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(revokeSucceeds);
        var generator = new Mock<ITokenGenerator>();
        generator.Setup(service => service.Hash(It.IsAny<string>())).Returns("HASH");
        generator.Setup(service => service.Generate()).Returns("new-token");
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(User(token.UserId));
        var unitOfWork = new Mock<IUnitOfWork>();
        TestSupport.RunTransaction<Result<TokenResponse>>(unitOfWork);
        return new RefreshTokenUseCase(generator.Object, tokens.Object, users.Object, Mock.Of<ITenantRepository>(), TestSupport.SessionIssuer(tokens, generator), new FixedClock(TestSupport.Now), unitOfWork.Object);
    }

    private static User User(Guid id) => new() { Id = id, Email = "ana@test.com", FirstName = "Ana", LastName = "Test", IsActive = true };
    private static RefreshToken RefreshToken() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        SessionId = Guid.NewGuid(),
        TokenHash = "HASH",
        CreatedAt = TestSupport.Now.AddDays(-1),
        ExpiresAt = TestSupport.Now.AddDays(1)
    };
    private static PasswordResetToken ResetToken() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        TokenHash = "TOKEN-HASH",
        CreatedAt = TestSupport.Now.AddMinutes(-1),
        ExpiresAt = TestSupport.Now.AddMinutes(30)
    };
}
