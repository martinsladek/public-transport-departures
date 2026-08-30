using System.Globalization;

namespace Departures;

static class Strings
{
    private static readonly string Lang =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

    public static string ProductName => L("Departures", "Odjezdy");
    public const string ConfigFolderName = "PublicTransportDepartures";
    public const string LegacyConfigFolderName = "Odjezdy";
    public const string ExeFileName = "PublicTransportDepartures.exe";
    public const string LegacyExeFileName = "Odjezdy.exe";

    public const string WebsiteUrl = "https://www.martinsladek.com/";
    public const string GitHubUrl = "https://github.com/martinsladek/public-transport-departures";

    public static string AppName => ProductName;

    public static string Settings => L("Settings", "Nastavení");

    public static string StartWithWindows => L("Start app with Windows", "Spouštět aplikaci s Windows");

    public static string About => L("About", "O aplikaci");

    public static string Exit => L("Exit", "Ukončit");

    public static string Ok => "OK";

    public static string Cancel => L("Cancel", "Zrušit");

    public static string Add => L("Add", "Přidat");

    public static string Edit => L("Edit", "Upravit");

    public static string Remove => L("Remove", "Odebrat");

    public static string Watches => L("Watches", "Sledované odjezdy");

    public static string WatchLabel => L("Label", "Název");

    public static string WatchSearch => L("Stop name", "Název zastávky");

    public static string WatchMatches => L("Matching stops", "Nalezené zastávky");

    public static string WatchPillar => L("Stop pillar", "Sloupek");

    public static string WatchLine => L("Line (optional)", "Linka (volitelně)");

    public static string WatchTo => L("To (destination, optional)", "Kam (cíl, volitelně)");

    public static string AnyLine => L("(any line)", "(kterákoli linka)");

    public static string AnyDestination => L("(any destination)", "(kterýkoli směr)");

    public static string WatchHint => L(
        "Type a stop name, then pick the pillar, line, and destination.",
        "Napište název zastávky a vyberte sloupek, linku a směr.");

    public static string WatchStopIdFallback => L(
        "Or paste a GTFS pillar id",
        "Nebo vložte GTFS id sloupku");

    public static string StopMap => L("Stop map", "Mapa zastávek");

    public const string StopMapUrl = "https://pid.cz/zastavky-pid/";

    public static string LoadingStops => L("Loading stops…", "Načítám zastávky…");

    public static string StopsLoadFailed => L(
        "Could not load the stop list. Check the network and try again.",
        "Seznam zastávek se nepodařilo načíst. Zkontrolujte síť a zkuste to znovu.");

    public static string GolemioSection => L("Live delays", "Živá zpoždění");

    public static string GolemioOptional => L("Optional live delays", "Volitelná živá zpoždění");

    public static string GolemioKey => L("Golemio API key", "Golemio API klíč");

    public static string GolemioHelp => L("Help", "Nápověda");

    public static string GolemioHelpTitle => L("Golemio API key", "Golemio API klíč");

    public static string GolemioHelpBody => L(
        "Optional. Scheduled departures work without it. A personal Golemio key adds live delays for the active watch.",
        "Volitelné. Plánované odjezdy fungují i bez něj. Vlastní Golemio klíč přidá živá zpoždění u aktivního sledování.");

    public static string GolemioHelpSteps => L(
        "Open the link, confirm the email if you are new, create an API key, then paste it here.",
        "Otevřete odkaz, při první registraci potvrďte e-mail, vytvořte API klíč a vložte ho sem.");

    public const string GolemioKeysUrl = "https://api.golemio.cz/api-keys";

    public static string AutostartSection => L("App startup", "Spuštění aplikace");

    public static string TimetableSection => L("Timetable", "Jízdní řád");

    public static string TimetableHint => L(
        "PID publishes a new timetable each morning (around 4:00). This app downloads it about once a day.",
        "PID vydává nový jízdní řád každé ráno (kolem 4:00). Aplikace ho stáhne zhruba jednou denně.");

    public static string UpdateTimetable => L("Update timetable", "Aktualizovat jízdní řád");

    public static string EditWatch => L("Watch", "Sledovaný odjezd");

    public static string StopIdRequired => L("Select a stop pillar.", "Vyberte zastávkový sloupek.");

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
