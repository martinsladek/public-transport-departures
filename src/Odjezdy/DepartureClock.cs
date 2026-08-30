namespace Odjezdy;

public static class DepartureClock
{
    /// <summary>
    /// Whole minutes remaining, rounded down. Null when the departure is due or past.
    /// 1 means 1:00.0 through 1:59.9…; 0 means more than 0 and less than 1:00.0.
    /// </summary>
    public static int? DisplayedMinutes(DateTimeOffset departure, DateTimeOffset now)
    {
        TimeSpan remaining = departure - now;
        if (remaining <= TimeSpan.Zero)
            return null;

        int seconds = (int)Math.Floor(remaining.TotalSeconds);
        if (seconds < 0)
            return null;

        return seconds / 60;
    }

    /// <summary>
    /// Delay until the displayed minute changes (or the departure is dropped).
    /// </summary>
    public static TimeSpan DelayUntilNextDisplayChange(DateTimeOffset departure, DateTimeOffset now)
    {
        int? minutes = DisplayedMinutes(departure, now);
        if (minutes is null)
            return TimeSpan.Zero;

        TimeSpan remaining = departure - now;
        TimeSpan threshold = TimeSpan.FromSeconds(minutes.Value * 60.0);
        TimeSpan delay = remaining - threshold + TimeSpan.FromMilliseconds(1);
        if (delay < TimeSpan.FromMilliseconds(50))
            return TimeSpan.FromMilliseconds(50);

        if (delay > TimeSpan.FromMinutes(1))
            return TimeSpan.FromMinutes(1);

        return delay;
    }

    public static int IconMinutes(int minutes) => Math.Clamp(minutes, 0, 99);
}
