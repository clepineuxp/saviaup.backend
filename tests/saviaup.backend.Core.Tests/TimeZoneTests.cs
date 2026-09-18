using SaviaUp.Backend.Infrastructure.Time;

namespace SaviaUp.Backend.Core.Tests;

public sealed class TimeZoneTests
{
    private readonly IanaTimeZoneService zones = new();

    [Theory]
    [InlineData("America/Bogota", "2026-09-18", "2026-09-18T05:00:00Z", 24)]
    [InlineData("America/New_York", "2026-03-08", "2026-03-08T05:00:00Z", 23)]
    [InlineData("America/New_York", "2026-11-01", "2026-11-01T04:00:00Z", 25)]
    [InlineData("Europe/Madrid", "2026-03-29", "2026-03-28T23:00:00Z", 23)]
    [InlineData("Europe/Madrid", "2026-10-25", "2026-10-24T22:00:00Z", 25)]
    [InlineData("Pacific/Apia", "2011-12-30", "2011-12-30T10:00:00Z", 0)]
    public void CivilDay_UsesRealZoneTransitions(string zone, string date, string expectedStart, int hours)
    {
        var (start, end) = zones.GetUtcRangeForLocalDate(DateOnly.Parse(date), zone);
        Assert.Equal(DateTimeOffset.Parse(expectedStart), start);
        Assert.Equal(TimeSpan.FromHours(hours), end - start);
        Assert.Equal(TimeSpan.Zero, start.Offset);
    }

    [Theory]
    [InlineData("UTC-5")]
    [InlineData("GMT-4")]
    [InlineData("Eastern Standard Time")]
    [InlineData("Imaginary/City")]
    public void RejectsNonIanaIdentifiers(string id) => Assert.False(zones.IsValid(id));

    [Theory]
    [InlineData(2026, 3, 8, 2, 30)]
    [InlineData(2026, 11, 1, 1, 30)]
    public void AmbiguousOrMissingWallTimesRequireExplicitInstant(int year, int month, int day, int hour, int minute) =>
        Assert.Throws<ArgumentException>(() => zones.ConvertToUtc(new DateTime(year, month, day, hour, minute, 0), "America/New_York"));

    [Fact]
    public void MidnightSaleAndCashShift_UseLocalDayButDurationsUseInstants()
    {
        var sale = DateTimeOffset.Parse("2026-09-19T03:30:00Z");
        Assert.Equal(new DateTime(2026, 9, 18, 22, 30, 0), zones.ConvertFromUtc(sale, "America/Bogota").DateTime);
        var opened = zones.ConvertToUtc(new DateTime(2026, 9, 18, 23, 30, 0), "America/Bogota");
        var closed = zones.ConvertToUtc(new DateTime(2026, 9, 19, 1, 0, 0), "America/Bogota");
        Assert.Equal(TimeSpan.FromMinutes(90), closed - opened);
        Assert.Equal(18, zones.ConvertFromUtc(opened, "America/Bogota").Day);
        Assert.Equal(19, zones.ConvertFromUtc(closed, "America/Bogota").Day);
    }

    [Fact]
    public void OrderDurationAcrossFallBack_IsNotWallClockSubtraction()
    {
        var started = DateTimeOffset.Parse("2026-11-01T05:45:00Z");
        var ended = DateTimeOffset.Parse("2026-11-01T06:15:00Z");
        Assert.Equal(TimeSpan.FromMinutes(30), ended - started);
        Assert.Equal(1, zones.ConvertFromUtc(started, "America/New_York").Hour);
        Assert.Equal(1, zones.ConvertFromUtc(ended, "America/New_York").Hour);
        Assert.NotEqual(zones.ConvertFromUtc(started, "America/New_York").Offset, zones.ConvertFromUtc(ended, "America/New_York").Offset);
    }
}
