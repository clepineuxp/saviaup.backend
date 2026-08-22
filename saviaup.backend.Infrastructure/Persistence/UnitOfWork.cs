using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Persistence;

public sealed class UnitOfWork(
    PlatformDbContext platformContext,
    ApplicationDbContext appContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var platformChanges = await platformContext.SaveChangesAsync(cancellationToken);
        var appChanges = await appContext.SaveChangesAsync(cancellationToken);
        return platformChanges + appChanges;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        if (!appContext.Database.IsRelational()) return await action(cancellationToken);
        await using var transaction = await appContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
