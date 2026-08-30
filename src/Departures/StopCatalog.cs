using System.Net.Http.Headers;
using System.Text.Json;

namespace Departures;

public sealed class StopGroup
{
    public required string Name { get; init; }
    public string Municipality { get; init; } = "";
    public IReadOnlyList<StopPillar> Pillars { get; init; } = [];

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Municipality) || Municipality.Equals("Praha", StringComparison.OrdinalIgnoreCase)
            ? Name
            : $"{Name} ({Municipality})";

    public override string ToString() => DisplayName;
}

public sealed class StopPillar
{
    public required string GtfsId { get; init; }
    public string Platform { get; init; } = "";
    public string AltName { get; init; } = "";
    public IReadOnlyList<string> Lines { get; init; } = [];
    public IReadOnlyList<string> Destinations { get; init; } = [];

    public string DisplayName
    {
        get
        {
            string platform = string.IsNullOrWhiteSpace(Platform) ? GtfsId : Platform;
            string lines = Lines.Count == 0 ? "" : " · " + string.Join(", ", Lines.Take(6));
            string extra = string.IsNullOrWhiteSpace(AltName) || AltName == platform ? "" : " · " + AltName;
            return platform + extra + lines;
        }
    }

    public override string ToString() => DisplayName;
}

public static class StopCatalog
{
    public const string FeedUrl = "https://data.pid.cz/stops/json/stops.json";

    private static readonly HttpClient Http = CreateClient();

    private static IReadOnlyList<StopGroup>? _groups;

    public static IReadOnlyList<StopGroup> Groups => _groups ?? [];

    public static bool FileExists => File.Exists(AppPaths.StopsJson);

    public static async Task<bool> EnsureAsync(CancellationToken token, bool force = false)
    {
        if (!force && _groups is { Count: > 0 } && FileLooksFresh())
            return true;

        if (force || !FileLooksFresh())
        {
            if (!await DownloadAsync(token).ConfigureAwait(false) && !FileExists)
                return false;
        }

        return LoadFromDisk();
    }

    public static IReadOnlyList<StopGroup> Search(string query, int limit = 40) =>
        Search(Groups, query, limit);

    public static IReadOnlyList<StopGroup> Search(IReadOnlyList<StopGroup> groups, string query, int limit = 40)
    {
        string folded = TextSearch.Fold(query).Trim();
        if (folded.Length < 2)
            return [];

        return groups
            .Where(g => TextSearch.Matches(g.Name, folded) || TextSearch.Matches(g.Municipality, folded))
            .OrderBy(g => TextSearch.Fold(g.Name).StartsWith(folded, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(limit)
            .ToList();
    }

    public static StopGroup? FindGroupByStopId(string? stopId)
    {
        if (string.IsNullOrWhiteSpace(stopId))
            return null;

        return Groups.FirstOrDefault(g =>
            g.Pillars.Any(p => string.Equals(p.GtfsId, stopId, StringComparison.OrdinalIgnoreCase)));
    }

    public static StopPillar? FindPillar(string? stopId)
    {
        if (string.IsNullOrWhiteSpace(stopId))
            return null;

        return Groups.SelectMany(g => g.Pillars)
            .FirstOrDefault(p => string.Equals(p.GtfsId, stopId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool FileLooksFresh() =>
        FileExists && DateTime.UtcNow - File.GetLastWriteTimeUtc(AppPaths.StopsJson) < GtfsTimetable.Freshness;

    private static async Task<bool> DownloadAsync(CancellationToken token)
    {
        Directory.CreateDirectory(AppPaths.GtfsDirectory);
        string temp = AppPaths.StopsJson + ".part";
        try
        {
            using HttpResponseMessage response = await Http.GetAsync(FeedUrl, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return false;

            await using Stream input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            await using FileStream output = File.Create(temp);
            await input.CopyToAsync(output, token).ConfigureAwait(false);
            output.Close();

            if (File.Exists(AppPaths.StopsJson))
                File.Delete(AppPaths.StopsJson);
            File.Move(temp, AppPaths.StopsJson);
            _groups = null;
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            TryDelete(temp);
            return false;
        }
    }

    public static IReadOnlyList<StopGroup> Parse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        JsonElement groupsElement = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("stopGroups", out JsonElement named) ? named : default;

        if (groupsElement.ValueKind != JsonValueKind.Array)
            return [];

        var groups = new List<StopGroup>();
        foreach (JsonElement group in groupsElement.EnumerateArray())
        {
            string name = Str(group, "name");
            if (name.Length == 0)
                name = Str(group, "uniqueName");
            if (name.Length == 0)
                name = Str(group, "idosName");
            if (name.Length == 0)
                continue;

            var pillars = new List<StopPillar>();
            if (group.TryGetProperty("stops", out JsonElement stops) && stops.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement stop in stops.EnumerateArray())
                {
                    string gtfsId = FirstGtfsId(stop);
                    if (gtfsId.Length == 0)
                        continue;

                    List<string> lines = [];
                    List<string> destinations = [];
                    if (stop.TryGetProperty("lines", out JsonElement lineEl) && lineEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement line in lineEl.EnumerateArray())
                        {
                            string lineName = Str(line, "name");
                            if (lineName.Length > 0 && !lines.Contains(lineName, StringComparer.OrdinalIgnoreCase))
                                lines.Add(lineName);

                            foreach (string dest in new[] { Str(line, "direction"), Str(line, "direction2") })
                            {
                                if (dest.Length > 0 && !destinations.Contains(dest, StringComparer.OrdinalIgnoreCase))
                                    destinations.Add(dest);
                            }
                        }
                    }

                    pillars.Add(new StopPillar
                    {
                        GtfsId = gtfsId,
                        Platform = Str(stop, "platform"),
                        AltName = Str(stop, "altIdosName"),
                        Lines = lines,
                        Destinations = destinations
                    });
                }
            }

            if (pillars.Count == 0)
                continue;

            groups.Add(new StopGroup
            {
                Name = name,
                Municipality = Str(group, "municipality"),
                Pillars = pillars
            });
        }

        return groups;
    }

    private static bool LoadFromDisk()
    {
        try
        {
            if (!FileExists)
                return false;

            IReadOnlyList<StopGroup> groups = Parse(File.ReadAllText(AppPaths.StopsJson));
            _groups = groups;
            return groups.Count > 0;
        }
        catch
        {
            _groups = [];
            return false;
        }
    }

    private static string FirstGtfsId(JsonElement stop)
    {
        if (stop.TryGetProperty("gtfsIds", out JsonElement ids) && ids.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement id in ids.EnumerateArray())
            {
                string value = id.GetString()?.Trim() ?? "";
                if (value.Length > 0)
                    return value;
            }
        }

        return Str(stop, "gtfsId");
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? ""
            : "";

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Departures/0.2 (+https://www.martinsladek.com/)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}
