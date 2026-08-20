using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Core.Authentication;

public sealed record IssuedSession(AuthSessionDto Session, Guid RefreshTokenId);

public sealed class SessionIssuer(
    IJwtTokenService jwtTokenService,
    ITokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokenRepository,
    IPermissionRepository permissionRepository,
    IDateTimeProvider dateTimeProvider,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<IssuedSession> IssueAsync(
        User user,
        Guid sessionId,
        TenantMembership? membership,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;
        var rawRefreshToken = tokenGenerator.Generate();
        var accessToken = jwtTokenService.CreateAccessToken(user, sessionId, membership?.TenantId, membership?.RoleId);
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionId = sessionId,
            TenantId = membership?.TenantId,
            RoleId = membership?.RoleId,
            TokenHash = tokenGenerator.Hash(rawRefreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenExpirationDays)
        };
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        IReadOnlyCollection<string> permissions = [];
        RoleDto? role = null;
        ActiveTenantDto? tenant = null;
        if (membership is not null)
        {
            permissions = await permissionRepository.GetForRoleAsync(membership.TenantId, membership.RoleId, cancellationToken);
            role = new RoleDto(membership.RoleId, membership.Role.Code, membership.Role.Name);
            tenant = new ActiveTenantDto(membership.TenantId, membership.Tenant.Name);
        }

        var userDto = new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PreferredLanguage,
            tenant,
            role,
            permissions);
        var session = new AuthSessionDto(
            accessToken.Value,
            rawRefreshToken,
            accessToken.ExpiresAt,
            accessToken.ExpiresAt,
            refreshToken.ExpiresAt,
            membership is null,
            userDto,
            tenant);
        return new IssuedSession(session, refreshToken.Id);
    }

    public static TokenResponse ToTokenResponse(AuthSessionDto session)
        => new(session.AccessToken, session.RefreshToken, session.ExpiresAt, session.AccessTokenExpiresAt, session.RefreshTokenExpiresAt);
}
