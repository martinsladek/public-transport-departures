using System.Globalization;

namespace Odjezdy;

static class Strings
{
    private static readonly string Lang =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

    public const string ProductName = "Odjezdy";
    public const string ConfigFolderName = "Odjezdy";

    public const string WebsiteUrl = "https://www.martinsladek.com/";
    public const string GitHubUrl = "https://github.com/martinsladek/public-transport-departures";

    public static string AppName => ProductName;

    public static string Settings => L("Settings", "Nastavení");

    public static string StartWithWindows => L("Start with Windows", "Spouštět s Windows");

    public static string About => L("About", "O aplikaci");

    public static string Exit => L("Exit", "Ukončit");

    public static string Ok => "OK";

    public static string Cancel => L("Cancel", "Zrušit");

    public static string Add => L("Add", "Přidat");

    public static string Edit => L("Edit", "Upravit");

    public static string Remove => L("Remove", "Odebrat");

    public static string Watches => L("Watches", "Sledované odjezdy");

    public static string WatchLabel => L("Label", "Název");

    public static string WatchFrom => L("From (GTFS stop id)", "Odkud (GTFS id sloupku)");

    public static string WatchLine => L("Line (optional)", "Linka (volitelně)");

    public static string WatchTo => L("To (destination, optional)", "Kam (cíl, volitelně)");

    public static string WatchHint => L(
        "Pillar ids: pid.cz/zastavky-pid",
        "Id sloupku: pid.cz/zastavky-pid");

    public static string GolemioKey => L(
        "Golemio API key (optional, delays)",
        "Golemio API klíč (volitelně, zpoždění)");

    public static string UpdateTimetable => L("Update timetable", "Aktualizovat jízdní řád");

    public static string EditWatch => L("Watch", "Sledovaný odjezd");

    public static string StopIdRequired => L("Enter a GTFS stop id.", "Zadejte GTFS identifikátor zastávky.");

    public static string NoStopConfigured => L("No stop configured", "Není nastavená zastávka");

    public static string AddStopInSettings => L("Add a stop in Settings", "Přidejte zastávku v Nastavení");

    public static string NoUpcoming => L("No upcoming departure", "Žádný nejbližší odjezd");

    public static string Downloading => L("Downloading timetable…", "Stahuji jízdní řád…");

    public static string DownloadFailed => L(
        "Could not download the timetable.",
        "Jízdní řád se nepodařilo stáhnout.");

    public static string TimetableReady => L("Timetable updated.", "Jízdní řád je aktualizovaný.");

    public static string Offline => L("offline", "offline");

    public static string AutostartFailed => L(
        "Could not change Start with Windows",
        "Spouštění s Windows se nepodařilo nastavit");

    public static string AboutTagline => L(
        "A lightweight Windows desktop utility that shows the next PID departure from one stop.",
        "Lehká desktopová utilita pro Windows, která ukazuje nejbližší odjezd PID z jedné zastávky.");

    public static string AboutDataCredit => L(
        "Timetables: PID open data (CC-BY).",
        "Jízdní řády: otevřená data PID (CC-BY).");

    public static string AboutCredit => L(
        "Developed by Martin Sladek with the help of AI models and workflows.",
        "Vytvořil Martin Sladek s pomocí AI modelů a vývojových postupů.");

    public static string Website => L("Website", "Web");

    public static string GitHub => "GitHub";

    public static string ApiKeyRejected => L(
        "Golemio rejected the API key. Scheduled times are still used.",
        "Golemio odmítlo API klíč. Používá se plánovaný čas.");

    public static string BalloonMinutes(int minutes) =>
        L($"in {minutes} min", $"za {minutes} min");

    public static string TooltipMinutes(int minutes) => $"({minutes} min)";

    private static string L(string en, string cs) => Lang == "cs" ? cs : en;
}
