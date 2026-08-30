using Xunit;

namespace Departures.Tests;

public class GtfsCalendarTests
{
    [Fact]
    public void Regular_weekday_and_range()
    {
        var calendar = new GtfsCalendar();
        calendar.AddRegular(
            "weekday",
            [true, true, true, true, true, false, false],
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31));

        Assert.True(calendar.IsActive("weekday", new DateOnly(2026, 8, 31))); // Monday
        Assert.False(calendar.IsActive("weekday", new DateOnly(2026, 8, 30))); // Sunday
        Assert.False(calendar.IsActive("weekday", new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void Exception_can_add_or_remove_a_day()
    {
        var calendar = new GtfsCalendar();
        calendar.AddRegular(
            "weekday",
            [true, true, true, true, true, false, false],
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31));
        calendar.AddException("weekday", new DateOnly(2026, 8, 31), 2);
        calendar.AddException("extra", new DateOnly(2026, 8, 30), 1);

        Assert.False(calendar.IsActive("weekday", new DateOnly(2026, 8, 31)));
        Assert.True(calendar.IsActive("extra", new DateOnly(2026, 8, 30)));
        Assert.False(calendar.IsActive("unknown", new DateOnly(2026, 8, 30)));
    }
}
