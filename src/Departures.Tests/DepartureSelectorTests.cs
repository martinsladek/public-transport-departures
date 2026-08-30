using Xunit;

namespace Departures.Tests;

public class DepartureSelectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 14, 0, 0, TimeSpan.FromHours(2));

    private static Departure D(string route, string headsign, int minutes) => new()
    {
        RouteShortName = route,
        Headsign = headsign,
        Time = Now.AddMinutes(minutes)
    };

    [Fact]
    public void Picks_soonest_matching_route_and_headsign()
    {
        var watch = new WatchConfig
        {
            Id = "home",
            StopId = "U1Z1P",
            RouteShortName = "22",
            HeadsignContains = "Braník"
        };

        Departure[] board =
        [
            D("9", "Sídliště Řepy", 2),
            D("22", "Bílá Hora", 3),
            D("22", "Nádraží Braník", 8),
            D("22", "Nádraží Braník", 20)
        ];

        Departure? next = DepartureSelector.Next(board, watch, Now);
        Assert.NotNull(next);
        Assert.Equal("22", next.RouteShortName);
        Assert.Equal("Nádraží Braník", next.Headsign);
        Assert.Equal(Now.AddMinutes(8), next.Time);
    }

    [Fact]
    public void Skips_departures_that_are_due()
    {
        var watch = new WatchConfig { Id = "x", StopId = "U1Z1P", RouteShortName = "22" };
        Departure[] board =
        [
            D("22", "A", -1),
            D("22", "B", 4)
        ];

        Departure? next = DepartureSelector.Next(board, watch, Now);
        Assert.NotNull(next);
        Assert.Equal("B", next.Headsign);
    }

    [Fact]
    public void Empty_filters_mean_any_line_or_direction()
    {
        var watch = new WatchConfig { Id = "x", StopId = "U1Z1P" };
        Departure[] board =
        [
            D("9", "Sídliště Řepy", 5),
            D("22", "Nádraží Braník", 2)
        ];

        Departure? next = DepartureSelector.Next(board, watch, Now);
        Assert.Equal("22", next?.RouteShortName);
    }
}
