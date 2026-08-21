using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class CashRegisterShiftRepository(SaviaUpDbContext context) : ICashRegisterShiftRepository
{
    public Task<bool> HasOpenShiftAsync(Guid tenantId, CancellationToken cancellationToken)
        => context.CashRegisterShifts.AsNoTracking()
            .AnyAsync(shift => shift.TenantId == tenantId && shift.ClosedAt == null, cancellationToken);
}
