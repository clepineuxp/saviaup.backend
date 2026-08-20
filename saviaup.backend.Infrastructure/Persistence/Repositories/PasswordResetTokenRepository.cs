using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository(SaviaUpDbContext context) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        => context.PasswordResetTokens.AsNoTracking().SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken)
        => await context.PasswordResetTokens.AddAsync(token, cancellationToken);

    public async Task<bool> TryMarkUsedAsync(Guid tokenId, DateTimeOffset usedAt, CancellationToken cancellationToken)
    {
        if (context.Database.IsRelational())
            return await context.PasswordResetTokens
                .Where(token => token.Id == tokenId && token.UsedAt == null)
                .ExecuteUpdateAsync(updates => updates.SetProperty(token => token.UsedAt, usedAt), cancellationToken) == 1;
        var token = await context.PasswordResetTokens.SingleOrDefaultAsync(value => value.Id == tokenId && value.UsedAt == null, cancellationToken);
        if (token is null) return false;
        token.UsedAt = usedAt;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
