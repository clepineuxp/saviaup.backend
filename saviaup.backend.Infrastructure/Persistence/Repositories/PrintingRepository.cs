using Microsoft.EntityFrameworkCore;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Infrastructure.Persistence.Application;

namespace SaviaUp.Backend.Infrastructure.Persistence.Repositories;

public sealed class PrintingRepository(ApplicationDbContext dbContext) : IPrintingRepository
{
    public async Task<Location> GetOrCreateDefaultLocationAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsDefault, cancellationToken);
        if (location is not null) return location;

        location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Principal",
            NormalizedName = "PRINCIPAL",
            IsDefault = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await dbContext.Locations.AddAsync(location, cancellationToken);
        return location;
    }

    public Task<Location?> GetLocationAsync(Guid tenantId, Guid locationId, CancellationToken cancellationToken)
        => dbContext.Locations.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == locationId, cancellationToken);

    public async Task<IReadOnlyCollection<Location>> GetLocationsAsync(Guid tenantId, CancellationToken cancellationToken)
        => await dbContext.Locations.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<PrintAgent>> GetAgentsAsync(Guid tenantId, CancellationToken cancellationToken)
        => await dbContext.PrintAgents.AsNoTracking().Include(x => x.Location).Include(x => x.Printers)
            .Where(x => x.TenantId == tenantId).OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<PrintAgent?> GetAgentAsync(Guid tenantId, Guid agentId, CancellationToken cancellationToken)
        => dbContext.PrintAgents.AsNoTracking().Include(x => x.Location).Include(x => x.Printers)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == agentId, cancellationToken);

    public Task<PrintAgent?> GetAgentForUpdateAsync(Guid tenantId, Guid agentId, CancellationToken cancellationToken)
        => dbContext.PrintAgents.Include(x => x.Location).Include(x => x.Printers)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == agentId, cancellationToken);

    public Task<PrintAgent?> GetAgentByDeviceAsync(Guid tenantId, Guid locationId, string deviceIdentifier, CancellationToken cancellationToken)
        => dbContext.PrintAgents.IgnoreQueryFilters().FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.LocationId == locationId && x.DeviceIdentifier == deviceIdentifier,
            cancellationToken);

    public Task AddAgentAsync(PrintAgent agent, CancellationToken cancellationToken)
        => dbContext.PrintAgents.AddAsync(agent, cancellationToken).AsTask();

    public Task AddCredentialAsync(PrintAgentCredential credential, CancellationToken cancellationToken)
        => dbContext.PrintAgentCredentials.AddAsync(credential, cancellationToken).AsTask();

    public async Task RevokeCredentialsAsync(Guid tenantId, Guid agentId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        var credentials = await dbContext.PrintAgentCredentials.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.PrintAgentId == agentId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var credential in credentials) credential.RevokedAt = revokedAt;
    }

    public async Task<AuthenticatedPrintAgent?> AuthenticateAgentAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var credential = await dbContext.PrintAgentCredentials
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.PrintAgent)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAt == null && x.ExpiresAt > now, cancellationToken);
        if (credential?.PrintAgent is null) return null;
        return new AuthenticatedPrintAgent(
            credential.PrintAgentId,
            credential.TenantId,
            credential.PrintAgent.LocationId,
            credential.PrintAgent.Enabled,
            credential.ExpiresAt);
    }

    public Task AddPairingCodeAsync(PrintAgentPairingCode code, CancellationToken cancellationToken)
        => dbContext.PrintAgentPairingCodes.AddAsync(code, cancellationToken).AsTask();

    public Task<PrintAgentPairingCode?> GetPairingCodeByHashAsync(string codeHash, CancellationToken cancellationToken)
        => dbContext.PrintAgentPairingCodes.IgnoreQueryFilters().Include(x => x.Location)
            .FirstOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken);

    public async Task<bool> TryConsumePairingCodeAsync(
        Guid pairingCodeId,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            var pairing = await dbContext.PrintAgentPairingCodes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == pairingCodeId, cancellationToken);
            if (pairing is null || pairing.ConsumedAt.HasValue || pairing.ExpiresAt <= consumedAt) return false;
            pairing.ConsumedAt = consumedAt;
            return true;
        }

        return await dbContext.PrintAgentPairingCodes.IgnoreQueryFilters()
            .Where(x => x.Id == pairingCodeId && x.ConsumedAt == null && x.ExpiresAt > consumedAt)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ConsumedAt, consumedAt), cancellationToken) == 1;
    }

    public async Task<IReadOnlyCollection<Printer>> GetPrintersAsync(Guid tenantId, Guid? agentId, CancellationToken cancellationToken)
    {
        var query = dbContext.Printers.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (agentId.HasValue) query = query.Where(x => x.PrintAgentId == agentId.Value);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PrintAgentDiscoveredPrinter>> GetDiscoveredPrintersAsync(
        Guid tenantId,
        Guid agentId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PrintAgentDiscoveredPrinters
            .Where(x => x.TenantId == tenantId && x.PrintAgentId == agentId);
        if (!tracking) query = query.AsNoTracking();
        return await query.OrderByDescending(x => x.IsAvailable)
            .ThenByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddDiscoveredPrinterAsync(PrintAgentDiscoveredPrinter printer, CancellationToken cancellationToken)
        => dbContext.PrintAgentDiscoveredPrinters.AddAsync(printer, cancellationToken).AsTask();

    public Task<Printer?> GetPrinterAsync(Guid tenantId, Guid printerId, CancellationToken cancellationToken)
        => dbContext.Printers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == printerId, cancellationToken);

    public Task AddPrinterAsync(Printer printer, CancellationToken cancellationToken)
        => dbContext.Printers.AddAsync(printer, cancellationToken).AsTask();

    public Task<bool> PrinterIsInUseAsync(Guid tenantId, Guid printerId, CancellationToken cancellationToken)
        => dbContext.PrintingZonePrinters.AnyAsync(x => x.TenantId == tenantId && x.PrinterId == printerId, cancellationToken);

    public void RemovePrinter(Printer printer) => dbContext.Printers.Remove(printer);

    public async Task<IReadOnlyCollection<PrintingZone>> GetZonesAsync(Guid tenantId, CancellationToken cancellationToken)
        => await ZoneQuery().AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<PrintingZone?> GetZoneAsync(Guid tenantId, Guid zoneId, CancellationToken cancellationToken)
        => ZoneQuery().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == zoneId, cancellationToken);

    public Task<bool> ZoneNameExistsAsync(Guid tenantId, Guid locationId, string normalizedName, Guid? excludedId, CancellationToken cancellationToken)
        => dbContext.PrintingZones.AnyAsync(
            x => x.TenantId == tenantId && x.LocationId == locationId && x.NormalizedName == normalizedName && (!excludedId.HasValue || x.Id != excludedId.Value),
            cancellationToken);

    public Task AddZoneAsync(PrintingZone zone, CancellationToken cancellationToken)
        => dbContext.PrintingZones.AddAsync(zone, cancellationToken).AsTask();

    public async Task ReplaceZoneLinksAsync(
        Guid tenantId,
        Guid zoneId,
        IReadOnlyCollection<Guid> printerIds,
        IReadOnlyCollection<Guid> categoryIds,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var printerLinks = await dbContext.PrintingZonePrinters.Where(x => x.TenantId == tenantId && x.PrintingZoneId == zoneId).ToListAsync(cancellationToken);
        var categoryLinks = await dbContext.CategoryPrintingRoutes.Where(x => x.TenantId == tenantId && x.PrintingZoneId == zoneId).ToListAsync(cancellationToken);
        var productLinks = await dbContext.ProductPrintingRoutes.Where(x => x.TenantId == tenantId && x.PrintingZoneId == zoneId).ToListAsync(cancellationToken);
        dbContext.RemoveRange(printerLinks);
        dbContext.RemoveRange(categoryLinks);
        dbContext.RemoveRange(productLinks);

        await dbContext.PrintingZonePrinters.AddRangeAsync(
            printerIds.Distinct().Select(id => new PrintingZonePrinter { TenantId = tenantId, PrintingZoneId = zoneId, PrinterId = id }),
            cancellationToken);
        await dbContext.CategoryPrintingRoutes.AddRangeAsync(
            categoryIds.Distinct().Select(id => new CategoryPrintingRoute { TenantId = tenantId, PrintingZoneId = zoneId, CategoryId = id }),
            cancellationToken);
        await dbContext.ProductPrintingRoutes.AddRangeAsync(
            productIds.Distinct().Select(id => new ProductPrintingRoute { TenantId = tenantId, PrintingZoneId = zoneId, ProductId = id }),
            cancellationToken);
    }

    public void RemoveZone(PrintingZone zone) => dbContext.PrintingZones.Remove(zone);

    public Task<bool> ZoneHasJobsAsync(Guid tenantId, Guid zoneId, CancellationToken cancellationToken)
        => dbContext.PrintJobs.AnyAsync(x => x.TenantId == tenantId && x.PrintingZoneId == zoneId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<PrintingDestination>>> ResolveDestinationsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, IReadOnlyCollection<PrintingDestination>>();

        var products = await dbContext.Products.AsNoTracking()
            .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
            .Select(x => new { x.Id, x.CategoryId })
            .ToListAsync(cancellationToken);
        var explicitRoutes = await dbContext.ProductPrintingRoutes.AsNoTracking()
            .Where(x => x.TenantId == tenantId && ids.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.PrintingZoneId })
            .ToListAsync(cancellationToken);
        var categoryIds = products.Select(x => x.CategoryId).Distinct().ToArray();
        var categoryRoutes = await dbContext.CategoryPrintingRoutes.AsNoTracking()
            .Where(x => x.TenantId == tenantId && categoryIds.Contains(x.CategoryId))
            .Select(x => new { x.CategoryId, x.PrintingZoneId })
            .ToListAsync(cancellationToken);
        var zoneIds = explicitRoutes.Select(x => x.PrintingZoneId)
            .Concat(categoryRoutes.Select(x => x.PrintingZoneId)).Distinct().ToArray();
        var zones = await dbContext.PrintingZones.AsNoTracking()
            .Include(x => x.PrintAgent)
            .Include(x => x.PrinterLinks).ThenInclude(x => x.Printer)
            .Where(x => x.TenantId == tenantId && zoneIds.Contains(x.Id) && x.Enabled && x.PrintAgent != null && x.PrintAgent.Enabled)
            .ToListAsync(cancellationToken);
        var zoneMap = zones.ToDictionary(x => x.Id);
        var result = new Dictionary<Guid, IReadOnlyCollection<PrintingDestination>>();

        foreach (var product in products)
        {
            var selectedIds = explicitRoutes.Where(x => x.ProductId == product.Id).Select(x => x.PrintingZoneId).Distinct().ToArray();
            if (selectedIds.Length == 0)
                selectedIds = categoryRoutes.Where(x => x.CategoryId == product.CategoryId).Select(x => x.PrintingZoneId).Distinct().ToArray();

            result[product.Id] = selectedIds
                .Where(zoneMap.ContainsKey)
                .Select(zoneId => zoneMap[zoneId])
                .Select(zone => new PrintingDestination(
                    zone.LocationId,
                    zone.PrintAgentId,
                    zone.Id,
                    zone.Name,
                    zone.PrinterLinks.Where(x => x.Printer is { Enabled: true }).Select(x => x.PrinterId).Distinct().ToArray()))
                .Where(x => x.PrinterIds.Count > 0)
                .ToArray();
        }

        return result;
    }

    public Task AddJobsAsync(IEnumerable<PrintJob> jobs, CancellationToken cancellationToken)
        => dbContext.PrintJobs.AddRangeAsync(jobs, cancellationToken);

    public async Task<PageData<PrintJob>> GetJobsPageAsync(Guid tenantId, PrintJobQueryRequest request, CancellationToken cancellationToken)
    {
        var query = JobQuery().AsNoTracking().Where(x => x.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(x => x.Status == request.Status.Trim().ToUpperInvariant());
        if (request.ZoneId.HasValue) query = query.Where(x => x.PrintingZoneId == request.ZoneId.Value);
        if (request.AgentId.HasValue) query = query.Where(x => x.PrintAgentId == request.AgentId.Value);
        if (request.PrinterId.HasValue) query = query.Where(x => x.PrinterId == request.PrinterId.Value);
        if (request.FromDate.HasValue) query = query.Where(x => x.CreatedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(x => x.CreatedAt < request.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.PayloadJson.ToLower().Contains(search)
                || x.PrintAgent!.Name.ToLower().Contains(search)
                || x.Printer!.Name.ToLower().Contains(search)
                || x.PrintingZone!.Name.ToLower().Contains(search));
        }
        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new PageData<PrintJob>(items, total);
    }

    public Task<PrintJob?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken)
        => JobQuery().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == jobId, cancellationToken);

    public async Task<IReadOnlyCollection<PrintJob>> GetPendingJobsAsync(Guid tenantId, Guid agentId, int limit, CancellationToken cancellationToken)
        => await JobQuery().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PrintAgentId == agentId
                && (x.Status == PrintJobStatuses.Pending || x.Status == PrintJobStatuses.Queued || x.Status == PrintJobStatuses.Sent))
            .OrderBy(x => x.CreatedAt).Take(Math.Clamp(limit, 1, 100)).ToListAsync(cancellationToken);

    public async Task CancelOpenJobsForAgentAsync(Guid tenantId, Guid agentId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var jobs = await dbContext.PrintJobs.Where(x => x.TenantId == tenantId && x.PrintAgentId == agentId
            && x.Status != PrintJobStatuses.Printed && x.Status != PrintJobStatuses.Cancelled).ToListAsync(cancellationToken);
        foreach (var job in jobs)
        {
            job.Status = PrintJobStatuses.Cancelled;
            job.FailedAt = now;
            job.LastError = "Agent disabled or unlinked.";
        }
    }

    private IQueryable<PrintingZone> ZoneQuery() => dbContext.PrintingZones
        .Include(x => x.PrintAgent)
        .Include(x => x.PrinterLinks)
        .Include(x => x.CategoryRoutes)
        .Include(x => x.ProductRoutes);

    private IQueryable<PrintJob> JobQuery() => dbContext.PrintJobs
        .Include(x => x.Location)
        .Include(x => x.PrintAgent)
        .Include(x => x.Printer)
        .Include(x => x.PrintingZone);
}
