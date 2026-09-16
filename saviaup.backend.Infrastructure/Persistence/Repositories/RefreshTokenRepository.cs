using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(PlatformDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => context.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
        => await context.RefreshTokens.AddAsync(token, cancellationToken);

    public async Task<bool> TryRevokeAsync(Guid tokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        if (context.Database.IsRelational())
            return await context.RefreshTokens
                .Where(token => token.Id == tokenId && token.RevokedAt == null)
                .ExecuteUpdateAsync(updates => updates.SetProperty(token => token.RevokedAt, revokedAt), cancellationToken) == 1;
        var token = await context.RefreshTokens.SingleOrDefaultAsync(value => value.Id == tokenId && value.RevokedAt == null, cancellationToken);
        if (token is null) return false;
        token.RevokedAt = revokedAt;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task SetReplacementAsync(Guid tokenId, Guid replacementId, CancellationToken cancellationToken)
    {
        if (context.Database.IsRelational())
        {
            await context.RefreshTokens.Where(token => token.Id == tokenId)
                .ExecuteUpdateAsync(updates => updates.SetProperty(token => token.ReplacedByTokenId, replacementId), cancellationToken);
            return;
        }
        var token = await context.RefreshTokens.SingleAsync(value => value.Id == tokenId, cancellationToken);
        token.ReplacedByTokenId = replacementId;
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task RevokeSessionAsync(Guid userId, Guid sessionId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        => RevokeWhereAsync(token => token.UserId == userId && token.SessionId == sessionId && token.RevokedAt == null, revokedAt, cancellationToken);

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        => RevokeWhereAsync(token => token.UserId == userId && token.RevokedAt == null, revokedAt, cancellationToken);

    public Task<bool> HasActiveSessionAsync(Guid userId, Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken)
        => context.RefreshTokens.AsNoTracking().AnyAsync(
            token => token.UserId == userId && token.SessionId == sessionId && token.RevokedAt == null && token.ExpiresAt > now,
            cancellationToken);

    private async Task RevokeWhereAsync(
        System.Linq.Expressions.Expression<Func<RefreshToken, bool>> predicate,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken)
    {
        if (context.Database.IsRelational())
        {
            await context.RefreshTokens.Where(predicate)
                .ExecuteUpdateAsync(updates => updates.SetProperty(token => token.RevokedAt, revokedAt), cancellationToken);
            return;
        }
        var tokens = await context.RefreshTokens.Where(predicate).ToArrayAsync(cancellationToken);
        foreach (var token in tokens) token.RevokedAt = revokedAt;
    }
}
