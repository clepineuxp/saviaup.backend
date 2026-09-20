namespace SaviaUp.Backend.Domain.Entities;

public static class PrintAgentStatuses
{
    public const string Online = "ONLINE";
    public const string Offline = "OFFLINE";
    public const string Warning = "WARNING";
    public const string Disabled = "DISABLED";
}

public static class PrinterConnectionTypes
{
    public const string WindowsSpooler = "WINDOWS_SPOOLER";
    public const string Network = "NETWORK";
    public const string EscPosNetwork = "ESC_POS_NETWORK";

    public static bool IsValid(string value) => value is WindowsSpooler or Network or EscPosNetwork;
}

public static class PrintJobStatuses
{
    public const string Pending = "PENDING";
    public const string Queued = "QUEUED";
    public const string Sent = "SENT";
    public const string Processing = "PROCESSING";
    public const string Printed = "PRINTED";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";

    public static bool IsAgentStatus(string value) => value is Processing or Printed or Failed;
}

public sealed class Location
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<PrintAgent> PrintAgents { get; set; } = new List<PrintAgent>();
}

public sealed class PrintAgent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = PrintAgentStatuses.Offline;
    public string? LocalIpAddress { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool Enabled { get; set; } = true;
    public Location? Location { get; set; }
    public ICollection<Printer> Printers { get; set; } = new List<Printer>();
    public ICollection<PrintAgentDiscoveredPrinter> DiscoveredPrinters { get; set; } = new List<PrintAgentDiscoveredPrinter>();
    public ICollection<PrintingZone> Zones { get; set; } = new List<PrintingZone>();
    public ICollection<PrintAgentCredential> Credentials { get; set; } = new List<PrintAgentCredential>();
}

public sealed class PrintAgentDiscoveredPrinter
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LocationId { get; set; }
    public Guid PrintAgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTimeOffset LastSeenAt { get; set; }
    public PrintAgent? PrintAgent { get; set; }
}

public sealed class PrintAgentCredential
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PrintAgentId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public PrintAgent? PrintAgent { get; set; }
}

public sealed class PrintAgentPairingCode
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LocationId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public Location? Location { get; set; }
}

public sealed class Printer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LocationId { get; set; }
    public Guid PrintAgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = PrinterConnectionTypes.WindowsSpooler;
    public string? LocalPrinterName { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public int PaperWidth { get; set; } = 80;
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Location? Location { get; set; }
    public PrintAgent? PrintAgent { get; set; }
    public ICollection<PrintingZonePrinter> ZoneLinks { get; set; } = new List<PrintingZonePrinter>();
}

public sealed class PrintingZone
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LocationId { get; set; }
    public Guid PrintAgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Location? Location { get; set; }
    public PrintAgent? PrintAgent { get; set; }
    public ICollection<PrintingZonePrinter> PrinterLinks { get; set; } = new List<PrintingZonePrinter>();
    public ICollection<CategoryPrintingRoute> CategoryRoutes { get; set; } = new List<CategoryPrintingRoute>();
    public ICollection<ProductPrintingRoute> ProductRoutes { get; set; } = new List<ProductPrintingRoute>();
}

public sealed class PrintingZonePrinter
{
    public Guid TenantId { get; set; }
    public Guid PrintingZoneId { get; set; }
    public Guid PrinterId { get; set; }
    public PrintingZone? PrintingZone { get; set; }
    public Printer? Printer { get; set; }
}

public sealed class CategoryPrintingRoute
{
    public Guid TenantId { get; set; }
    public Guid PrintingZoneId { get; set; }
    public Guid CategoryId { get; set; }
    public PrintingZone? PrintingZone { get; set; }
    public Category? Category { get; set; }
}

public sealed class ProductPrintingRoute
{
    public Guid TenantId { get; set; }
    public Guid PrintingZoneId { get; set; }
    public Guid ProductId { get; set; }
    public PrintingZone? PrintingZone { get; set; }
    public Product? Product { get; set; }
}

public sealed class PrintJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LocationId { get; set; }
    public Guid PrintAgentId { get; set; }
    public Guid PrinterId { get; set; }
    public Guid PrintingZoneId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = PrintJobStatuses.Pending;
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset QueuedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? PrintingAt { get; set; }
    public DateTimeOffset? PrintedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? LastError { get; set; }
    public Guid? OriginalPrintJobId { get; set; }
    public bool IsReprint { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ReprintRequestedByUserId { get; set; }
    public DateTimeOffset? ReprintRequestedAt { get; set; }
    public Location? Location { get; set; }
    public PrintAgent? PrintAgent { get; set; }
    public Printer? Printer { get; set; }
    public PrintingZone? PrintingZone { get; set; }
    public PrintJob? OriginalPrintJob { get; set; }
}
