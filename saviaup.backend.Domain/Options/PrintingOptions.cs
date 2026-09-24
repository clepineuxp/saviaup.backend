namespace SaviaUp.Backend.Domain.Options;

public sealed class PrintingOptions
{
    public const string SectionName = "Printing";
    public string? AgentDownloadUrl { get; set; }
    public int PairingCodeExpirationMinutes { get; set; } = 10;
    public int DeviceTokenExpirationDays { get; set; } = 365;
    public int HeartbeatIntervalSeconds { get; set; } = 20;
    public int OfflineTimeoutSeconds { get; set; } = 90;
    public int HeartbeatPersistenceSeconds { get; set; } = 10;
    public int DiscoveryExpirationSeconds { get; set; } = 120;
    public int DiscoveryPollIntervalSeconds { get; set; } = 3;
    public string? NetworkFingerprintKey { get; set; }
}
