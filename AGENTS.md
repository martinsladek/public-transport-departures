# Agent notes

## Releases

Do **not** attach a locally built EXE to GitHub Releases and do not commit `dist/`.

GitHub Actions publishes `PublicTransportDepartures.exe` when a tag matching `v*` is pushed (see `.github/workflows/release.yml`). After a public repository exists, the stable URL is `/releases/latest/download/PublicTransportDepartures.exe`.

To ship a build: commit to `main`, then `git tag vX.Y.Z` and `git push origin vX.Y.Z`. Local try does not need a tag.

## Local try

Stop the running `PublicTransportDepartures.exe` (or leftover `Odjezdy.exe`) first — otherwise `dotnet publish` fails because the file is locked. Then publish to `dist/` and start `dist\PublicTransportDepartures.exe`. That local EXE is only for this machine — never attach it to a GitHub Release.

## Secrets

The optional Golemio API key belongs only in `%LocalAppData%\PublicTransportDepartures\config.json`. Never commit a real key. `config.example.json` stays empty. Do not commit `%LocalAppData%\PublicTransportDepartures\gtfs\` or `dist/`. Older `%LocalAppData%\Odjezdy\` is migrated on launch.

## Build

.NET 8 SDK (`global.json`). Target `net8.0-windows10.0.19041.0`.
