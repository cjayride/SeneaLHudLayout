# Progress - SeneaLHudLayout

## Goal
Thunderstore release **1.5.12**: correct listing icon (`Package/icon.png` from `dist/icon.png`); flat zip for upload.

## Status
Released **1.5.12** — committed, tagged `v1.5.12`, pushed to `origin/main`; zip in `dist/`.

## Changes (1.5.12)
- `Package/icon.png` — replaced placeholder with artwork from `dist/icon.png`.
- Version **1.5.12** in `SeneaLHudLayout.csproj`, `SeneaLHudLayoutPlugin.cs`, `Package/manifest.json`.
- `CHANGELOG.md` / `Package/CHANGELOG.md` — 1.5.12 entry.

## Changes (1.5.11)
- `StatsWindow.cs` — status effect remaining time uses `m_ttl - m_time` (Valheim has no `StatusEffect.GetRemainingTime`); removes `[HarmonyX] AccessTools.Method` console warning when Character Stats is open.

## Thunderstore zip
- **Flat archive** (no folders): `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png`, `SeneaLHudLayout.dll` at zip root.
- No `config/` in package.

## Output
- **DLL:** `D:\AI-Agent\Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`
- **Zip:** `D:\AI-Agent\Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\cjayride-SeneaLHudLayout-1.5.12.zip`

## Build
- `dotnet build SeneaLHudLayout.csproj -c Release` — succeeded (existing project warnings only).
