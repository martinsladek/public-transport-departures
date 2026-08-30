namespace Departures;

public static class GtfsTime
{
    /// <summary>
    /// Parses GTFS departure_time (HH:MM:SS). Hours may be 24–30 for night service.
    /// </summary>
    public static bool TryParse(string? value, out int secondsFromServiceMidnight)
    {
        secondsFromServiceMidnight = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts = value.Trim().Split(':');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out int hours)
            || !int.TryParse(parts[1], out int minutes)
            || !int.TryParse(parts[2], out int seconds))
            return false;

        if (hours < 0 || minutes is < 0 or > 59 || seconds is < 0 or > 59)
            return false;

        secondsFromServiceMidnight = hours * 3600 + minutes * 60 + seconds;
        return true;
    }

    public static DateTimeOffset ToDateTimeOffset(DateOnly serviceDate, int secondsFromServiceMidnight)
    {
        TimeZoneInfo tz = PragueTimeZone();
        DateTime localUnspecified = DateTime.SpecifyKind(
            serviceDate.ToDateTime(TimeOnly.MinValue).AddSeconds(secondsFromServiceMidnight),
            DateTimeKind.Unspecified);
        TimeSpan offset = tz.GetUtcOffset(localUnspecified);
        return new DateTimeOffset(localUnspecified, offset);
    }

    public static DateOnly ServiceDate(DateTimeOffset now)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(now, PragueTimeZone());
        if (local.TimeOfDay < TimeSpan.FromHours(4))
            return DateOnly.FromDateTime(local.DateTime).AddDays(-1);

        return DateOnly.FromDateTime(local.DateTime);
    }

    public static TimeZoneInfo PragueTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
        }
    }
}
