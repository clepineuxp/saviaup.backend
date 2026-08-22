using Microsoft.Extensions.Options;
using Moq;
using SaviaUp.Backend.Core.Authentication;
using SaviaUp.Backend.Core.Security;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Tests;

internal sealed class FixedClock(DateTimeOffset utcNow) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

internal static class TestSupport
{
    public static readonly DateTimeOffset Now = new(2026, 8, 19, 20, 0, 0, TimeSpan.Zero);

    public static PasswordPolicy PasswordPolicy()
        => new(Options.Create(new PasswordPolicyOptions()));

    public static SessionIssuer SessionIssuer(
        Mock<IRefreshTokenRepository>? refreshTokens = null,
        Mock<ITokenGenerator>? tokenGenerator = null,
        Mock<IRoleRepository>? roles = null,
        Mock<IPermissionRepository>? permissions = null)
    {
        refreshTokens ??= new Mock<IRefreshTokenRepository>();
        tokenGenerator ??= new Mock<ITokenGenerator>();
        roles ??= new Mock<IRoleRepository>();
        permissions ??= new Mock<IPermissionRepository>();
        tokenGenerator.Setup(generator => generator.Generate()).Returns("raw-refresh-token");
        permissions.Setup(repository => repository.GetForRoleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(service => service.CreateAccessToken(It.IsAny<Domain.Entities.User>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .Returns(new AccessToken("access-token", Now.AddMinutes(15)));
        return new SessionIssuer(
            jwt.Object,
            tokenGenerator.Object,
            refreshTokens.Object,
            roles.Object,
            permissions.Object,
            new FixedClock(Now),
            Options.Create(new JwtOptions { RefreshTokenExpirationDays = 30 }));
    }

    public static void RunTransaction<T>(Mock<IUnitOfWork> unitOfWork)
        => unitOfWork
            .Setup(work => work.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<T>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<T>> action, CancellationToken token) => action(token));
}
