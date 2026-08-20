using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(SaviaUpDbContext context) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken)
        => context.Tenants.AsNoTracking().SingleOrDefaultAsync(tenant => tenant.Id == tenantId, cancellationToken);

    public Task<TenantMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
        => context.TenantMemberships
            .Include(membership => membership.Tenant)
            .Include(membership => membership.Role)
            .SingleOrDefaultAsync(membership => membership.UserId == userId && membership.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyCollection<TenantDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
        => await context.TenantMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && membership.IsActive && membership.Tenant.IsActive && membership.Role.IsActive)
            .OrderBy(membership => membership.Tenant.Name)
            .Select(membership => new TenantDto(membership.TenantId, membership.Tenant.Name, membership.RoleId, membership.Role.Name))
            .ToArrayAsync(cancellationToken);

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken)
        => await context.Tenants.AddAsync(tenant, cancellationToken);

    public async Task AddMembershipAsync(TenantMembership membership, CancellationToken cancellationToken)
        => await context.TenantMemberships.AddAsync(membership, cancellationToken);
}
