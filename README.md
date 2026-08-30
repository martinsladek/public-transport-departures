# Odjezdy

A Windows 10 system tray utility that shows the next PID departure from one stop.

- **Hover** — next departure (line, destination, clock time, whole minutes left)
- **Left click** — the same fact in a balloon
- **Icon** — whole minutes remaining, rounded down
- **Right click** — watch, Settings (v1: disabled), Start with Windows, About, Exit

The interface follows the Windows display language (`en`, `cs`). Other languages fall back to English.

### Notes

- v1 is one hardcoded watch in `%LocalAppData%\Odjezdy\config.json`.
- Departures come from the [Golemio PID departure boards](https://api.golemio.cz/pid/docs/openapi/). Put your own free API key in `config.json` (never commit it). Register at https://api.golemio.cz/api-keys
- `stopId` is a PID GTFS pillar id (sloupek), not just a stop name. Lists: [PID open data](https://pid.cz/o-systemu/opendata/), map: [pid.cz/zastavky-pid](https://pid.cz/zastavky-pid/).
- First launch copies [config.example.json](config.example.json) to LocalAppData if the file is missing.

### Building

```powershell
dotnet publish src/Odjezdy/Odjezdy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

Requires the .NET 8 SDK.

Pushing a version tag (`v0.1.0`, `v1.0.0`, …) runs GitHub Actions: it publishes `Odjezdy.exe` and creates a GitHub Release.

### Recreate from the idea

[SPEC.md](SPEC.md) is the full product contract. Give that file to a coding agent and ask it to implement the specification.
