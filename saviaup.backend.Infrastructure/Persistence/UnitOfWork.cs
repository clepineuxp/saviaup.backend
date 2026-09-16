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
        try
        {
            var appChanges = await appContext.SaveChangesAsync(cancellationToken);
            return platformChanges + appChanges;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // DIAGNÓSTICO TEMPORAL — remover después de identificar el problema
            Console.Error.WriteLine("=== DbUpdateConcurrencyException ===");
            foreach (var entry in ex.Entries)
            {
                Console.Error.WriteLine($"  Entity: {entry.Entity.GetType().Name} | State: {entry.State}");
                foreach (var prop in entry.Properties)
                {
                    Console.Error.WriteLine(
                        $"    Prop: {prop.Metadata.Name} | Original: {prop.OriginalValue} | Current: {prop.CurrentValue} | IsModified: {prop.IsModified} | IsTemporary: {prop.IsTemporary}");
                }
            }
            Console.Error.WriteLine("====================================");
            throw;
        }
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
