using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Time;

public sealed class OrganizationTimeZone(PlatformDbContext context) : IOrganizationTimeZone
{
    public Task<string> GetAsync(Guid tenantId, CancellationToken cancellationToken) =>
        context.Tenants.AsNoTracking().Where(t => t.Id == tenantId)
            .Select(t => t.TimeZoneId).SingleAsync(cancellationToken);
}
