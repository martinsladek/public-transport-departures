namespace Odjezdy;

public sealed class Departure
{
    public required string RouteShortName { get; init; }
    public required string Headsign { get; init; }
    public required DateTimeOffset Time { get; init; }
    public DateTimeOffset? ScheduledTime { get; init; }
}

public static class DepartureSelector
{
    public static IReadOnlyList<Departure> Upcoming(
        IEnumerable<Departure> departures,
        WatchConfig watch,
        DateTimeOffset now)
    {
        string? route = Normalize(watch.RouteShortName);
        string? headsign = Normalize(watch.HeadsignContains);

        return departures
            .Where(d => d.Time > now)
            .Where(d => route is null || string.Equals(Normalize(d.RouteShortName), route, StringComparison.OrdinalIgnoreCase))
            .Where(d => headsign is null || (Normalize(d.Headsign)?.Contains(headsign, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(d => d.Time)
            .ToList();
    }

    public static Departure? Next(IEnumerable<Departure> departures, WatchConfig watch, DateTimeOffset now) =>
        Upcoming(departures, watch, now).FirstOrDefault();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
