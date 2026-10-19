using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class OrganizationAccessLinkRepository(PlatformDbContext context) : IOrganizationAccessLinkRepository
{
    public async Task AddAsync(OrganizationAccessLink link, CancellationToken cancellationToken)
        => await context.OrganizationAccessLinks.AddAsync(link, cancellationToken);

    public Task<OrganizationAccessLink?> GetByCodeHashAsync(string codeHash, CancellationToken cancellationToken)
        => context.OrganizationAccessLinks
            .Include(link => link.Tenant)
            .Include(link => link.RequestedByUser)
            .SingleOrDefaultAsync(link => link.CodeHash == codeHash, cancellationToken);

    public async Task<IReadOnlyCollection<OrganizationAccessLink>> GetOpenInvitationsAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
        => await context.OrganizationAccessLinks
            .Include(link => link.Tenant)
            .Where(link => link.TenantId == tenantId && link.Kind == OrganizationAccessLinkKind.Invitation
                && link.AcceptedAt == null && link.ExpiresAt > now)
            .OrderByDescending(link => link.CreatedAt).ToArrayAsync(cancellationToken);
}
