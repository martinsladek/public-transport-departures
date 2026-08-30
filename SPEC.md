# Departures — specification

Hand this file to a coding agent with: **Implement this specification on Windows 10.**

This is the product contract, not a chat log. Follow the decisions below. Do not resurrect rejected ideas from the “Out of scope” section.

## Goal

A tiny Windows 10 desktop utility that lives only in the **notification area** (system tray). It shows the **next departure** of a user-defined PID watch: from one stop pillar, optionally one line, toward one destination. There is no main window.

| Item | Value |
|---|---|
| Product name | **Departures** (English UI). Czech UI: **Odjezdy**. |
| Assembly / EXE name | `PublicTransportDepartures.exe` |
| Config / install folder | `%LocalAppData%\PublicTransportDepartures\` (stable, never localized) |
| Author | Martin Sladek |
| Website | https://www.martinsladek.com/ |
| Repository | https://github.com/martinsladek/public-transport-departures |

A **watch** is “from this pillar, toward this destination” (optional line). The user may define several watches and pick the active one from the tray menu. A later location/profile only selects which watch is active — it does not replace this model.

This is **not** an IDOS-style journey planner (no “any route from A to B”, no transfers).

## Data source

**Pražská integrovaná doprava (PID)** only (Prague and the PID regional network).

### Default — static GTFS (no account)

- Download [https://data.pid.cz/PID_GTFS.zip](https://data.pid.cz/PID_GTFS.zip) (CC-BY, credit PID / ROPID).
- Store the zip under `%LocalAppData%\PublicTransportDepartures\gtfs\`. Do **not** keep the unpacked `stop_times.txt` (~100+ MB) on disk.
- Stream-parse the zip, keep only rows for the configured stop pillars, and resolve upcoming departures locally (calendar, calendar exceptions, GTFS times past 24:00 for night service).
- Refresh the zip about **once a day** (the feed is generated each morning). If the download fails, keep using the last zip.
- First launch with no zip: download in the background. Grey icon and a balloon (“Downloading timetable…”) until a cache exists. Do not freeze the tray.
- Changing watches that introduce a new `stopId` re-parses the local zip; it does not re-download unless the zip is stale or missing.

No user registration. No API key required for scheduled times.

### Optional — Golemio realtime

- If the user pastes their own Golemio API key in Settings, also query Golemio departure boards for the **active** watch (~every 45 seconds).
- When that call returns matching departures, use the **predicted** time (delays). Otherwise keep the GTFS schedule.
- The key lives only in `%LocalAppData%\PublicTransportDepartures\config.json`. Never commit it, never bake it into the EXE.
- Do **not** ship a shared key, and do **not** proxy Golemio through a server without written permission from the provider.
- End users must not be required to register. The key is an optional extra for delays.
- Settings shows a **?** next to **Optional live delays**. That opens a short help dialog (optional live delays; one link to https://api.golemio.cz/api-keys; confirm the email if new, create a key, paste it here). After the dialog closes, focus the key field. Do **not** build a multi-step register/login wizard.

### Identity

- Stop identity is the PID **GTFS `stop_id`** of a concrete **stop pillar** (sloupek), for example `U1040Z101P`. A stop name alone is not a watch.
- Users pick that pillar in Settings by typing the stop **name** (diacritics-insensitive), choosing one of the few pillars at that stop, then optionally a line and a destination from that pillar’s catalogue.
- Catalogue: [https://data.pid.cz/stops/json/stops.json](https://data.pid.cz/stops/json/stops.json), cached under `%LocalAppData%\PublicTransportDepartures\gtfs\stops.json`. Refresh on the same daily cadence as the GTFS zip. If the download fails, keep using the last file.
- Backup map: [pid.cz/zastavky-pid](https://pid.cz/zastavky-pid/). Do not dump every stop in the region into one combo box.

## Watch model

`%LocalAppData%\PublicTransportDepartures\config.json`:

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

If an older build left data in `%LocalAppData%\Odjezdy\`, move that folder to `%LocalAppData%\PublicTransportDepartures\` (config, `gtfs`, leftover EXE). If `config.json` is still in `%AppData%\Odjezdy\`, move it into the current LocalAppData folder and remove the empty roaming folder. Rewrite a leftover Run value named `Odjezdy` to `PublicTransportDepartures`.

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

Visual sections as `GroupBox` blocks with space between them (not HTML-style horizontal rules):

1. **Watches** — list, **Add**, **Edit**, **Remove**.
2. **Live delays** — first line: **Optional live delays** with a **?** help button beside it. Next line: **Golemio API key** and the key field on the same row (password-style is fine; the box fills the remaining width). Empty key = scheduled GTFS only.
3. **App startup** — one checkbox: **Start app with Windows**. Read live registry state when the dialog opens. Apply the change only on OK (Cancel leaves autostart as it was).
4. **Timetable** — short note that PID publishes a new timetable each morning (around 4:00) and this app downloads it about once a day, then **Update timetable** (GTFS and stop-catalogue now).

OK saves `config.json` and reloads departures. Cancel discards in-dialog edits.

### Add / Edit watch

A second modal (`FixedDialog`, sized to its content — labels and OK/Cancel fully visible). Do not clip buttons with a too-small fixed height.

1. Type a stop **name**. Search is diacritics-insensitive (`andel` finds Anděl). Do not list every stop in one combo.
2. Pick a matching stop (typically a short list).
3. Pick a **pillar** (typically 2–8). This sets `stopId`.
4. Optionally pick a **line** and a **destination** from that pillar’s catalogue (`routeShortName`, `headsignContains`). Empty = any.
5. Optional **label**. If empty, use `{stop name} · {line}` or the stop name.
6. A link to the PID stop map as a backup, plus an optional “or paste a GTFS pillar id” field for map users and catalogue outages. That field is not the primary path.

### About dialog

Standard modal WinForms dialog (`FixedDialog`, no maximize/minimize, not in the taskbar). Product name is **Departures** in English and **Odjezdy** in Czech. Tagline, credit, data credit, and link labels come from `Strings.cs` for the current UI language.

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

OK button closes the dialog. GitHub and OK stay untranslated.

### Language

Read `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName`. UI strings live in `Strings.cs`. Supported:

`en` (default), `cs`

Any other Windows language falls back to English. Product name is **Departures** in English and **Odjezdy** in Czech. README, SPEC, and GitHub stay English only.

## Autostart

Optional, off by default. No admin rights. Portable until the user opts in.

**Enable**

1. Copy the currently running EXE to `%LocalAppData%\PublicTransportDepartures\PublicTransportDepartures.exe` (skip if already running from that path).
2. Write `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` value `PublicTransportDepartures` = quoted path to that copy.
3. Write `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run` value `PublicTransportDepartures` as enabled (`0x02…`), so Task Manager / Settings → Apps → Startup agree. Remove leftover `Odjezdy` Run / Approved values.

**Disable**

1. Delete the Run value and the StartupApproved value. Autostart is off immediately (next logon will not start the app). Keep `config.json` and the GTFS zip.
2. Delete the installed EXE now if this process is **not** that file.
3. If this process **is** the installed EXE, Windows will not delete a running image. Mark it for deletion and remove it ~1s after Exit via `cmd timeout & del`. Re-checking **Start app with Windows** before Exit cancels that deletion.

On a later portable launch, if Run is not registered, delete any leftover installed EXE.

**Checkbox state**

The Settings checkbox is checked only if the Run value exists **and** StartupApproved does not mark it disabled (`0x03` / `0x07`). Task Manager “Disable” leaves Run in place; Settings must not show checked in that case. Checking the box again and saving OK re-enables Approved.

**Updates**

On launch, if Run is registered and this process is a different file than the installed copy (size or last-write), overwrite the LocalAppData EXE so the next logon is not an old download.

## Process

Single instance via mutex `Local\PublicTransportDepartures.SingleInstance`. A second launch exits silently.

## Technical stack (required)

| Layer | Choice |
|---|---|
| Language | C# |
| UI | WinForms, `ApplicationContext` + `NotifyIcon` (no main form) |
| Target | `net8.0-windows10.0.19041.0` |
| Output | `WinExe`, self-contained single-file `win-x64` |
| HTTP | `HttpClient` for the PID GTFS zip, the PID stops catalogue, and optional Golemio. No third-party transit SDK. |

Publish:

```powershell
dotnet publish src/Departures/Departures.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist
```

The published EXE must run on a PC that has no .NET SDK and no extra runtimes installed.

Do not use trimming (`PublishTrimmed=false`).

No installer. Distribution of the binary is **GitHub Releases**, never git. A Release is created by GitHub Actions on `windows-latest` when a tag matching `v*` is pushed (`PublicTransportDepartures.exe` asset). The `/releases/latest/download/PublicTransportDepartures.exe` URL then points at that Release.

## Suggested layout

```
src/Departures/
  Departures.csproj
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
  GolemioHelpForm.cs
  TrayDialog.cs
  DepartureClock.cs
  DepartureSelector.cs
  GtfsTimetable.cs
  StopCatalog.cs
  TextSearch.cs
  GolemioClient.cs
src/Departures.Tests/
```

`.gitignore`: `bin/`, `obj/`, `dist/`, `.vs/`, `*.user`, `config.json`, `.env`.

README is English only. Public download: [https://github.com/martinsladek/public-transport-departures/releases/latest/download/PublicTransportDepartures.exe](https://github.com/martinsladek/public-transport-departures/releases/latest/download/PublicTransportDepartures.exe). Merge to `main` does not create a Release; only a `v*` tag does.

## Out of scope (do not implement)

These were considered and deferred or rejected:

- Windows Location / GPS / geofencing
- Multiple locations / behavior profiles (a location will later only select which watch is active)
- IDOS-style journey planner, transfers, or “any path from A to B”
- Golemio multi-step register / login wizard (the **?** help and one portal link are enough)
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
- Settings dialog: grouped sections; add/edit/remove watches by stop name then pillar; optional Golemio key with short **?** help; App startup checkbox; update timetable
- About and Settings are single-instance (`TrayDialog.ShowOnce`)
- Scheduled departures work with no API key (local GTFS)
- Optional Golemio key overlays predicted times when present
- About dialog with website, GitHub, and PID CC-BY credit
- UI localized for `en` and `cs` (other Windows languages fall back to English)
- Optional Start with Windows via HKCU Run + StartupApproved, EXE copy only when enabled
- First-run `config.json` in LocalAppData from `config.example.json`
- Self-contained `dist/PublicTransportDepartures.exe` builds and runs without a local SDK
- Source in git; `dist/`, GTFS zip, and API keys not in git
