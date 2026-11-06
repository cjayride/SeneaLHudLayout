# Progress - SeneaLHudLayout

## Goal
Thunderstore release **1.5.11**: Character Stats no longer spams HarmonyX when resolving status effect remaining time; flat zip for upload.

## Status
Released **1.5.11** — committed, tagged `v1.5.11`, pushed to `origin/main`; zip in `dist/`.

## Changes (1.5.11)
- `StatsWindow.cs` — status effect remaining time uses `m_ttl - m_time` (Valheim has no `StatusEffect.GetRemainingTime`); removes `[HarmonyX] AccessTools.Method` console warning when Character Stats is open.
- Version **1.5.11** in `SeneaLHudLayout.csproj`, `SeneaLHudLayoutPlugin.cs`, `Package/manifest.json`.
- `CHANGELOG.md` / `Package/CHANGELOG.md` — 1.5.11 entry.
- `Package/icon.png` — 256×256 placeholder for Thunderstore (replace with a proper mod icon anytime).

## Thunderstore zip
- **Flat archive** (no folders): `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`, `SeneaLHudLayout.dll` at zip root.
- No `config/` in package.

## Output
- **DLL:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`
- **Zip:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\cjayride-SeneaLHudLayout-1.5.11.zip`

## Build
- `dotnet build SeneaLHudLayout.csproj -c Release` — succeeded (existing project warnings only).
