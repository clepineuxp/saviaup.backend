using System.Text.Json;
using Microsoft.Extensions.Options;
using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Core.Settings;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Printing;

public static class PrintingRules
{
    public static string Normalize(string value)
        => string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    public static PrintAgentDto ToDto(PrintAgent agent, DateTimeOffset now, int offlineTimeoutSeconds)
    {
        var status = !agent.Enabled
            ? PrintAgentStatuses.Disabled
            : agent.LastSeenAt.HasValue && agent.LastSeenAt.Value.AddSeconds(offlineTimeoutSeconds) >= now
                ? agent.Status == PrintAgentStatuses.Warning ? PrintAgentStatuses.Warning : PrintAgentStatuses.Online
                : PrintAgentStatuses.Offline;
        return new PrintAgentDto(
            agent.Id,
            agent.LocationId,
            agent.Location?.Name ?? string.Empty,
            agent.Name,
            agent.DeviceIdentifier,
            agent.Hostname,
            agent.OperatingSystem,
            agent.Version,
            status,
            agent.LastSeenAt,
            agent.Printers.Count,
            agent.LocalIpAddress,
            agent.Enabled,
            agent.CreatedAt,
            agent.UpdatedAt);
    }

    public static PrinterDto ToDto(Printer printer) => new(
        printer.Id, printer.LocationId, printer.PrintAgentId, printer.Name, printer.ConnectionType,
        printer.LocalPrinterName, printer.IpAddress, printer.Port, printer.PaperWidth, printer.Enabled,
        printer.CreatedAt, printer.UpdatedAt);

    public static AvailablePrinterDto ToDto(
        PrintAgentDiscoveredPrinter printer,
        IReadOnlySet<string> configuredNames) => new(
        printer.Id,
        printer.PrintAgentId,
        printer.Name,
        printer.IsDefault,
        printer.IsAvailable,
        configuredNames.Contains(printer.NormalizedName),
        printer.LastSeenAt);

    public static PrintingZoneDto ToDto(PrintingZone zone) => new(
        zone.Id,
        zone.LocationId,
        zone.PrintAgentId,
        zone.PrintAgent?.Name ?? string.Empty,
        zone.Name,
        zone.Enabled,
        zone.PrinterLinks.Select(x => x.PrinterId).ToArray(),
        zone.CategoryRoutes.Select(x => x.CategoryId).ToArray(),
        zone.ProductRoutes.Select(x => x.ProductId).ToArray(),
        zone.CreatedAt,
        zone.UpdatedAt);

    public static PrintJobDto ToDto(PrintJob job) => new(
        job.Id,
        job.LocationId,
        job.Location?.Name ?? string.Empty,
        job.PrintAgentId,
        job.PrintAgent?.Name ?? string.Empty,
        job.PrinterId,
        job.Printer?.Name ?? string.Empty,
        job.Printer?.ConnectionType ?? string.Empty,
        job.Printer?.LocalPrinterName,
        job.Printer?.IpAddress,
        job.Printer?.Port,
        job.Printer?.PaperWidth ?? 80,
        job.PrintingZoneId,
        job.PrintingZone?.Name ?? string.Empty,
        job.SourceType,
        job.SourceId,
        job.DocumentType,
        job.PayloadJson,
        job.Status,
        job.Attempts,
        job.CreatedAt,
        job.QueuedAt,
        job.SentAt,
        job.PrintingAt,
        job.PrintedAt,
        job.FailedAt,
        job.LastError,
        job.OriginalPrintJobId,
        job.IsReprint);
}

public sealed class PrintJobFactory(
    IPrintingRepository printingRepository,
    ISettingsRepository settingsRepository) : IPrintJobFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<PrintJobNotification>> CreateForOrderItemsAsync(
        Guid tenantId,
        Order order,
        IReadOnlyCollection<OrderItem> newItems,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var parameter = (await settingsRepository.GetParametersAsync(tenantId, cancellationToken))
            .FirstOrDefault(item => item.Key == SettingsDefaults.EnableOrderPrintZones);
        if (!bool.TryParse(parameter?.Value, out var enabled) || !enabled) return [];
        var productIds = newItems.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToArray();
        var routes = await printingRepository.ResolveDestinationsAsync(tenantId, productIds, cancellationToken);
        var buckets = new Dictionary<(Guid AgentId, Guid LocationId, Guid ZoneId, string ZoneName), List<OrderItem>>();

        foreach (var item in newItems.Where(x => x.ProductId.HasValue))
        {
            if (!routes.TryGetValue(item.ProductId!.Value, out var destinations)) continue;
            foreach (var destination in destinations)
            {
                var key = (destination.AgentId, destination.LocationId, destination.ZoneId, destination.ZoneName);
                if (!buckets.TryGetValue(key, out var items)) buckets[key] = items = [];
                items.Add(item);
            }
        }

        var jobs = new List<PrintJob>();
        foreach (var bucket in buckets)
        {
            var destination = routes.Values.SelectMany(x => x)
                .First(x => x.AgentId == bucket.Key.AgentId && x.ZoneId == bucket.Key.ZoneId);
            var payload = new KitchenOrderPrintPayload(
                "KitchenOrder",
                $"CMD-{order.OrderNumber:D6}",
                order.Table?.Name,
                order.LastModifiedByUserName ?? order.CreatedByUserName,
                now,
                bucket.Value.Select(x => new KitchenOrderPrintItem(x.Quantity, x.ProductName, [], x.Notes)).ToArray(),
                order.Observations,
                false);
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            foreach (var printerId in destination.PrinterIds)
            {
                jobs.Add(new PrintJob
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    LocationId = destination.LocationId,
                    PrintAgentId = destination.AgentId,
                    PrinterId = printerId,
                    PrintingZoneId = destination.ZoneId,
                    SourceType = "ORDER",
                    SourceId = order.Id,
                    DocumentType = "KITCHEN_ORDER",
                    PayloadJson = json,
                    Status = PrintJobStatuses.Pending,
                    CreatedAt = now,
                    QueuedAt = now,
                    CreatedByUserId = userId
                });
            }
        }

        if (jobs.Count > 0) await printingRepository.AddJobsAsync(jobs, cancellationToken);
        return jobs.Select(x => new PrintJobNotification(x.PrintAgentId, x.Id)).ToArray();
    }
}

public sealed class PrintingAdministrationUseCase(
    IPrintingRepository repository,
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    ITokenGenerator tokenGenerator,
    IDateTimeProvider clock,
    IPrintingRealtimeNotifier realtime,
    IUnpairedPrintAgentRegistry discoveryRegistry,
    IOrganizationTimeZone organizationTimeZone,
    ITimeZoneService timeZones,
    IOptions<PrintingOptions> configuredOptions,
    IUnitOfWork unitOfWork) : IPrintingAdministrationUseCase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private PrintingOptions Options => configuredOptions.Value;

    public async Task<Result<IReadOnlyCollection<PrintingLocationDto>>> ListLocationsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await repository.GetOrCreateDefaultLocationAsync(tenantId, clock.UtcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var locations = await repository.GetLocationsAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<PrintingLocationDto>>.Success(
            locations.Select(x => new PrintingLocationDto(x.Id, x.Name, x.IsDefault, x.IsActive)).ToArray());
    }

    public Task<Result<PrintingConfigurationDto>> GetConfigurationAsync(CancellationToken cancellationToken)
        => Task.FromResult(Result<PrintingConfigurationDto>.Success(
            new PrintingConfigurationDto(Options.AgentDownloadUrl, Options.HeartbeatIntervalSeconds)));

    public async Task<Result<PrintingRoutingOptionsDto>> GetRoutingOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.GetForTenantAsync(tenantId, false, cancellationToken);
        var products = await productRepository.GetAllForTenantAsync(tenantId, false, cancellationToken);
        return Result<PrintingRoutingOptionsDto>.Success(new PrintingRoutingOptionsDto(
            categories.OrderBy(x => x.Name).Select(x => new PrintingRouteOptionDto(x.Id, x.Name)).ToArray(),
            products.OrderBy(x => x.Name).Select(x => new PrintingRouteOptionDto(x.Id, x.Name)).ToArray()));
    }

    public async Task<Result<PairingCodeDto>> CreatePairingCodeAsync(
        Guid tenantId,
        Guid userId,
        CreatePairingCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AgentName)) return Result<PairingCodeDto>.Failure(Errors.Validation);
        var now = clock.UtcNow;
        var location = request.LocationId.HasValue
            ? await repository.GetLocationAsync(tenantId, request.LocationId.Value, cancellationToken)
            : await repository.GetOrCreateDefaultLocationAsync(tenantId, now, cancellationToken);
        if (location is null || !location.IsActive) return Result<PairingCodeDto>.Failure(Errors.PrintingLocationNotFound);
        var raw = tokenGenerator.Generate().Replace("-", string.Empty).Replace("_", string.Empty);
        var code = raw[..Math.Min(8, raw.Length)].ToUpperInvariant();
        var expiresAt = now.AddMinutes(Math.Clamp(Options.PairingCodeExpirationMinutes, 2, 60));
        await repository.AddPairingCodeAsync(new PrintAgentPairingCode
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = location.Id,
            AgentName = request.AgentName.Trim(),
            CodeHash = tokenGenerator.Hash(code),
            CreatedByUserId = userId,
            CreatedAt = now,
            ExpiresAt = expiresAt
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PairingCodeDto>.Success(new PairingCodeDto(code, location.Id, location.Name, request.AgentName.Trim(), expiresAt));
    }

    public async Task<Result<IReadOnlyCollection<DiscoveredPrintAgentDto>>> ListDiscoveredAgentsAsync(
        CancellationToken cancellationToken)
        => Result<IReadOnlyCollection<DiscoveredPrintAgentDto>>.Success(
            await discoveryRegistry.ListAsync(cancellationToken));

    public async Task<Result<PrintAgentDto>> LinkDiscoveredAgentAsync(
        Guid tenantId,
        LinkDiscoveredPrintAgentRequest request,
        CancellationToken cancellationToken)
    {
        var discovered = await discoveryRegistry.FindAsync(request.DiscoveryId, cancellationToken);
        if (discovered is null) return Result<PrintAgentDto>.Failure(Errors.PrintAgentPairingInvalid);

        var now = clock.UtcNow;
        var location = request.LocationId.HasValue
            ? await repository.GetLocationAsync(tenantId, request.LocationId.Value, cancellationToken)
            : await repository.GetOrCreateDefaultLocationAsync(tenantId, now, cancellationToken);
        if (location is null || !location.IsActive) return Result<PrintAgentDto>.Failure(Errors.PrintingLocationNotFound);

        var name = string.IsNullOrWhiteSpace(request.AgentName) ? discovered.Hostname : request.AgentName.Trim();
        var result = await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var agent = await repository.GetAgentByDeviceAsync(
                tenantId, location.Id, discovered.DeviceIdentifier, transactionToken);
            if (agent is null)
            {
                agent = new PrintAgent
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, LocationId = location.Id, Name = name,
                    DeviceIdentifier = discovered.DeviceIdentifier, Hostname = discovered.Hostname,
                    OperatingSystem = discovered.OperatingSystem, Version = discovered.Version,
                    LocalIpAddress = Clean(discovered.LocalIpAddress), Status = PrintAgentStatuses.Online,
                    LastSeenAt = now, CreatedAt = now, UpdatedAt = now, Enabled = true
                };
                await repository.AddAgentAsync(agent, transactionToken);
            }
            else
            {
                agent.Name = name;
                agent.Hostname = discovered.Hostname;
                agent.OperatingSystem = discovered.OperatingSystem;
                agent.Version = discovered.Version;
                agent.LocalIpAddress = Clean(discovered.LocalIpAddress);
                agent.Status = PrintAgentStatuses.Online;
                agent.LastSeenAt = now;
                agent.UpdatedAt = now;
                agent.Enabled = true;
                await repository.RevokeCredentialsAsync(tenantId, agent.Id, now, transactionToken);
            }

            var rawToken = tokenGenerator.Generate();
            var expiresAt = now.AddDays(Math.Clamp(Options.DeviceTokenExpirationDays, 1, 730));
            await repository.AddCredentialAsync(new PrintAgentCredential
            {
                Id = Guid.NewGuid(), TenantId = tenantId, PrintAgentId = agent.Id,
                TokenHash = tokenGenerator.Hash(rawToken), CreatedAt = now, ExpiresAt = expiresAt
            }, transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            return Result<(PrintAgent Agent, PairPrintAgentResponse Response)>.Success((agent,
                new PairPrintAgentResponse(agent.Id, agent.LocationId, rawToken, expiresAt,
                    Math.Clamp(Options.HeartbeatIntervalSeconds, 10, 300), "/hubs/printing")));
        }, cancellationToken);
        if (!result.IsSuccess) return Result<PrintAgentDto>.Failure(result.Error!);
        if (!await discoveryRegistry.DeliverPairingAsync(request.DiscoveryId, result.Value!.Response, cancellationToken))
            return Result<PrintAgentDto>.Failure(Errors.PrintAgentPairingInvalid);
        return Result<PrintAgentDto>.Success(PrintingRules.ToDto(
            result.Value!.Agent, now, Options.OfflineTimeoutSeconds));
    }

    public async Task<Result<IReadOnlyCollection<PrintAgentDto>>> ListAgentsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var agents = await repository.GetAgentsAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<PrintAgentDto>>.Success(agents.Select(x => PrintingRules.ToDto(x, now, Options.OfflineTimeoutSeconds)).ToArray());
    }

    public async Task<Result<PrintAgentDto>> GetAgentAsync(Guid tenantId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await repository.GetAgentAsync(tenantId, agentId, cancellationToken);
        return agent is null
            ? Result<PrintAgentDto>.Failure(Errors.PrintAgentNotFound)
            : Result<PrintAgentDto>.Success(PrintingRules.ToDto(agent, clock.UtcNow, Options.OfflineTimeoutSeconds));
    }

    public async Task<Result<PrintAgentDto>> UpdateAgentAsync(Guid tenantId, Guid agentId, UpdatePrintAgentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<PrintAgentDto>.Failure(Errors.Validation);
        var agent = await repository.GetAgentForUpdateAsync(tenantId, agentId, cancellationToken);
        if (agent is null) return Result<PrintAgentDto>.Failure(Errors.PrintAgentNotFound);
        agent.Name = request.Name.Trim();
        agent.Enabled = request.Enabled;
        agent.Status = request.Enabled ? PrintAgentStatuses.Offline : PrintAgentStatuses.Disabled;
        agent.UpdatedAt = clock.UtcNow;
        if (!request.Enabled)
        {
            await repository.RevokeCredentialsAsync(tenantId, agentId, agent.UpdatedAt, cancellationToken);
            await repository.CancelOpenJobsForAgentAsync(tenantId, agentId, agent.UpdatedAt, cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PrintAgentDto>.Success(PrintingRules.ToDto(agent, clock.UtcNow, Options.OfflineTimeoutSeconds));
    }

    public async Task<Result> DeleteAgentAsync(Guid tenantId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await repository.GetAgentForUpdateAsync(tenantId, agentId, cancellationToken);
        if (agent is null) return Result.Failure(Errors.PrintAgentNotFound);
        var now = clock.UtcNow;
        agent.Enabled = false;
        agent.Status = PrintAgentStatuses.Disabled;
        agent.UpdatedAt = now;
        await repository.RevokeCredentialsAsync(tenantId, agentId, now, cancellationToken);
        await repository.CancelOpenJobsForAgentAsync(tenantId, agentId, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyCollection<PrinterDto>>> ListPrintersAsync(Guid tenantId, Guid? agentId, CancellationToken cancellationToken)
    {
        var printers = await repository.GetPrintersAsync(tenantId, agentId, cancellationToken);
        return Result<IReadOnlyCollection<PrinterDto>>.Success(printers.Select(PrintingRules.ToDto).ToArray());
    }

    public async Task<Result<IReadOnlyCollection<AvailablePrinterDto>>> ListAvailablePrintersAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var agent = await repository.GetAgentAsync(tenantId, agentId, cancellationToken);
        if (agent is null) return Result<IReadOnlyCollection<AvailablePrinterDto>>.Failure(Errors.PrintAgentNotFound);
        var discovered = await repository.GetDiscoveredPrintersAsync(tenantId, agentId, false, cancellationToken);
        var configured = await repository.GetPrintersAsync(tenantId, agentId, cancellationToken);
        var configuredNames = configured
            .Where(x => x.ConnectionType == PrinterConnectionTypes.WindowsSpooler && !string.IsNullOrWhiteSpace(x.LocalPrinterName))
            .Select(x => PrintingRules.Normalize(x.LocalPrinterName!))
            .ToHashSet(StringComparer.Ordinal);
        return Result<IReadOnlyCollection<AvailablePrinterDto>>.Success(
            discovered.Select(x => PrintingRules.ToDto(x, configuredNames)).ToArray());
    }

    public async Task<Result> RequestPrinterDiscoveryAsync(Guid tenantId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await repository.GetAgentAsync(tenantId, agentId, cancellationToken);
        if (agent is null || !agent.Enabled) return Result.Failure(Errors.PrintAgentNotFound);
        await realtime.PrinterDiscoveryRequestedAsync(agentId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PrinterDto>> CreatePrinterAsync(Guid tenantId, SavePrinterRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidatePrinterAsync(tenantId, request, cancellationToken);
        if (!validation.IsSuccess) return Result<PrinterDto>.Failure(validation.Error!);
        var now = clock.UtcNow;
        var agent = validation.Value!;
        var printer = new Printer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = agent.LocationId,
            PrintAgentId = agent.Id,
            Name = request.Name.Trim(),
            ConnectionType = request.ConnectionType.Trim().ToUpperInvariant(),
            LocalPrinterName = Clean(request.LocalPrinterName),
            IpAddress = Clean(request.IpAddress),
            Port = request.Port,
            PaperWidth = request.PaperWidth,
            Enabled = request.Enabled,
            CreatedAt = now,
            UpdatedAt = now
        };
        await repository.AddPrinterAsync(printer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PrinterDto>.Success(PrintingRules.ToDto(printer));
    }

    public async Task<Result<PrinterDto>> UpdatePrinterAsync(Guid tenantId, Guid printerId, SavePrinterRequest request, CancellationToken cancellationToken)
    {
        var printer = await repository.GetPrinterAsync(tenantId, printerId, cancellationToken);
        if (printer is null) return Result<PrinterDto>.Failure(Errors.PrinterNotFound);
        var validation = await ValidatePrinterAsync(tenantId, request, cancellationToken);
        if (!validation.IsSuccess) return Result<PrinterDto>.Failure(validation.Error!);
        printer.LocationId = validation.Value!.LocationId;
        printer.PrintAgentId = request.PrintAgentId;
        printer.Name = request.Name.Trim();
        printer.ConnectionType = request.ConnectionType.Trim().ToUpperInvariant();
        printer.LocalPrinterName = Clean(request.LocalPrinterName);
        printer.IpAddress = Clean(request.IpAddress);
        printer.Port = request.Port;
        printer.PaperWidth = request.PaperWidth;
        printer.Enabled = request.Enabled;
        printer.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PrinterDto>.Success(PrintingRules.ToDto(printer));
    }

    public async Task<Result> DeletePrinterAsync(Guid tenantId, Guid printerId, CancellationToken cancellationToken)
    {
        var printer = await repository.GetPrinterAsync(tenantId, printerId, cancellationToken);
        if (printer is null) return Result.Failure(Errors.PrinterNotFound);
        if (await repository.PrinterIsInUseAsync(tenantId, printerId, cancellationToken))
        {
            printer.Enabled = false;
            printer.UpdatedAt = clock.UtcNow;
        }
        else repository.RemovePrinter(printer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyCollection<PrintingZoneDto>>> ListZonesAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var zones = await repository.GetZonesAsync(tenantId, cancellationToken);
        return Result<IReadOnlyCollection<PrintingZoneDto>>.Success(zones.Select(PrintingRules.ToDto).ToArray());
    }

    public async Task<Result<PrintingZoneDto>> CreateZoneAsync(Guid tenantId, SavePrintingZoneRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateZoneAsync(tenantId, null, request, cancellationToken);
        if (!validation.IsSuccess) return Result<PrintingZoneDto>.Failure(validation.Error!);
        var (agent, location) = validation.Value!;
        var now = clock.UtcNow;
        var zone = new PrintingZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = location.Id,
            PrintAgentId = agent.Id,
            Name = request.Name.Trim(),
            NormalizedName = PrintingRules.Normalize(request.Name),
            Enabled = request.Enabled,
            CreatedAt = now,
            UpdatedAt = now
        };
        await repository.AddZoneAsync(zone, cancellationToken);
        await repository.ReplaceZoneLinksAsync(tenantId, zone.Id, request.PrinterIds, request.CategoryIds, request.ProductIds, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var saved = await repository.GetZoneAsync(tenantId, zone.Id, cancellationToken);
        return Result<PrintingZoneDto>.Success(PrintingRules.ToDto(saved!));
    }

    public async Task<Result<PrintingZoneDto>> UpdateZoneAsync(Guid tenantId, Guid zoneId, SavePrintingZoneRequest request, CancellationToken cancellationToken)
    {
        var zone = await repository.GetZoneAsync(tenantId, zoneId, cancellationToken);
        if (zone is null) return Result<PrintingZoneDto>.Failure(Errors.PrintingZoneNotFound);
        var validation = await ValidateZoneAsync(tenantId, zoneId, request, cancellationToken);
        if (!validation.IsSuccess) return Result<PrintingZoneDto>.Failure(validation.Error!);
        var (agent, location) = validation.Value!;
        zone.LocationId = location.Id;
        zone.PrintAgentId = agent.Id;
        zone.Name = request.Name.Trim();
        zone.NormalizedName = PrintingRules.Normalize(request.Name);
        zone.Enabled = request.Enabled;
        zone.UpdatedAt = clock.UtcNow;
        await repository.ReplaceZoneLinksAsync(tenantId, zone.Id, request.PrinterIds, request.CategoryIds, request.ProductIds, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var saved = await repository.GetZoneAsync(tenantId, zone.Id, cancellationToken);
        return Result<PrintingZoneDto>.Success(PrintingRules.ToDto(saved!));
    }

    public async Task<Result> DeleteZoneAsync(Guid tenantId, Guid zoneId, CancellationToken cancellationToken)
    {
        var zone = await repository.GetZoneAsync(tenantId, zoneId, cancellationToken);
        if (zone is null) return Result.Failure(Errors.PrintingZoneNotFound);
        if (await repository.ZoneHasJobsAsync(tenantId, zoneId, cancellationToken))
        {
            zone.Enabled = false;
            zone.UpdatedAt = clock.UtcNow;
        }
        else repository.RemoveZone(zone);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PrintJobPageDto>> ListJobsAsync(Guid tenantId, PrintJobQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100 || request.FromDate > request.ToDate
            || request.FromLocalDate > request.ToLocalDate || request.ToLocalDate == DateOnly.MaxValue)
            return Result<PrintJobPageDto>.Failure(Errors.Validation);
        var zone = await organizationTimeZone.GetAsync(tenantId, cancellationToken);
        var normalized = request with
        {
            FromDate = request.FromLocalDate is { } from ? timeZones.StartOfDayUtc(from, zone) : request.FromDate,
            ToDate = request.ToLocalDate is { } to ? timeZones.StartOfDayUtc(to.AddDays(1), zone) : request.ToDate
        };
        var page = await repository.GetJobsPageAsync(tenantId, normalized, cancellationToken);
        var pages = (int)Math.Ceiling(page.TotalCount / (double)request.PageSize);
        return Result<PrintJobPageDto>.Success(new PrintJobPageDto(page.Items.Select(PrintingRules.ToDto).ToArray(), request.Page, request.PageSize, page.TotalCount, pages));
    }

    public async Task<Result<PrintJobDto>> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        var job = await repository.GetJobAsync(tenantId, jobId, cancellationToken);
        return job is null ? Result<PrintJobDto>.Failure(Errors.PrintJobNotFound) : Result<PrintJobDto>.Success(PrintingRules.ToDto(job));
    }

    public async Task<Result<PrintJobDto>> RetryJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        var job = await repository.GetJobAsync(tenantId, jobId, cancellationToken);
        if (job is null) return Result<PrintJobDto>.Failure(Errors.PrintJobNotFound);
        if (job.Status != PrintJobStatuses.Failed) return Result<PrintJobDto>.Failure(Errors.PrintJobInvalidStatus);
        job.Status = PrintJobStatuses.Pending;
        job.QueuedAt = clock.UtcNow;
        job.SentAt = null;
        job.PrintingAt = null;
        job.FailedAt = null;
        job.LastError = null;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await realtime.JobAvailableAsync(job.PrintAgentId, job.Id, cancellationToken);
        return Result<PrintJobDto>.Success(PrintingRules.ToDto(job));
    }

    public async Task<Result<PrintJobDto>> ReprintJobAsync(Guid tenantId, Guid jobId, Guid userId, CancellationToken cancellationToken)
    {
        var original = await repository.GetJobAsync(tenantId, jobId, cancellationToken);
        if (original is null) return Result<PrintJobDto>.Failure(Errors.PrintJobNotFound);
        var now = clock.UtcNow;
        var payload = JsonSerializer.Deserialize<KitchenOrderPrintPayload>(original.PayloadJson, JsonOptions);
        var job = new PrintJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = original.LocationId,
            PrintAgentId = original.PrintAgentId,
            PrinterId = original.PrinterId,
            PrintingZoneId = original.PrintingZoneId,
            SourceType = original.SourceType,
            SourceId = original.SourceId,
            DocumentType = original.DocumentType,
            PayloadJson = payload is null ? original.PayloadJson : JsonSerializer.Serialize(payload with { IsReprint = true }, JsonOptions),
            Status = PrintJobStatuses.Pending,
            CreatedAt = now,
            QueuedAt = now,
            OriginalPrintJobId = original.Id,
            IsReprint = true,
            CreatedByUserId = userId,
            ReprintRequestedByUserId = userId,
            ReprintRequestedAt = now,
            Location = original.Location,
            PrintAgent = original.PrintAgent,
            Printer = original.Printer,
            PrintingZone = original.PrintingZone
        };
        await repository.AddJobsAsync([job], cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await realtime.JobAvailableAsync(job.PrintAgentId, job.Id, cancellationToken);
        return Result<PrintJobDto>.Success(PrintingRules.ToDto(job));
    }

    public async Task<Result<PrintJobDto>> CreateTestJobAsync(Guid tenantId, Guid agentId, Guid printerId, Guid userId, CancellationToken cancellationToken)
    {
        var agent = await repository.GetAgentAsync(tenantId, agentId, cancellationToken);
        var printer = await repository.GetPrinterAsync(tenantId, printerId, cancellationToken);
        if (agent is null) return Result<PrintJobDto>.Failure(Errors.PrintAgentNotFound);
        if (printer is null || printer.PrintAgentId != agentId) return Result<PrintJobDto>.Failure(Errors.PrinterNotFound);
        var zone = (await repository.GetZonesAsync(tenantId, cancellationToken)).FirstOrDefault(x => x.PrintAgentId == agentId && x.PrinterLinks.Any(link => link.PrinterId == printerId));
        if (zone is null) return Result<PrintJobDto>.Failure(Errors.PrintingZoneNotFound);
        var now = clock.UtcNow;
        var payload = new KitchenOrderPrintPayload("TestPage", "PRUEBA", null, "Savia Up", now,
            [new KitchenOrderPrintItem(1, "Impresión de prueba", [], "Conexión configurada correctamente")], null, false);
        var job = new PrintJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationId = agent.LocationId,
            PrintAgentId = agentId,
            PrinterId = printerId,
            PrintingZoneId = zone.Id,
            SourceType = "TEST",
            SourceId = Guid.NewGuid(),
            DocumentType = "TEST_PAGE",
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            Status = PrintJobStatuses.Pending,
            CreatedAt = now,
            QueuedAt = now,
            CreatedByUserId = userId,
            Location = agent.Location,
            PrintAgent = agent,
            Printer = printer,
            PrintingZone = zone
        };
        await repository.AddJobsAsync([job], cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await realtime.JobAvailableAsync(agentId, job.Id, cancellationToken);
        return Result<PrintJobDto>.Success(PrintingRules.ToDto(job));
    }

    private async Task<Result<PrintAgent>> ValidatePrinterAsync(Guid tenantId, SavePrinterRequest request, CancellationToken cancellationToken)
    {
        var type = request.ConnectionType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Name) || !PrinterConnectionTypes.IsValid(type) || request.PaperWidth is not (58 or 80))
            return Result<PrintAgent>.Failure(Errors.Validation);
        if (type == PrinterConnectionTypes.WindowsSpooler && string.IsNullOrWhiteSpace(request.LocalPrinterName))
            return Result<PrintAgent>.Failure(Errors.Validation);
        if (type is PrinterConnectionTypes.Network or PrinterConnectionTypes.EscPosNetwork
            && (string.IsNullOrWhiteSpace(request.IpAddress) || !request.Port.HasValue))
            return Result<PrintAgent>.Failure(Errors.Validation);
        var agent = await repository.GetAgentAsync(tenantId, request.PrintAgentId, cancellationToken);
        return agent is null ? Result<PrintAgent>.Failure(Errors.PrintAgentNotFound) : Result<PrintAgent>.Success(agent);
    }

    private async Task<Result<(PrintAgent Agent, Location Location)>> ValidateZoneAsync(
        Guid tenantId, Guid? zoneId, SavePrintingZoneRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.PrinterIds.Count == 0)
            return Result<(PrintAgent, Location)>.Failure(Errors.Validation);
        var agent = await repository.GetAgentAsync(tenantId, request.PrintAgentId, cancellationToken);
        if (agent is null || !agent.Enabled) return Result<(PrintAgent, Location)>.Failure(Errors.PrintAgentNotFound);
        var locationId = request.LocationId ?? agent.LocationId;
        var location = await repository.GetLocationAsync(tenantId, locationId, cancellationToken);
        if (location is null || agent.LocationId != location.Id) return Result<(PrintAgent, Location)>.Failure(Errors.PrintingLocationNotFound);
        if (await repository.ZoneNameExistsAsync(tenantId, location.Id, PrintingRules.Normalize(request.Name), zoneId, cancellationToken))
            return Result<(PrintAgent, Location)>.Failure(Errors.PrintingZoneAlreadyExists);
        var printers = await repository.GetPrintersAsync(tenantId, agent.Id, cancellationToken);
        if (request.PrinterIds.Distinct().Any(id => printers.All(x => x.Id != id || !x.Enabled)))
            return Result<(PrintAgent, Location)>.Failure(Errors.PrinterNotFound);
        foreach (var categoryId in request.CategoryIds.Distinct())
            if (await categoryRepository.GetByIdAsNoTrackingAsync(tenantId, categoryId, cancellationToken) is null)
                return Result<(PrintAgent, Location)>.Failure(Errors.CategoryNotFound);
        foreach (var productId in request.ProductIds.Distinct())
            if (await productRepository.GetByIdAsync(tenantId, productId, cancellationToken) is null)
                return Result<(PrintAgent, Location)>.Failure(Errors.ProductNotFound);
        return Result<(PrintAgent, Location)>.Success((agent, location));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class PrintAgentUseCase(
    IPrintingRepository repository,
    ITokenGenerator tokenGenerator,
    IDateTimeProvider clock,
    IOptions<PrintingOptions> configuredOptions,
    IUnitOfWork unitOfWork) : IPrintAgentUseCase
{
    private PrintingOptions Options => configuredOptions.Value;

    public async Task<Result<PairPrintAgentResponse>> PairAsync(PairPrintAgentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PairingCode) || string.IsNullOrWhiteSpace(request.DeviceIdentifier)
            || string.IsNullOrWhiteSpace(request.Hostname) || string.IsNullOrWhiteSpace(request.OperatingSystem)
            || string.IsNullOrWhiteSpace(request.Version))
            return Result<PairPrintAgentResponse>.Failure(Errors.Validation);
        var now = clock.UtcNow;
        var pairing = await repository.GetPairingCodeByHashAsync(tokenGenerator.Hash(request.PairingCode.Trim().ToUpperInvariant()), cancellationToken);
        if (pairing is null || pairing.ConsumedAt.HasValue || pairing.ExpiresAt <= now || pairing.Location is null || !pairing.Location.IsActive)
            return Result<PairPrintAgentResponse>.Failure(Errors.PrintAgentPairingInvalid);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            if (!await repository.TryConsumePairingCodeAsync(pairing.Id, now, transactionToken))
                return Result<PairPrintAgentResponse>.Failure(Errors.PrintAgentPairingInvalid);

            var agent = await repository.GetAgentByDeviceAsync(pairing.TenantId, pairing.LocationId, request.DeviceIdentifier.Trim(), transactionToken);
            if (agent is null)
            {
                agent = new PrintAgent
                {
                    Id = Guid.NewGuid(),
                    TenantId = pairing.TenantId,
                    LocationId = pairing.LocationId,
                    Name = pairing.AgentName,
                    DeviceIdentifier = request.DeviceIdentifier.Trim(),
                    Hostname = request.Hostname.Trim(),
                    OperatingSystem = request.OperatingSystem.Trim(),
                    Version = request.Version.Trim(),
                    LocalIpAddress = Clean(request.LocalIpAddress),
                    Status = PrintAgentStatuses.Online,
                    LastSeenAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Enabled = true
                };
                await repository.AddAgentAsync(agent, transactionToken);
            }
            else
            {
                agent.Name = pairing.AgentName;
                agent.Hostname = request.Hostname.Trim();
                agent.OperatingSystem = request.OperatingSystem.Trim();
                agent.Version = request.Version.Trim();
                agent.LocalIpAddress = Clean(request.LocalIpAddress);
                agent.Status = PrintAgentStatuses.Online;
                agent.LastSeenAt = now;
                agent.UpdatedAt = now;
                agent.Enabled = true;
                await repository.RevokeCredentialsAsync(pairing.TenantId, agent.Id, now, transactionToken);
            }

            var rawToken = tokenGenerator.Generate();
            var tokenExpiresAt = now.AddDays(Math.Clamp(Options.DeviceTokenExpirationDays, 1, 730));
            await repository.AddCredentialAsync(new PrintAgentCredential
            {
                Id = Guid.NewGuid(),
                TenantId = pairing.TenantId,
                PrintAgentId = agent.Id,
                TokenHash = tokenGenerator.Hash(rawToken),
                CreatedAt = now,
                ExpiresAt = tokenExpiresAt
            }, transactionToken);
            pairing.ConsumedAt = now;
            await unitOfWork.SaveChangesAsync(transactionToken);
            return Result<PairPrintAgentResponse>.Success(new PairPrintAgentResponse(
                agent.Id, agent.LocationId, rawToken, tokenExpiresAt,
                Math.Clamp(Options.HeartbeatIntervalSeconds, 10, 300), "/hubs/printing"));
        }, cancellationToken);
    }

    public async Task<Result> HeartbeatAsync(Guid tenantId, Guid agentId, PrintAgentHeartbeatRequest request, CancellationToken cancellationToken)
    {
        var agent = await repository.GetAgentForUpdateAsync(tenantId, agentId, cancellationToken);
        if (agent is null || !agent.Enabled) return Result.Failure(Errors.PrintAgentCredentialInvalid);
        var now = clock.UtcNow;
        var version = request.Version.Trim();
        var localIpAddress = Clean(request.LocalIpAddress);
        var status = string.Equals(request.Status, PrintAgentStatuses.Warning, StringComparison.OrdinalIgnoreCase)
            ? PrintAgentStatuses.Warning : PrintAgentStatuses.Online;
        var minimumPersistenceInterval = TimeSpan.FromSeconds(Math.Clamp(Options.HeartbeatPersistenceSeconds, 5, 300));
        if (agent.LastSeenAt.HasValue
            && agent.LastSeenAt.Value.Add(minimumPersistenceInterval) > now
            && agent.Version == version
            && agent.LocalIpAddress == localIpAddress
            && agent.Status == status)
            return Result.Success();
        agent.Version = version;
        agent.LocalIpAddress = localIpAddress;
        agent.Status = status;
        agent.LastSeenAt = now;
        agent.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyCollection<PrintJobDto>>> GetPendingJobsAsync(Guid tenantId, Guid agentId, int limit, CancellationToken cancellationToken)
    {
        var jobs = await repository.GetPendingJobsAsync(tenantId, agentId, limit, cancellationToken);
        return Result<IReadOnlyCollection<PrintJobDto>>.Success(jobs.Select(PrintingRules.ToDto).ToArray());
    }

    public async Task<Result<IReadOnlyCollection<AvailablePrinterDto>>> SyncPrintersAsync(
        Guid tenantId, Guid agentId, SyncDiscoveredPrintersRequest request, CancellationToken cancellationToken)
    {
        var agent = await repository.GetAgentForUpdateAsync(tenantId, agentId, cancellationToken);
        if (agent is null || !agent.Enabled) return Result<IReadOnlyCollection<AvailablePrinterDto>>.Failure(Errors.PrintAgentCredentialInvalid);
        if (request.Printers is null || request.Printers.Count > 500)
            return Result<IReadOnlyCollection<AvailablePrinterDto>>.Failure(Errors.Validation);
        var existing = (await repository.GetDiscoveredPrintersAsync(tenantId, agentId, true, cancellationToken)).ToList();
        var now = clock.UtcNow;
        foreach (var printer in existing) printer.IsAvailable = false;
        foreach (var discovered in request.Printers
                     .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                     .DistinctBy(x => PrintingRules.Normalize(x.Name), StringComparer.Ordinal))
        {
            var normalizedName = PrintingRules.Normalize(discovered.Name);
            var printer = existing.FirstOrDefault(x => x.NormalizedName == normalizedName);
            if (printer is null)
            {
                printer = new PrintAgentDiscoveredPrinter
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    LocationId = agent.LocationId,
                    PrintAgentId = agentId,
                    Name = discovered.Name.Trim(),
                    NormalizedName = normalizedName
                };
                await repository.AddDiscoveredPrinterAsync(printer, cancellationToken);
                existing.Add(printer);
            }
            printer.Name = discovered.Name.Trim();
            printer.IsDefault = discovered.IsDefault;
            printer.IsAvailable = true;
            printer.LastSeenAt = now;
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var configuredNames = (await repository.GetPrintersAsync(tenantId, agentId, cancellationToken))
            .Where(x => x.ConnectionType == PrinterConnectionTypes.WindowsSpooler && !string.IsNullOrWhiteSpace(x.LocalPrinterName))
            .Select(x => PrintingRules.Normalize(x.LocalPrinterName!))
            .ToHashSet(StringComparer.Ordinal);
        return Result<IReadOnlyCollection<AvailablePrinterDto>>.Success(
            existing.OrderByDescending(x => x.IsAvailable)
                .ThenByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .Select(x => PrintingRules.ToDto(x, configuredNames))
                .ToArray());
    }

    public async Task<Result<PrintJobDto>> UpdateJobStatusAsync(
        Guid tenantId, Guid agentId, Guid jobId, PrintJobStatusRequest request, CancellationToken cancellationToken)
    {
        var status = request.Status?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!PrintJobStatuses.IsAgentStatus(status)) return Result<PrintJobDto>.Failure(Errors.Validation);
        var job = await repository.GetJobAsync(tenantId, jobId, cancellationToken);
        if (job is null || job.PrintAgentId != agentId) return Result<PrintJobDto>.Failure(Errors.PrintJobNotFound);
        if (job.Status == PrintJobStatuses.Printed) return Result<PrintJobDto>.Success(PrintingRules.ToDto(job));
        if (job.Status == PrintJobStatuses.Cancelled) return Result<PrintJobDto>.Failure(Errors.PrintJobInvalidStatus);
        var now = clock.UtcNow;
        job.Status = status;
        job.SentAt ??= now;
        if (status == PrintJobStatuses.Processing)
        {
            job.PrintingAt ??= now;
            job.Attempts++;
        }
        else if (status == PrintJobStatuses.Printed)
        {
            if (!job.PrintingAt.HasValue)
            {
                job.PrintingAt = now;
                job.Attempts++;
            }
            job.PrintedAt = now;
            job.FailedAt = null;
            job.LastError = null;
        }
        else
        {
            job.FailedAt = now;
            job.LastError = string.IsNullOrWhiteSpace(request.Error) ? "Printing failed." : request.Error.Trim();
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PrintJobDto>.Success(PrintingRules.ToDto(job));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
