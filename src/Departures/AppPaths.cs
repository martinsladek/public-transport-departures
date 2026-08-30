namespace Departures;

static class AppPaths
{
    public static string DataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Strings.ConfigFolderName);

    public static string LegacyLocalDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Strings.LegacyConfigFolderName);

    public static string ConfigFile => Path.Combine(DataDirectory, "config.json");

    public static string GtfsDirectory => Path.Combine(DataDirectory, "gtfs");

    public static string GtfsZip => Path.Combine(GtfsDirectory, "PID_GTFS.zip");

    public static string StopsJson => Path.Combine(GtfsDirectory, "stops.json");

    public static string InstalledExe => Path.Combine(DataDirectory, Strings.ExeFileName);

    public static string LegacyInstalledExeInCurrentFolder =>
        Path.Combine(DataDirectory, Strings.LegacyExeFileName);

    public static string LegacyInstalledExeInOldFolder =>
        Path.Combine(LegacyLocalDataDirectory, Strings.LegacyExeFileName);

    public static string LegacyConfigFile =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Strings.LegacyConfigFolderName,
            "config.json");

    public static string CurrentExe =>
        Environment.ProcessPath
        ?? throw new InvalidOperationException("The current executable path is unknown.");

    public static bool IsRunningFromInstallLocation =>
        PathsEqual(CurrentExe, InstalledExe);

    public static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    public static void MigrateLegacyLocalData()
    {
        string current = DataDirectory;
        string legacy = LegacyLocalDataDirectory;
        if (PathsEqual(current, legacy) || !Directory.Exists(legacy))
            return;

        try
        {
            if (!Directory.Exists(current))
            {
                Directory.Move(legacy, current);
                return;
            }

            TryMoveFile(Path.Combine(legacy, "config.json"), ConfigFile);
            string legacyGtfs = Path.Combine(legacy, "gtfs");
            if (Directory.Exists(legacyGtfs) && !Directory.Exists(GtfsDirectory))
                Directory.Move(legacyGtfs, GtfsDirectory);

            if (!Directory.EnumerateFileSystemEntries(legacy).Any())
                Directory.Delete(legacy);
        }
        catch
        {
        }
    }

    public static void TryDeleteLegacyInstalledExe()
    {
        TryDeleteFile(LegacyInstalledExeInCurrentFolder);
        TryDeleteFile(LegacyInstalledExeInOldFolder);
    }

    private static void TryMoveFile(string from, string to)
    {
        if (!File.Exists(from) || File.Exists(to))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(to)!);
        File.Move(from, to);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path) && !PathsEqual(path, CurrentExe))
                File.Delete(path);
        }
        catch
        {
        }
    }

    public static string ExampleConfigInRepo
    {
        get
        {
            string? dir = Path.GetDirectoryName(CurrentExe);
            if (dir is null)
                return "";

            string nextToExe = Path.Combine(dir, "config.example.json");
            if (File.Exists(nextToExe))
                return nextToExe;

            DirectoryInfo? cursor = new(dir);
            while (cursor is not null)
            {
                string candidate = Path.Combine(cursor.FullName, "config.example.json");
                if (File.Exists(candidate))
                    return candidate;

                cursor = cursor.Parent;
            }

            return "";
        }
    }
}
