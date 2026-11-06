# Progress - SeneaLHudLayout

## Status
**1.5.1 complete** — Food-bar level recenters on the full timer group.

## Goal
Keep `ShowLevelFoodBar` centered under the food timers when dead vs geared, without extra per-frame mesh work.

## Current status
Completed. Built Release. Packaged Thunderstore zip.

## Files changed
- `CharacterExtras.cs` — world-space union of all `time*` timers; layout stamp; mesh update only when text changes
- `SeneaLHudLayoutPlugin.cs` — version 1.5.1; OffsetX default 0; migrate -33/-63
- `SeneaLHudLayout.csproj`, `Package/manifest.json`, changelogs, packaged cfg

## Commands
- `dotnet build -c Release` — succeeded, 0 errors
- DLL FileVersion 1.5.1

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`
- Zip: `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\cjayride-SeneaLHudLayout-1.5.1.zip`

## Remaining
In-game check: dead login vs geared, OffsetX 0, label under the three food timers.
