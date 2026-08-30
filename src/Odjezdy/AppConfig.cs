using System.Text.Json;

namespace Odjezdy;

public sealed class WatchConfig
{
    public string Id { get; set; } = "";
    public string? Label { get; set; }
    public string StopId { get; set; } = "";
    public string? RouteShortName { get; set; }
    public string? HeadsignContains { get; set; }

    public string MenuLabel
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Label))
                return Label;

            if (!string.IsNullOrWhiteSpace(RouteShortName) && !string.IsNullOrWhiteSpace(HeadsignContains))
                return $"{RouteShortName.Trim()} → {HeadsignContains.Trim()}";

            if (!string.IsNullOrWhiteSpace(RouteShortName))
                return RouteShortName.Trim();

            return string.IsNullOrWhiteSpace(StopId) ? Strings.NoStopConfigured : StopId.Trim();
        }
    }
}

sealed class AppConfig
{
    public string? GolemioApiKey { get; set; }
    public string? ActiveWatchId { get; set; }
    public List<WatchConfig> Watches { get; set; } = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public WatchConfig? ActiveWatch
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ActiveWatchId))
            {
                WatchConfig? named = Watches.FirstOrDefault(w =>
                    string.Equals(w.Id, ActiveWatchId, StringComparison.OrdinalIgnoreCase));
                if (named is not null)
                    return named;
            }

            return Watches.FirstOrDefault();
        }
    }

    public static AppConfig Load()
    {
        try
        {
            MigrateLegacyConfig();
            EnsureCreated();
            string path = AppPaths.ConfigFile;
            if (!File.Exists(path))
                return new AppConfig();

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save()
    {
        string path = AppPaths.ConfigFile;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    public static void EnsureCreated()
    {
        string path = AppPaths.ConfigFile;
        if (File.Exists(path))
            return;

        Directory.CreateDirectory(AppPaths.DataDirectory);
        string example = AppPaths.ExampleConfigInRepo;
        if (example.Length > 0)
        {
            File.Copy(example, path);
            return;
        }

        var created = new AppConfig
        {
            GolemioApiKey = "",
            ActiveWatchId = "example",
            Watches =
            [
                new WatchConfig
                {
                    Id = "example",
                    Label = "Example · 22",
                    StopId = "U1040Z1P",
                    RouteShortName = "22",
                    HeadsignContains = "Braník"
                }
            ]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(created, JsonOptions));
    }

    private static void MigrateLegacyConfig()
    {
        string current = AppPaths.ConfigFile;
        string legacy = AppPaths.LegacyConfigFile;
        if (File.Exists(current) || !File.Exists(legacy))
            return;

        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.Move(legacy, current, overwrite: false);

        try
        {
            string? legacyDir = Path.GetDirectoryName(legacy);
            if (legacyDir is not null && Directory.Exists(legacyDir) && !Directory.EnumerateFileSystemEntries(legacyDir).Any())
                Directory.Delete(legacyDir);
        }
        catch
        {
        }
    }
}
