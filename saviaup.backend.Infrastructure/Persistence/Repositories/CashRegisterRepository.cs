using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class CashRegisterRepository(ApplicationDbContext context) : ICashRegisterRepository
{
    public async Task<IReadOnlyCollection<CashRegister>> GetForTenantAsync(
        Guid tenantId,
        bool includeInactive,
        CancellationToken cancellationToken)
        => await context.CashRegisters
            .AsNoTracking()
            .Where(cr => cr.TenantId == tenantId && (includeInactive || cr.IsActive))
            .OrderBy(cr => cr.NormalizedName)
            .ToArrayAsync(cancellationToken);

    public Task<CashRegister?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        => context.CashRegisters.SingleOrDefaultAsync(
            cr => cr.TenantId == tenantId && cr.Id == id,
            cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludedId,
        CancellationToken cancellationToken)
        => context.CashRegisters.AsNoTracking().AnyAsync(
            cr => cr.TenantId == tenantId
                && cr.NormalizedName == normalizedName
                && (!excludedId.HasValue || cr.Id != excludedId.Value),
            cancellationToken);

    public Task<bool> HasOtherActiveAsync(
        Guid tenantId,
        Guid? excludedId,
        CancellationToken cancellationToken)
        => context.CashRegisters.AsNoTracking().AnyAsync(
            cr => cr.TenantId == tenantId
                && cr.IsActive
                && (!excludedId.HasValue || cr.Id != excludedId.Value),
            cancellationToken);

    public async Task AddAsync(CashRegister cashRegister, CancellationToken cancellationToken)
        => await context.CashRegisters.AddAsync(cashRegister, cancellationToken);

    public Task<bool> IsInUseAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        => context.CashRegisterShifts.AsNoTracking()
            .AnyAsync(shift => shift.TenantId == tenantId && shift.CashRegisterId == id, cancellationToken);

    public void Remove(CashRegister cashRegister) => context.CashRegisters.Remove(cashRegister);
}
