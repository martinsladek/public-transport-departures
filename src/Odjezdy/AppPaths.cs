namespace Odjezdy;

static class AppPaths
{
    public static string DataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Strings.ConfigFolderName);

    public static string ConfigFile => Path.Combine(DataDirectory, "config.json");

    public static string InstalledExe => Path.Combine(DataDirectory, "Odjezdy.exe");

    public static string LegacyConfigFile =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Strings.ConfigFolderName,
            "config.json");

    public static string CurrentExe =>
        Environment.ProcessPath
        ?? throw new InvalidOperationException("The current executable path is unknown.");

    public static bool IsRunningFromInstallLocation =>
        PathsEqual(CurrentExe, InstalledExe);

    public static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

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
