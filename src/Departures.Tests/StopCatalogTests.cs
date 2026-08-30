using Xunit;

namespace Departures.Tests;

public class StopCatalogTests
{
    private const string Sample = """
        {
          "stopGroups": [
            {
              "name": "Anděl",
              "municipality": "Praha",
              "stops": [
                {
                  "platform": "B",
                  "altIdosName": "Anděl",
                  "gtfsIds": ["U1040Z101P"],
                  "lines": [
                    { "name": "B", "direction": "Černý Most", "direction2": "Zličín" }
                  ]
                },
                {
                  "platform": "A",
                  "gtfsIds": ["U1040Z102P"],
                  "lines": [
                    { "name": "B", "direction": "Zličín" }
                  ]
                }
              ]
            },
            {
              "name": "Albertov",
              "municipality": "Praha",
              "stops": [
                {
                  "platform": "A",
                  "gtfsIds": ["U876Z1P"],
                  "lines": [
                    { "name": "7", "direction": "Sídliště Barrandov" }
                  ]
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Parse_reads_pillars_lines_and_destinations()
    {
        IReadOnlyList<StopGroup> groups = StopCatalog.Parse(Sample);
        Assert.Equal(2, groups.Count);

        StopGroup andel = groups[0];
        Assert.Equal("Anděl", andel.Name);
        Assert.Equal(2, andel.Pillars.Count);
        Assert.Equal("U1040Z101P", andel.Pillars[0].GtfsId);
        Assert.Equal("B", andel.Pillars[0].Platform);
        Assert.Contains("B", andel.Pillars[0].Lines);
        Assert.Contains("Černý Most", andel.Pillars[0].Destinations);
        Assert.Contains("Zličín", andel.Pillars[0].Destinations);
    }

    [Fact]
    public void Search_finds_andel_without_diacritics()
    {
        IReadOnlyList<StopGroup> groups = StopCatalog.Parse(Sample);
        IReadOnlyList<StopGroup> hits = StopCatalog.Search(groups, "andel");
        Assert.Single(hits);
        Assert.Equal("Anděl", hits[0].Name);
    }

    [Fact]
    public void Search_requires_two_characters()
    {
        IReadOnlyList<StopGroup> groups = StopCatalog.Parse(Sample);
        Assert.Empty(StopCatalog.Search(groups, "a"));
        Assert.Empty(StopCatalog.Search(groups, ""));
    }

    [Fact]
    public void Search_prefers_prefix_matches()
    {
        IReadOnlyList<StopGroup> groups = StopCatalog.Parse(Sample);
        IReadOnlyList<StopGroup> hits = StopCatalog.Search(groups, "al");
        Assert.Equal("Albertov", hits[0].Name);
    }
}
