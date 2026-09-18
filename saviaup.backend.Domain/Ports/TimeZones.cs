namespace SaviaUp.Backend.Domain.Ports;

public interface ITimeZoneService
{
    bool IsValid(string timeZoneId);
    DateTimeOffset ConvertFromUtc(DateTimeOffset instant, string timeZoneId);
    // Unspecified local time only. Ambiguous/nonexistent times are rejected.
    DateTimeOffset ConvertToUtc(DateTime localTime, string timeZoneId);
    DateTimeOffset StartOfDayUtc(DateOnly date, string timeZoneId);
    (DateTimeOffset FromUtc, DateTimeOffset ToUtc) GetUtcRangeForLocalDate(DateOnly date, string timeZoneId);
}

public interface IOrganizationTimeZone
{
    Task<string> GetAsync(Guid tenantId, CancellationToken cancellationToken);
}
