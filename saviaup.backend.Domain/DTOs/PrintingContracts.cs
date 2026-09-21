using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record PrintingLocationDto(Guid Id, string Name, bool IsDefault, bool IsActive);

public sealed record PrintAgentDto(
    Guid Id,
    Guid LocationId,
    string LocationName,
    string Name,
    string DeviceIdentifier,
    string Hostname,
    string OperatingSystem,
    string Version,
    string Status,
    DateTimeOffset? LastSeenAt,
    int PrinterCount,
    string? LocalIpAddress,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreatePairingCodeRequest(
    Guid? LocationId,
    [Required, MaxLength(120)] string AgentName);

public sealed record PairingCodeDto(
    string Code,
    Guid LocationId,
    string LocationName,
    string AgentName,
    DateTimeOffset ExpiresAt);

public sealed record PairPrintAgentRequest(
    [Required, MinLength(6), MaxLength(64)] string PairingCode,
    [Required, MaxLength(200)] string DeviceIdentifier,
    [Required, MaxLength(200)] string Hostname,
    [Required, MaxLength(200)] string OperatingSystem,
    [Required, MaxLength(50)] string Version,
    [MaxLength(64)] string? LocalIpAddress);

public sealed record PairPrintAgentResponse(
    Guid AgentId,
    Guid LocationId,
    string DeviceToken,
    DateTimeOffset TokenExpiresAt,
    int HeartbeatIntervalSeconds,
    string PrintingHubPath);

// An agent publishes this metadata before it owns a tenant credential.  It is transient:
// the backend keeps it only while the SignalR connection is alive.
public sealed record DiscoverPrintAgentRequest(
    [Required, MaxLength(200)] string DeviceIdentifier,
    [Required, MaxLength(200)] string Hostname,
    [Required, MaxLength(200)] string OperatingSystem,
    [Required, MaxLength(50)] string Version,
    [MaxLength(64)] string? LocalIpAddress);

public sealed record DiscoveredPrintAgentDto(
    Guid DiscoveryId,
    string DeviceIdentifier,
    string Hostname,
    string OperatingSystem,
    string Version,
    string? LocalIpAddress,
    DateTimeOffset ConnectedAt);

public sealed record LinkDiscoveredPrintAgentRequest(
    [Required] Guid DiscoveryId,
    Guid? LocationId,
    [MaxLength(120)] string? AgentName);

public sealed record UpdatePrintAgentRequest(
    [Required, MaxLength(120)] string Name,
    bool Enabled);

public sealed record PrinterDto(
    Guid Id,
    Guid LocationId,
    Guid PrintAgentId,
    string Name,
    string ConnectionType,
    string? LocalPrinterName,
    string? IpAddress,
    int? Port,
    int PaperWidth,
    bool Enabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SavePrinterRequest(
    [Required] Guid PrintAgentId,
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(40)] string ConnectionType,
    [MaxLength(260)] string? LocalPrinterName,
    [MaxLength(64)] string? IpAddress,
    [Range(1, 65535)] int? Port,
    [Range(58, 80)] int PaperWidth,
    bool Enabled = true);

public sealed record PrintingZoneDto(
    Guid Id,
    Guid LocationId,
    Guid PrintAgentId,
    string AgentName,
    string Name,
    bool Enabled,
    IReadOnlyCollection<Guid> PrinterIds,
    IReadOnlyCollection<Guid> CategoryIds,
    IReadOnlyCollection<Guid> ProductIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SavePrintingZoneRequest(
    Guid? LocationId,
    [Required] Guid PrintAgentId,
    [Required, MaxLength(120)] string Name,
    bool Enabled,
    IReadOnlyCollection<Guid> PrinterIds,
    IReadOnlyCollection<Guid> CategoryIds,
    IReadOnlyCollection<Guid> ProductIds);

public sealed record PrintJobDto(
    Guid Id,
    Guid LocationId,
    string LocationName,
    Guid PrintAgentId,
    string AgentName,
    Guid PrinterId,
    string PrinterName,
    string PrinterConnectionType,
    string? LocalPrinterName,
    string? PrinterIpAddress,
    int? PrinterPort,
    int PaperWidth,
    Guid? PrintingZoneId,
    string ZoneName,
    string SourceType,
    Guid SourceId,
    string DocumentType,
    string PayloadJson,
    string Status,
    int Attempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset QueuedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? PrintingAt,
    DateTimeOffset? PrintedAt,
    DateTimeOffset? FailedAt,
    string? LastError,
    Guid? OriginalPrintJobId,
    bool IsReprint);

public sealed record PrintJobQueryRequest(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    string? Status = null,
    Guid? ZoneId = null,
    Guid? AgentId = null,
    Guid? PrinterId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    DateOnly? FromLocalDate = null,
    DateOnly? ToLocalDate = null);

public sealed record PrintJobPageDto(
    IReadOnlyCollection<PrintJobDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PrintAgentHeartbeatRequest(
    [Required, MaxLength(50)] string Version,
    [MaxLength(64)] string? LocalIpAddress,
    [MaxLength(20)] string? Status);

public sealed record PrintJobStatusRequest(
    [Required, MaxLength(20)] string Status,
    [MaxLength(2000)] string? Error);

public sealed record DiscoveredPrinterDto(
    [Required, MaxLength(260)] string Name,
    bool IsDefault);

public sealed record SyncDiscoveredPrintersRequest(IReadOnlyCollection<DiscoveredPrinterDto> Printers);

public sealed record AvailablePrinterDto(
    Guid Id,
    Guid PrintAgentId,
    string Name,
    bool IsDefault,
    bool IsAvailable,
    bool IsConfigured,
    DateTimeOffset LastSeenAt);

public sealed record PrintingConfigurationDto(string? AgentDownloadUrl, int HeartbeatIntervalSeconds);

public sealed record PrintingRouteOptionDto(Guid Id, string Name);

public sealed record PrintingRoutingOptionsDto(
    IReadOnlyCollection<PrintingRouteOptionDto> Categories,
    IReadOnlyCollection<PrintingRouteOptionDto> Products);

public sealed record KitchenOrderPrintItem(int Quantity, string Name, IReadOnlyCollection<string> Modifiers, string? Notes);

public sealed record KitchenOrderPrintPayload(
    string DocumentType,
    string OrderNumber,
    string? Table,
    string Waiter,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<KitchenOrderPrintItem> Items,
    string? Notes,
    bool IsReprint,
    string? PrinterName = null,
    string? OrganizationName = null,
    string? FooterMessage = null);

public sealed record PrintingDestination(
    Guid LocationId,
    Guid AgentId,
    Guid? ZoneId,
    string ZoneName,
    IReadOnlyCollection<Guid> PrinterIds);

public sealed record PrintJobNotification(Guid AgentId, Guid PrintJobId);

public sealed record AuthenticatedPrintAgent(
    Guid AgentId,
    Guid TenantId,
    Guid LocationId,
    bool Enabled,
    DateTimeOffset TokenExpiresAt);
