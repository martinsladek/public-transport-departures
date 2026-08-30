using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Departures;

enum BoardFetchStatus
{
    Ok,
    MissingApiKey,
    Rejected,
    Failed
}

readonly record struct BoardFetchResult(BoardFetchStatus Status, IReadOnlyList<Departure> Departures);

static class GolemioClient
{
    private static readonly HttpClient Http = CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<BoardFetchResult> FetchAsync(string apiKey, string stopId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new BoardFetchResult(BoardFetchStatus.MissingApiKey, []);

        if (string.IsNullOrWhiteSpace(stopId))
            return new BoardFetchResult(BoardFetchStatus.Failed, []);

        string url =
            "https://api.golemio.cz/v2/pid/departureboards"
            + "?ids=" + Uri.EscapeDataString(stopId.Trim())
            + "&limit=40"
            + "&minutesAfter=180"
            + "&preferredTimezone=Europe%2FPrague"
            + "&skip=canceled"
            + "&order=real";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("X-Access-Token", apiKey.Trim());

            using HttpResponseMessage response = await Http.SendAsync(request, token).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new BoardFetchResult(BoardFetchStatus.Rejected, []);

            if (!response.IsSuccessStatusCode)
                return new BoardFetchResult(BoardFetchStatus.Failed, []);

            await using Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            BoardDto? board = await JsonSerializer.DeserializeAsync<BoardDto>(stream, JsonOptions, token)
                .ConfigureAwait(false);

            List<Departure> departures = [];
            foreach (BoardDepartureDto? item in board?.Departures ?? [])
            {
                if (item is null || item.Trip?.IsCanceled == true)
                    continue;

                DateTimeOffset? time = ParseTime(item.DepartureTimestamp?.Predicted)
                    ?? ParseTime(item.DepartureTimestamp?.Scheduled);
                if (time is null)
                    continue;

                string route = item.Route?.ShortName?.Trim() ?? "";
                string headsign = item.Trip?.Headsign?.Trim() ?? "";
                if (route.Length == 0)
                    continue;

                departures.Add(new Departure
                {
                    RouteShortName = route,
                    Headsign = headsign,
                    Time = time.Value,
                    ScheduledTime = ParseTime(item.DepartureTimestamp?.Scheduled)
                });
            }

            return new BoardFetchResult(BoardFetchStatus.Ok, departures);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new BoardFetchResult(BoardFetchStatus.Failed, []);
        }
    }

    private static DateTimeOffset? ParseTime(string? value) =>
        DateTimeOffset.TryParse(value, out DateTimeOffset time) ? time : null;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Departures/0.2 (+https://www.martinsladek.com/)");
        return client;
    }

    private sealed class BoardDto
    {
        public List<BoardDepartureDto>? Departures { get; set; }
    }

    private sealed class BoardDepartureDto
    {
        public StopTimeDto? DepartureTimestamp { get; set; }
        public RouteDto? Route { get; set; }
        public TripDto? Trip { get; set; }
    }

    private sealed class StopTimeDto
    {
        public string? Predicted { get; set; }
        public string? Scheduled { get; set; }
    }

    private sealed class RouteDto
    {
        public string? ShortName { get; set; }
    }

    private sealed class TripDto
    {
        public string? Headsign { get; set; }

        [JsonPropertyName("is_canceled")]
        public bool IsCanceled { get; set; }
    }
}
