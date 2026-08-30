using Xunit;

namespace Odjezdy.Tests;

public class DepartureClockTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 14, 0, 0, TimeSpan.FromHours(2));

    [Theory]
    [InlineData(119.9, 1)]
    [InlineData(60.0, 1)]
    [InlineData(59.9, 0)]
    [InlineData(1.0, 0)]
    [InlineData(0.001, 0)]
    public void DisplayedMinutes_floors_whole_minutes(double remainingSeconds, int expected)
    {
        DateTimeOffset departure = Now.AddSeconds(remainingSeconds);
        Assert.Equal(expected, DepartureClock.DisplayedMinutes(departure, Now));
    }

    [Fact]
    public void DisplayedMinutes_is_null_when_due_or_past()
    {
        Assert.Null(DepartureClock.DisplayedMinutes(Now, Now));
        Assert.Null(DepartureClock.DisplayedMinutes(Now.AddMilliseconds(-1), Now));
    }

    [Fact]
    public void DisplayedMinutes_one_means_at_least_one_full_minute()
    {
        Assert.Equal(1, DepartureClock.DisplayedMinutes(Now.AddMinutes(1), Now));
        Assert.Equal(0, DepartureClock.DisplayedMinutes(Now.AddMinutes(1).AddTicks(-1), Now));
        Assert.Equal(1, DepartureClock.DisplayedMinutes(Now.AddMinutes(2).AddTicks(-1), Now));
        Assert.Equal(2, DepartureClock.DisplayedMinutes(Now.AddMinutes(2), Now));
    }

    [Fact]
    public void Next_change_from_one_to_zero_is_just_after_the_60_second_mark()
    {
        DateTimeOffset departure = Now.AddSeconds(90);
        TimeSpan delay = DepartureClock.DelayUntilNextDisplayChange(departure, Now);
        DateTimeOffset fireAt = Now + delay;

        Assert.Equal(1, DepartureClock.DisplayedMinutes(departure, Now));
        Assert.Equal(0, DepartureClock.DisplayedMinutes(departure, fireAt));
    }

    [Fact]
    public void Next_change_from_zero_drops_the_departure()
    {
        DateTimeOffset departure = Now.AddSeconds(20);
        TimeSpan delay = DepartureClock.DelayUntilNextDisplayChange(departure, Now);
        DateTimeOffset fireAt = Now + delay;

        Assert.Equal(0, DepartureClock.DisplayedMinutes(departure, Now));
        Assert.Null(DepartureClock.DisplayedMinutes(departure, fireAt));
    }

    [Fact]
    public void IconMinutes_caps_at_99()
    {
        Assert.Equal(0, DepartureClock.IconMinutes(0));
        Assert.Equal(12, DepartureClock.IconMinutes(12));
        Assert.Equal(99, DepartureClock.IconMinutes(99));
        Assert.Equal(99, DepartureClock.IconMinutes(140));
    }
}
