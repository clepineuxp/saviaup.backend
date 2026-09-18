using NodaTime;
using NodaTime.TimeZones;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Time;

public sealed class IanaTimeZoneService : ITimeZoneService
{
    public bool IsValid(string timeZoneId) => !string.IsNullOrWhiteSpace(timeZoneId)
        && DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) is not null;

    private static DateTimeZone Zone(string id) => DateTimeZoneProviders.Tzdb.GetZoneOrNull(id)
        ?? throw new ArgumentException("Unknown IANA time zone.", nameof(id));

    public DateTimeOffset ConvertFromUtc(DateTimeOffset instant, string timeZoneId) =>
        Instant.FromDateTimeOffset(instant).InZone(Zone(timeZoneId)).ToDateTimeOffset();

    public DateTimeOffset ConvertToUtc(DateTime localTime, string timeZoneId)
    {
        if (localTime.Kind != DateTimeKind.Unspecified)
            throw new ArgumentException("A local date/time must have Unspecified kind.", nameof(localTime));
        var zone = Zone(timeZoneId);
        var mapping = zone.MapLocal(LocalDateTime.FromDateTime(localTime));
        if (mapping.Count != 1)
            throw new ArgumentException("Local time is ambiguous or does not exist. Supply an explicit instant.", nameof(localTime));
        return mapping.Single().ToInstant().ToDateTimeOffset();
    }

    public DateTimeOffset StartOfDayUtc(DateOnly date, string timeZoneId)
    {
        // Earliest occurrence at a repeated midnight; first valid instant after a gap.
        // A wholly skipped civil date has an empty range, never an invented 24-hour day.
        var midnight = new LocalDate(date.Year, date.Month, date.Day).AtMidnight();
        return Zone(timeZoneId).ResolveLocal(midnight,
            Resolvers.CreateMappingResolver(Resolvers.ReturnEarlier, Resolvers.ReturnStartOfIntervalAfter))
            .ToInstant().ToDateTimeOffset();
    }

    public (DateTimeOffset FromUtc, DateTimeOffset ToUtc) GetUtcRangeForLocalDate(DateOnly date, string timeZoneId) =>
        (StartOfDayUtc(date, timeZoneId), StartOfDayUtc(date.AddDays(1), timeZoneId));
}
