using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Infrastructure.Persistence.Application;
using SaviaUp.Backend.Infrastructure.Persistence.Platform;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(
    PlatformDbContext platformContext,
    ApplicationDbContext appContext) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken)
        => platformContext.Tenants.AsNoTracking().SingleOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);

    public Task<TenantMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
        => platformContext.TenantMemberships
            .Include(membership => membership.Tenant)
            .SingleOrDefaultAsync(membership => membership.UserId == userId && membership.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyCollection<TenantDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var memberships = await platformContext.TenantMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && (m.IsActive || m.DisabledUntil <= DateTimeOffset.UtcNow) && m.Tenant.IsActive)
            .Select(m => new { m.TenantId, TenantName = m.Tenant.Name, m.RoleId })
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0) return [];

        var roleIds = memberships.Select(m => m.RoleId).Distinct().ToList();
        var roles = await appContext.Roles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => roleIds.Contains(r.Id) && r.IsActive)
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var result = new List<TenantDto>();
        foreach (var m in memberships)
        {
            if (roles.TryGetValue(m.RoleId, out var roleName))
            {
                result.Add(new TenantDto(m.TenantId, m.TenantName, m.RoleId, roleName));
            }
        }

        return result.OrderBy(t => t.Name).ToArray();
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken)
        => await platformContext.Tenants.AddAsync(tenant, cancellationToken);

    public async Task AddMembershipAsync(TenantMembership membership, CancellationToken cancellationToken)
        => await platformContext.TenantMemberships.AddAsync(membership, cancellationToken);
}
