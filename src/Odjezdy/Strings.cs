using System.Globalization;

namespace Odjezdy;

static class Strings
{
    private static readonly string Lang =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

    public const string ProductName = "Odjezdy";
    public const string ConfigFolderName = "Odjezdy";

    public const string WebsiteUrl = "https://www.martinsladek.com/";

    public static string AppName => ProductName;

    public static string Settings => L("Settings", "Nastavení");

    public static string StartWithWindows => L("Start with Windows", "Spouštět s Windows");

    public static string About => L("About", "O aplikaci");

    public static string Exit => L("Exit", "Ukončit");

    public static string Ok => "OK";

    public static string NoStopConfigured => L("No stop configured", "Není nastavená zastávka");

    public static string SetupConfig => L("Set up config.json", "Nastavte config.json");

    public static string NoUpcoming => L("No upcoming departure", "Žádný nejbližší odjezd");

    public static string Offline => L("offline", "offline");

    public static string AutostartFailed => L(
        "Could not change Start with Windows",
        "Spouštění s Windows se nepodařilo nastavit");

    public static string AboutTagline => L(
        "A lightweight Windows desktop utility that shows the next PID departure from one stop.",
        "Lehká desktopová utilita pro Windows, která ukazuje nejbližší odjezd PID z jedné zastávky.");

    public static string AboutCredit => L(
        "Developed by Martin Sladek with the help of AI models and workflows.",
        "Vytvořil Martin Sladek s pomocí AI modelů a vývojových postupů.");

    public static string Website => L("Website", "Web");

    public static string MissingApiKey => L(
        "Add your Golemio API key to config.json.",
        "Do config.json doplňte Golemio API klíč.");

    public static string MissingStop => L(
        "Add a watch with a PID stop id to config.json.",
        "Do config.json doplňte watch s PID identifikátorem zastávky.");

    public static string ConfigPathPrefix => L("Config file:", "Soubor nastavení:");

    public static string ApiKeyRejected => L(
        "Golemio rejected the API key.",
        "Golemio odmítlo API klíč.");

    public static string RefreshFailed => L(
        "Could not refresh departures.",
        "Odjezdy se nepodařilo obnovit.");

    public static string BalloonMinutes(int minutes) =>
        L($"in {minutes} min", $"za {minutes} min");

    public static string TooltipMinutes(int minutes) => $"({minutes} min)";

    private static string L(string en, string cs) => Lang == "cs" ? cs : en;
}
