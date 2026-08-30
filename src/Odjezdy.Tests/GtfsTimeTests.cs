using Xunit;

namespace Odjezdy.Tests;

public class GtfsTimeTests
{
    [Theory]
    [InlineData("5:32:00", 5 * 3600 + 32 * 60)]
    [InlineData("05:32:00", 5 * 3600 + 32 * 60)]
    [InlineData("25:10:00", 25 * 3600 + 10 * 60)]
    [InlineData("30:00:00", 30 * 3600)]
    public void Parses_hours_past_midnight(string value, int seconds)
    {
        Assert.True(GtfsTime.TryParse(value, out int parsed));
        Assert.Equal(seconds, parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("5:32")]
    [InlineData("xx:00:00")]
    public void Rejects_invalid_times(string value)
    {
        Assert.False(GtfsTime.TryParse(value, out _));
    }

    [Fact]
    public void Night_time_lands_on_the_next_calendar_morning()
    {
        DateTimeOffset when = GtfsTime.ToDateTimeOffset(new DateOnly(2026, 8, 29), 25 * 3600 + 10 * 60);
        DateTimeOffset local = TimeZoneInfo.ConvertTime(when, GtfsTime.PragueTimeZone());
        Assert.Equal(new DateTime(2026, 8, 30, 1, 10, 0), local.DateTime);
    }

    [Fact]
    public void Service_date_before_4am_belongs_to_the_previous_day()
    {
        var tz = GtfsTime.PragueTimeZone();
        var local = new DateTime(2026, 8, 30, 3, 15, 0, DateTimeKind.Unspecified);
        var now = new DateTimeOffset(local, tz.GetUtcOffset(local));
        Assert.Equal(new DateOnly(2026, 8, 29), GtfsTime.ServiceDate(now));
    }

    [Fact]
    public void Service_date_after_4am_is_today()
    {
        var tz = GtfsTime.PragueTimeZone();
        var local = new DateTime(2026, 8, 30, 4, 0, 0, DateTimeKind.Unspecified);
        var now = new DateTimeOffset(local, tz.GetUtcOffset(local));
        Assert.Equal(new DateOnly(2026, 8, 30), GtfsTime.ServiceDate(now));
    }
}
