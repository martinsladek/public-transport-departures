# Odjezdy — specification

Hand this file to a coding agent with: **Implement this specification on Windows 10.**

This is the product contract, not a chat log. Follow the decisions below. Do not resurrect rejected ideas from the “Out of scope” section.

## Goal

A tiny Windows 10 desktop utility that lives only in the **notification area** (system tray). It shows the **next departure** of a user-defined PID watch: from one stop pillar, optionally one line, toward one destination. There is no main window.

| Item | Value |
|---|---|
| Product name | Odjezdy |
| Assembly / EXE name | `Odjezdy.exe` |
| Config / install folder | `%LocalAppData%\Odjezdy\` (stable, never localized) |
| Author | Martin Sladek |
| Website | https://www.martinsladek.com/ |
| Repository | https://github.com/martinsladek/public-transport-departures |

A **watch** is “from this pillar, toward this destination” (optional line). The user may define several watches and pick the active one from the tray menu. A later location/profile only selects which watch is active — it does not replace this model.

This is **not** an IDOS-style journey planner (no “any route from A to B”, no transfers).

## Data source

**Pražská integrovaná doprava (PID)** only (Prague and the PID regional network).

### Default — static GTFS (no account)

- Download [https://data.pid.cz/PID_GTFS.zip](https://data.pid.cz/PID_GTFS.zip) (CC-BY, credit PID / ROPID).
- Store the zip under `%LocalAppData%\Odjezdy\gtfs\`. Do **not** keep the unpacked `stop_times.txt` (~100+ MB) on disk.
- Stream-parse the zip, keep only rows for the configured stop pillars, and resolve upcoming departures locally (calendar, calendar exceptions, GTFS times past 24:00 for night service).
- Refresh the zip about **once a day** (the feed is generated each morning). If the download fails, keep using the last zip.
- First launch with no zip: download in the background. Grey icon and a balloon (“Downloading timetable…”) until a cache exists. Do not freeze the tray.
- Changing watches that introduce a new `stopId` re-parses the local zip; it does not re-download unless the zip is stale or missing.

No user registration. No API key required for scheduled times.

### Optional — Golemio realtime

- If the user pastes their own Golemio API key in Settings, also query Golemio departure boards for the **active** watch (~every 45 seconds).
- When that call returns matching departures, use the **predicted** time (delays). Otherwise keep the GTFS schedule.
- The key lives only in `%LocalAppData%\Odjezdy\config.json`. Never commit it, never bake it into the EXE.
- Do **not** ship a shared key, and do **not** proxy Golemio through a server without written permission from the provider.
- End users must not be required to register. The key is an optional extra for delays.

Register a personal key at https://api.golemio.cz/api-keys (optional).

### Identity

- Stop identity is the PID **GTFS `stop_id`** of a concrete **stop pillar** (sloupek), for example `U1040Z101P`.
- A human stop name is not enough. Lists: [PID open data](https://pid.cz/o-systemu/opendata/), map: [pid.cz/zastavky-pid](https://pid.cz/zastavky-pid/).

## Watch model

`%LocalAppData%\Odjezdy\config.json`:

```json
{
  "golemioApiKey": "",
  "activeWatchId": "andel-b",
  "watches": [
    {
      "id": "andel-b",
      "label": "Anděl · B",
      "stopId": "U1040Z101P",
      "routeShortName": "B",
      "headsignContains": ""
    }
  ]
}
```

| Field | Rule |
|---|---|
| `id` | Stable id. `activeWatchId` points at one watch. |
| `label` | Menu text. If empty, show `{route} → {headsign}` or the stop id. |
| `stopId` | Required. GTFS pillar id (“from”). |
| `routeShortName` | Optional. Exact line label (`22`, `B`, `S9`). Empty = any line at that pillar. |
| `headsignContains` | Optional. Case-insensitive substring of the trip headsign (“to”). Empty = any direction. |

On first launch, if `config.json` is missing, write a copy of the repo `config.example.json` into LocalAppData. Do not overwrite an existing file.

If an older build left `config.json` in `%AppData%\Odjezdy\`, move it to LocalAppData on first load and remove the empty roaming folder.

Do not use Roaming: the EXE is large, and the optional API key is machine-local.

## Behavior

### Icon

Draw a simple 32×32 tray icon at runtime (filled circle + text). Do not ship third-party icon assets.

| State | Look | Meaning |
|---|---|---|
| Has a future departure | Windows blue (`#0078D7`) fill, white digits | Next matching departure is known |
| No departure / not configured / download in progress / no usable cache | Grey filled circle (`#707070`), white `—` | Nothing to count down |

When a future departure exists, the circle shows **whole minutes remaining, rounded down**:

- **1 minute** means at least 1:00.0 and less than 2:00.0 (1:00.0 through 1:59.9…).
- **0** means at least 0:00.1 and less than 1:00.0.
- At the exact departure instant (remaining ≤ 0), that trip is gone. Show the next matching cached departure, or grey `—` if none.
- 100 minutes or more: show `99`.

The displayed number must change at the **second** the remaining time crosses a minute boundary (for example 2:00.0 → still `2`; 1:59.9 → `1`). Schedule the next icon/tooltip refresh for that instant. Do **not** drive the number from a loose one-minute poll, and do not call the network every second.

### Hover

Native tooltip only. No menu on hover. `NotifyIcon.Text` is limited to 63 characters — truncate.

Examples:

- `B → Černý Most  05:12 (3 min)`
- `Downloading timetable…`
- `No upcoming departure`
- `Add a stop in Settings`

### Left click — balloon

Show a balloon with the same fact as the tooltip, but without the 63-character limit. Include clock time and whole minutes remaining.

If nothing is configured, tell the user to open Settings.

### Right click — native context menu

No flyout (custom popup above the taskbar). Use `ContextMenuStrip`.

```
✓  Anděl · B
   Home · 22
─────────────────
   Settings
─────────────────
   About
   Exit
```

Rules:

- List configured watches. Checkmark = `activeWatchId`. Clicking a row selects it, saves it, and refreshes the icon.
- If the list is empty, show a disabled “No stop configured” row.
- **Settings** opens the Settings dialog (or activates it if it is already open).
- **About** sits immediately above **Exit**. Opening About again activates the existing dialog — do not stack a second copy. The same rule applies to Settings. A tray app keeps pumping messages during `ShowDialog`, so a second click would otherwise open another window. Use a single-instance helper (`TrayDialog.ShowOnce`).
- **Exit** hides the tray icon and quits.

### Settings dialog

A simple modal WinForms dialog (`FixedDialog`, not in the taskbar). No main window besides this and About.

- List of watches. **Add**, **Edit**, **Remove**.
- Add/Edit fields: label, **from** (`stopId`), optional **line**, optional **to** (headsign substring).
- Optional Golemio API key (password-style or plain text is fine). Empty = scheduled GTFS only.
- **Start with Windows** checkbox. Read live registry state when the dialog opens. Apply the change only on OK (Cancel leaves autostart as it was).
- **Update timetable** starts a GTFS download now (otherwise the daily refresh is enough).
- OK saves `config.json` and reloads departures. Cancel discards in-dialog edits.
- The Add/Edit watch dialog must size to its content (labels and OK/Cancel fully visible). Do not clip buttons with a too-small fixed height.

v2 does **not** include a stop-name search picker. The from field is the GTFS pillar id; the dialog may show a short hint and the PID map URL.

### About dialog

Standard modal WinForms dialog (`FixedDialog`, no maximize/minimize, not in the taskbar). Product name stays **Odjezdy**. Tagline, credit, data credit, and link labels come from `Strings.cs` for the current UI language.

English canonical copy:

> A lightweight Windows desktop utility that shows the next PID departure from one stop.
>
> Timetables: PID open data (CC-BY).
>
> Developed by Martin Sladek with the help of AI models and workflows.

Clickable links (labels are localized; URLs are not):

| Role | URL |
|---|---|
| Website | https://www.martinsladek.com/ |
| GitHub | https://github.com/martinsladek/public-transport-departures |

OK button closes the dialog. Product name, GitHub, and OK stay untranslated.

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

1. Delete the Run value and the StartupApproved value. Autostart is off immediately (next logon will not start the app). Keep `config.json` and the GTFS zip.
2. Delete the installed EXE now if this process is **not** that file.
3. If this process **is** the installed EXE, Windows will not delete a running image. Mark it for deletion and remove it ~1s after Exit via `cmd timeout & del`. Re-checking Start with Windows before Exit cancels that deletion.

On a later portable launch, if Run is not registered, delete any leftover installed EXE.

**Checkbox state**

The Settings checkbox is checked only if the Run value exists **and** StartupApproved does not mark it disabled (`0x03` / `0x07`). Task Manager “Disable” leaves Run in place; Settings must not show checked in that case. Checking the box again and saving OK re-enables Approved.

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
| HTTP | `HttpClient` for the PID GTFS zip and optional Golemio. No third-party transit SDK. |

Publish:

```powershell
dotnet publish src/Odjezdy/Odjezdy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist
```

The published EXE must run on a PC that has no .NET SDK and no extra runtimes installed.

Do not use trimming (`PublishTrimmed=false`).

No installer. Distribution of the binary is **GitHub Releases**, never git. A Release is created by GitHub Actions on `windows-latest` when a tag matching `v*` is pushed (`Odjezdy.exe` asset). The `/releases/latest/download/Odjezdy.exe` URL then points at that Release.

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
  SettingsForm.cs
  WatchEditForm.cs
  TrayDialog.cs
  DepartureClock.cs
  DepartureSelector.cs
  GtfsTimetable.cs
  GolemioClient.cs
src/Odjezdy.Tests/
```

`.gitignore`: `bin/`, `obj/`, `dist/`, `.vs/`, `*.user`, `config.json`, `.env`.

README is English only. Do not put a download link in README until a public Release binary exists.

## Out of scope (do not implement)

These were considered and deferred or rejected:

- Windows Location / GPS / geofencing
- Multiple locations / behavior profiles (a location will later only select which watch is active)
- IDOS-style journey planner, transfers, or “any path from A to B”
- Stop-name search picker (type the GTFS pillar id in v2)
- More than one *active* watch at a time
- Other Czech regions (IDS JMK, IREDO, …)
- Walking time to the stop (`walkMinutes`)
- Shipping or proxying a shared Golemio key
- Requiring every user to register at Golemio
- Custom tray flyout instead of a native context menu
- Java, Python, Node, C++ toolchains
- Putting the self-contained EXE or the GTFS zip into git
- Installer

No administrator rights are required.

## Definition of done

- Tray icon: blue + floored minutes, or grey `—`
- Displayed minutes change at the correct second
- Hover tooltip and left-click balloon
- Right-click menu lists watches and selects the active one
- Settings dialog: add/edit/remove watches, optional Golemio key, Start with Windows, update timetable
- About and Settings are single-instance (`TrayDialog.ShowOnce`)
- Scheduled departures work with no API key (local GTFS)
- Optional Golemio key overlays predicted times when present
- About dialog with website, GitHub, and PID CC-BY credit
- UI localized for `en` and `cs` (other Windows languages fall back to English)
- Optional Start with Windows via HKCU Run + StartupApproved, EXE copy only when enabled
- First-run `config.json` in LocalAppData from `config.example.json`
- Self-contained `dist/Odjezdy.exe` builds and runs without a local SDK
- Source in git; `dist/`, GTFS zip, and API keys not in git
