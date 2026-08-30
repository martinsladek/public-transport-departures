# Odjezdy

A Windows 10 system tray utility that shows the next PID departure from one stop.

- **Hover** — next departure (line, destination, clock time, whole minutes left)
- **Left click** — the same fact in a balloon
- **Icon** — whole minutes remaining, rounded down
- **Right click** — switch watches, Settings, About, Exit
- **Settings** — watches, optional Golemio key, Start with Windows, update timetable

Scheduled times come from the [PID GTFS feed](https://data.pid.cz/PID_GTFS.zip) (CC-BY). No account is required. A Golemio API key in Settings is optional and only used for live delays.

The interface follows the Windows display language (`en`, `cs`). Other languages fall back to English.

### Notes

- Watches (from stop / optional line / destination) are edited in Settings and stored in `%LocalAppData%\Odjezdy\config.json`.
- The GTFS zip is downloaded to `%LocalAppData%\Odjezdy\gtfs\` and refreshed about once a day. Only the configured stop pillars are parsed.
- `stopId` is a PID GTFS pillar id (sloupek). Map: [pid.cz/zastavky-pid](https://pid.cz/zastavky-pid/).
- First launch copies [config.example.json](config.example.json) to LocalAppData if the file is missing.

### Building

```powershell
dotnet publish src/Odjezdy/Odjezdy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

Requires the .NET 8 SDK.

Pushing a version tag (`v0.1.0`, `v1.0.0`, …) runs GitHub Actions: it publishes `Odjezdy.exe` and creates a GitHub Release.

### Recreate from the idea

[SPEC.md](SPEC.md) is the full product contract. Give that file to a coding agent and ask it to implement the specification.
