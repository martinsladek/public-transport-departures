using System.IO.Compression;
using System.Net.Http.Headers;

namespace Odjezdy;

static class GtfsTimetable
{
    public const string FeedUrl = "https://data.pid.cz/PID_GTFS.zip";
    public static readonly TimeSpan Freshness = TimeSpan.FromHours(20);

    private static readonly HttpClient Http = CreateClient();

    public static bool ZipExists => File.Exists(AppPaths.GtfsZip);

    public static bool ZipLooksFresh()
    {
        if (!ZipExists)
            return false;

        return DateTime.UtcNow - File.GetLastWriteTimeUtc(AppPaths.GtfsZip) < Freshness;
    }

    public static async Task<bool> DownloadAsync(CancellationToken token)
    {
        Directory.CreateDirectory(AppPaths.GtfsDirectory);
        string temp = AppPaths.GtfsZip + ".part";
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

            if (File.Exists(AppPaths.GtfsZip))
                File.Delete(AppPaths.GtfsZip);
            File.Move(temp, AppPaths.GtfsZip);
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

    public static IReadOnlyList<Departure> LoadDepartures(IEnumerable<string> stopIds, DateTimeOffset now)
    {
        HashSet<string> stops = new(
            stopIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()),
            StringComparer.OrdinalIgnoreCase);

        if (stops.Count == 0 || !ZipExists)
            return [];

        try
        {
            using ZipArchive zip = ZipFile.OpenRead(AppPaths.GtfsZip);
            List<StopTimeRow> stopTimes = ReadStopTimes(zip, stops);
            if (stopTimes.Count == 0)
                return [];

            HashSet<string> tripIds = stopTimes.Select(s => s.TripId).ToHashSet(StringComparer.Ordinal);
            Dictionary<string, TripRow> trips = ReadTrips(zip, tripIds);
            HashSet<string> routeIds = trips.Values.Select(t => t.RouteId).ToHashSet(StringComparer.Ordinal);
            Dictionary<string, string> routes = ReadRoutes(zip, routeIds);
            GtfsCalendar calendar = ReadCalendar(zip);

            DateOnly serviceDate = GtfsTime.ServiceDate(now);
            DateOnly[] dates = [serviceDate.AddDays(-1), serviceDate, serviceDate.AddDays(1), serviceDate.AddDays(2)];

            var departures = new List<Departure>();
            foreach (StopTimeRow stopTime in stopTimes)
            {
                if (!trips.TryGetValue(stopTime.TripId, out TripRow trip))
                    continue;

                if (!routes.TryGetValue(trip.RouteId, out string? route) || string.IsNullOrWhiteSpace(route))
                    continue;

                foreach (DateOnly date in dates)
                {
                    if (!calendar.IsActive(trip.ServiceId, date))
                        continue;

                    DateTimeOffset when = GtfsTime.ToDateTimeOffset(date, stopTime.Seconds);
                    if (when <= now)
                        continue;

                    departures.Add(new Departure
                    {
                        RouteShortName = route,
                        Headsign = string.IsNullOrWhiteSpace(stopTime.Headsign) ? trip.Headsign : stopTime.Headsign,
                        Time = when
                    });
                }
            }

            return departures
                .OrderBy(d => d.Time)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static List<StopTimeRow> ReadStopTimes(ZipArchive zip, HashSet<string> stops)
    {
        using StreamReader reader = Open(zip, "stop_times.txt");
        string? header = reader.ReadLine();
        if (header is null)
            return [];

        Dictionary<string, int> map = GtfsCsv.HeaderMap(header);
        var rows = new List<StopTimeRow>();
        while (reader.ReadLine() is string line)
        {
            if (line.Length == 0)
                continue;

            string[] cells = GtfsCsv.Split(line);
            string stopId = GtfsCsv.Cell(cells, map, "stop_id");
            if (!stops.Contains(stopId))
                continue;

            if (!GtfsTime.TryParse(GtfsCsv.Cell(cells, map, "departure_time"), out int seconds))
                continue;

            string pickup = GtfsCsv.Cell(cells, map, "pickup_type");
            if (pickup is "1")
                continue;

            rows.Add(new StopTimeRow(
                GtfsCsv.Cell(cells, map, "trip_id"),
                seconds,
                GtfsCsv.Cell(cells, map, "stop_headsign")));
        }

        return rows;
    }

    private static Dictionary<string, TripRow> ReadTrips(ZipArchive zip, HashSet<string> tripIds)
    {
        using StreamReader reader = Open(zip, "trips.txt");
        string? header = reader.ReadLine();
        if (header is null)
            return [];

        Dictionary<string, int> map = GtfsCsv.HeaderMap(header);
        var trips = new Dictionary<string, TripRow>(StringComparer.Ordinal);
        while (reader.ReadLine() is string line)
        {
            if (line.Length == 0)
                continue;

            string[] cells = GtfsCsv.Split(line);
            string tripId = GtfsCsv.Cell(cells, map, "trip_id");
            if (!tripIds.Contains(tripId))
                continue;

            trips[tripId] = new TripRow(
                GtfsCsv.Cell(cells, map, "route_id"),
                GtfsCsv.Cell(cells, map, "service_id"),
                GtfsCsv.Cell(cells, map, "trip_headsign"));
        }

        return trips;
    }

    private static Dictionary<string, string> ReadRoutes(ZipArchive zip, HashSet<string> routeIds)
    {
        using StreamReader reader = Open(zip, "routes.txt");
        string? header = reader.ReadLine();
        if (header is null)
            return [];

        Dictionary<string, int> map = GtfsCsv.HeaderMap(header);
        var routes = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.ReadLine() is string line)
        {
            if (line.Length == 0)
                continue;

            string[] cells = GtfsCsv.Split(line);
            string routeId = GtfsCsv.Cell(cells, map, "route_id");
            if (!routeIds.Contains(routeId))
                continue;

            routes[routeId] = GtfsCsv.Cell(cells, map, "route_short_name");
        }

        return routes;
    }

    private static GtfsCalendar ReadCalendar(ZipArchive zip)
    {
        var calendar = new GtfsCalendar();
        ZipArchiveEntry? regular = Find(zip, "calendar.txt");
        if (regular is not null)
        {
            using StreamReader reader = new(regular.Open());
            string? header = reader.ReadLine();
            if (header is not null)
            {
                Dictionary<string, int> map = GtfsCsv.HeaderMap(header);
                while (reader.ReadLine() is string line)
                {
                    if (line.Length == 0)
                        continue;

                    string[] cells = GtfsCsv.Split(line);
                    string serviceId = GtfsCsv.Cell(cells, map, "service_id");
                    if (serviceId.Length == 0)
                        continue;

                    bool[] days =
                    [
                        Flag(cells, map, "monday"),
                        Flag(cells, map, "tuesday"),
                        Flag(cells, map, "wednesday"),
                        Flag(cells, map, "thursday"),
                        Flag(cells, map, "friday"),
                        Flag(cells, map, "saturday"),
                        Flag(cells, map, "sunday")
                    ];

                    if (!TryParseDate(GtfsCsv.Cell(cells, map, "start_date"), out DateOnly start)
                        || !TryParseDate(GtfsCsv.Cell(cells, map, "end_date"), out DateOnly end))
                        continue;

                    calendar.AddRegular(serviceId, days, start, end);
                }
            }
        }

        ZipArchiveEntry? exceptions = Find(zip, "calendar_dates.txt");
        if (exceptions is not null)
        {
            using StreamReader reader = new(exceptions.Open());
            string? header = reader.ReadLine();
            if (header is not null)
            {
                Dictionary<string, int> map = GtfsCsv.HeaderMap(header);
                while (reader.ReadLine() is string line)
                {
                    if (line.Length == 0)
                        continue;

                    string[] cells = GtfsCsv.Split(line);
                    string serviceId = GtfsCsv.Cell(cells, map, "service_id");
                    if (!TryParseDate(GtfsCsv.Cell(cells, map, "date"), out DateOnly date))
                        continue;

                    if (!int.TryParse(GtfsCsv.Cell(cells, map, "exception_type"), out int type))
                        continue;

                    calendar.AddException(serviceId, date, type);
                }
            }
        }

        return calendar;
    }

    private static StreamReader Open(ZipArchive zip, string name) =>
        new(Find(zip, name)?.Open() ?? throw new InvalidDataException($"GTFS file {name} is missing."));

    private static ZipArchiveEntry? Find(ZipArchive zip, string name) =>
        zip.Entries.FirstOrDefault(e =>
            string.Equals(Path.GetFileName(e.FullName), name, StringComparison.OrdinalIgnoreCase));

    private static bool Flag(string[] cells, Dictionary<string, int> map, string name) =>
        GtfsCsv.Cell(cells, map, name) == "1";

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyyMMdd", out date);

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
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Odjezdy/0.2 (+https://www.martinsladek.com/)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/zip"));
        return client;
    }

    private readonly record struct StopTimeRow(string TripId, int Seconds, string Headsign);

    private readonly record struct TripRow(string RouteId, string ServiceId, string Headsign);
}
