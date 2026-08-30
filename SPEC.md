# Odjezdy — specification

Hand this file to a coding agent with: **Implement this specification on Windows 10.**

This is the product contract, not a chat log. Follow the decisions below. Do not resurrect rejected ideas from the “Out of scope” section.

## Goal

A tiny Windows 10 desktop utility that lives only in the **notification area** (system tray). It shows the **next departure** of one user-defined PID line from one stop pillar, in one direction. There is no main window.

| Item | Value |
|---|---|
| Product name | Odjezdy |
| Assembly / EXE name | `Odjezdy.exe` |
| Config / install folder | `%LocalAppData%\Odjezdy\` (stable, never localized) |
| Author | Martin Sladek |
| Website | https://www.martinsladek.com/ |

v1 is a single hardcoded **watch** (stop + line + optional headsign filter) in `config.json`. Later versions may add more watches, manual menu selection, and location-based switching. Those features must reuse the same watch model — they do not replace it.

## Data source

v1 reads **Pražská integrovaná doprava (PID)** only (Prague and the PID regional network).

- Live departures: Golemio PID departure boards (`https://api.golemio.cz/v2/pid/departureboards`).
- The user supplies their own free Golemio API key. The key lives only in `%LocalAppData%\Odjezdy\config.json`. It is never committed to git and never baked into the EXE.
- Stop identity is the PID **GTFS `stop_id`** of a concrete **stop pillar** (sloupek), for example `U1072Z101P`. A human stop name is not enough — one name has several pillars and directions.
- A public stop list is at [PID open data](https://pid.cz/o-systemu/opendata/) (`https://data.pid.cz/stops/json/stops.json`). A map of pillars is at [pid.cz/zastavky-pid](https://pid.cz/zastavky-pid/).
- Use the **predicted** departure time when Golemio provides one; otherwise the scheduled time. Skip canceled trips.
- Refresh the board about every **45 seconds**. Do not call the API on every hover or every clock tick.
- On network or API failure, keep the last successful board if it still has a future departure. The icon stays usable; the tooltip/balloon may say the data is stale.

Register a key at https://api.golemio.cz/api-keys

## Watch model

A **watch** is the unit that later locations and menus will select. v1 has exactly one watch in the file (more may exist in the JSON later; only `activeWatchId` is used).

`%LocalAppData%\Odjezdy\config.json`:

```json
{
  "golemioApiKey": "",
  "activeWatchId": "example",
  "watches": [
    {
      "id": "example",
      "label": "Home · 22",
      "stopId": "U1040Z1P",
      "routeShortName": "22",
      "headsignContains": "Braník"
    }
  ]
}
```

| Field | Rule |
|---|---|
| `id` | Stable id. `activeWatchId` points at one watch. |
| `label` | Menu text. If empty, show `{route} → {headsign}` or the stop id. |
| `stopId` | Required. GTFS pillar id. |
| `routeShortName` | Optional. Exact line label (`22`, `A`, `S9`). Empty = any line at that pillar. |
| `headsignContains` | Optional. Case-insensitive substring of the trip headsign. Empty = any direction. |

On first launch, if `config.json` is missing, write a copy of the repo `config.example.json` into LocalAppData. Do not overwrite an existing file.

If an older build left `config.json` in `%AppData%\Odjezdy\`, move it to LocalAppData on first load and remove the empty roaming folder.

Do not use Roaming: the EXE is large, and the API key is machine-local.

## Behavior

### Icon

Draw a simple 32×32 tray icon at runtime (filled circle + text). Do not ship third-party icon assets.

| State | Look | Meaning |
|---|---|---|
| Has a future departure | Windows blue (`#0078D7`) fill, white digits | Next matching departure is known |
| No departure / not configured / no usable cache | Grey filled circle (`#707070`), white `—` | Nothing to count down |

When a future departure exists, the circle shows **whole minutes remaining, rounded down**:

- **1 minute** means at least 1:00.0 and less than 2:00.0 (1:00.0 through 1:59.9…).
- **0** means at least 0:00.1 and less than 1:00.0.
- At the exact departure instant (remaining ≤ 0), that trip is gone. Show the next matching cached departure, or grey `—` if none.
- 100 minutes or more: show `99`.

The displayed number must change at the **second** the remaining time crosses a minute boundary (for example 2:00.0 → still `2`; 1:59.9 → `1`). Schedule the next icon/tooltip refresh for that instant. Do **not** drive the number from a loose one-minute poll, and do not call Golemio every second.

The 45-second board refresh may replace the predicted time; after that, reschedule the display timer from the new timestamp.

### Hover

Native tooltip only. No menu on hover. `NotifyIcon.Text` is limited to 63 characters — truncate.

Examples:

- `22 → Nádraží Braník  14:32 (3 min)`
- `Set up config.json`
- `No upcoming departure`
- `22 → Nádraží Braník  14:32 (3 min) · offline`

### Left click — balloon

Show a balloon with the same fact as the tooltip, but without the 63-character limit. Include clock time and whole minutes remaining.

If the app is not configured (missing API key, missing watch, missing `stopId`), the balloon explains that and includes the full path to `config.json`.

### Right click — native context menu

No flyout (custom popup above the taskbar). Use `ContextMenuStrip`.

```
✓  Home · 22
─────────────────
   Settings
   Start with Windows  ✓
─────────────────
   About
   Exit
```

Rules:

- List configured watches. Checkmark = `activeWatchId`. v1 has one watch; clicking it keeps it selected.
- If the list is empty, show a disabled “No stop configured” row.
- **Settings** is visible and **disabled** (does nothing in v1).
- **Start with Windows** is a checkable setting (see Autostart). Read live registry state when the menu opens.
- **About** sits immediately above **Exit**.
- **Exit** hides the tray icon and quits.

### About dialog

Standard modal WinForms dialog (`FixedDialog`, no maximize/minimize, not in the taskbar). Product name stays **Odjezdy**. Tagline, credit, and the website link label come from `Strings.cs` for the current UI language.

English canonical copy:

> A lightweight Windows desktop utility that shows the next PID departure from one stop.
>
> Developed by Martin Sladek with the help of AI models and workflows.

Clickable links (labels are localized; URLs are not):

| Role | URL |
|---|---|
| Website | https://www.martinsladek.com/ |

GitHub and Download rows are omitted until a public repository and Release binary exist.

OK button closes the dialog. Product name and OK stay untranslated.

### Language

Read `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName`. UI strings live in `Strings.cs`. Supported:

`en` (default), `cs`

Any other Windows language falls back to English. Product name stays **Odjezdy** in every locale. README, SPEC, and GitHub stay English only.

## Autostart

Optional, off by default. No admin rights. Portable until the user opts in.

**Enable**

1. Copy the currently running EXE to `%LocalAppData%\Odjezdy\Odjezdy.exe` (skip if already running from that path).
2. Write `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` value `Odjezdy` = quoted path to that copy.
3. Write `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run` value `Odjezdy` as enabled (`0x02…`), so Task Manager / Settings → Apps → Startup agree.

**Disable**

1. Delete the Run value and the StartupApproved value. Autostart is off immediately (next logon will not start the app). Keep `config.json`.
2. Delete the installed EXE now if this process is **not** that file.
3. If this process **is** the installed EXE, Windows will not delete a running image. Mark it for deletion and remove it ~1s after Exit via `cmd timeout & del`. Re-checking Start with Windows before Exit cancels that deletion.

On a later portable launch, if Run is not registered, delete any leftover installed EXE.

**Checkbox state**

Checked only if the Run value exists **and** StartupApproved does not mark it disabled (`0x03` / `0x07`). Task Manager “Disable” leaves Run in place; the menu must not show checked in that case. Checking the box again re-enables Approved.

**Updates**

On launch, if Run is registered and this process is a different file than the installed copy (size or last-write), overwrite the LocalAppData EXE so the next logon is not an old download.

## Process

Single instance via mutex `Local\Odjezdy.SingleInstance`. A second launch exits silently.

## Technical stack (required)

| Layer | Choice |
|---|---|
| Language | C# |
| UI | WinForms, `ApplicationContext` + `NotifyIcon` (no main form) |
| Target | `net8.0-windows10.0.19041.0` |
| Output | `WinExe`, self-contained single-file `win-x64` |
| HTTP | `HttpClient` to Golemio. No third-party transit SDK. |

Publish:

```powershell
dotnet publish src/Odjezdy/Odjezdy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist
```

The published EXE must run on a PC that has no .NET SDK and no extra runtimes installed.

Do not use trimming (`PublishTrimmed=false`).

No installer in v1. Distribution of the binary is **GitHub Releases**, never git. A Release is created by GitHub Actions on `windows-latest` when a tag matching `v*` is pushed (`Odjezdy.exe` asset). The `/releases/latest/download/Odjezdy.exe` URL then points at that Release — only after a public repository exists.

## Suggested layout

```
src/Odjezdy/
  Odjezdy.csproj
  app.manifest
  Program.cs
  Strings.cs
  AppConfig.cs
  AppPaths.cs
  Autostart.cs
  TrayApplicationContext.cs
  TrayIcons.cs
  AboutForm.cs
  DepartureClock.cs
  DepartureSelector.cs
  GolemioClient.cs
src/Odjezdy.Tests/
```

`.gitignore`: `bin/`, `obj/`, `dist/`, `.vs/`, `*.user`.

README is English only. Do not put a download link in README until a public Release binary exists.

## Out of scope (do not implement)

These were considered and deferred or rejected:

- Windows Location / GPS / geofencing
- Multiple locations and behavior profiles (a location will later only select which watch is active)
- Settings dialog or editing watches in the UI
- Choosing a stop from a search list
- More than one *active* watch at a time
- Other Czech regions (IDS JMK, IREDO, …) or IDOS
- Walking time to the stop (`walkMinutes`)
- Downloading or parsing the full PID GTFS zip in v1
- Custom tray flyout instead of a native context menu
- Java, Python, Node, C++ toolchains
- Putting the self-contained EXE into git
- Installer

No administrator rights are required.

## Definition of done

- Tray icon: blue + floored minutes, or grey `—`
- Displayed minutes change at the correct second
- Hover tooltip and left-click balloon
- Right-click menu as specified (Settings disabled)
- About dialog with the website link
- UI localized for `en` and `cs` (other Windows languages fall back to English)
- Optional Start with Windows via HKCU Run + StartupApproved, EXE copy only when enabled
- First-run `config.json` in LocalAppData from `config.example.json`
- Self-contained `dist/Odjezdy.exe` builds and runs without a local SDK
- Source in git; `dist/` and API keys not in git
