# Progress - SeneaLHudLayout

## Current task (2026-11-09): 1.5.26 self-cleaning config

### Goal
Ship a DLL so that every player's config file (not just the pack copy) arranges itself
properly: entries left behind by the 1.5.23 section rename and by removed settings are
dropped from the file. Acceptance: values of migrated settings are kept, user comments
and all other lines are untouched, a clean file is never rewritten, build produces a
versioned DLL and a Thunderstore zip in /dist.

### Status
Completed through packaging. Compiled and algorithm-verified against real config files.
**Not launched in-game** (game-launch testing needs the user's Valheim session).

### Why a DLL change was the only real fix
BepInEx keeps every config line it did not bind in a private `OrphanedEntries`
dictionary (`IDictionary<ConfigDefinition,string>`, confirmed in the BepInEx 5.4 source
and by the string in BepInEx.dll) and **rewrites it on every save**. So legacy keys are
immortal: `[Character]` (pre-1.5.23 name), `[Performance] SeneaLFpsTweaks` (removed
1.5.18), `[Stats] ValuesBesideLabels` / `LeftOverflow` (moved 1.5.8-era), the old
`[Chat]` window keys (ChatWindowAdjust/Lock/LockKey/ResetKey/ChatAdjustKey, removed in
the chat-window rework). The pack cfg fix only helps the pack; every existing user keeps
the cruft.

### Changes
- **`ConfigHousekeeping.cs` (new)** — runs once from `Awake` after all `Config.Bind`
  calls:
  1. clears the in-memory unrecognized-line list (reflection on `OrphanedEntries`;
     no-op if BepInEx ever renames it — file is still pruned, keys may return at next save);
  2. text-prunes lines whose `[Section] key` is not in `config.Keys`, plus each pruned
     key's doc-comment block, plus section headers left with zero keys; collapses double
     blanks; keeps every other line verbatim. No `config.Save()` (that would rewrite the
     file in BepInEx's canonical form and destroy the player's comments).
  Safety: skips the file if more than 200 unbound lines (config from a very different
  version) and logs; whole pass wrapped in try/catch; clean config => no write at all.
- `SeneaLHudLayoutPlugin.cs` — call after bindings; `VERSION` 1.5.26.
- `SeneaLHudLayout.csproj`, `Package/manifest.json` — 1.5.26.
- `CHANGELOG.md`, `README.md` (1.5.26 entries, copied into `Package/`).
- `beta_cjaycraft/manifest.json` — dependency bump 1.5.25 -> 1.5.26.
- `beta_cjaycraft/CHANGELOG.md` — pack entries for the DLL update and cfg reorg.
- `tools/TestPrune.ps1` — exact port of the algorithm for offline testing (kept).
- `tools/VerifyZip.ps1` — hashes zip entries vs staging files.

### Commands and results
- `dotnet build -c Release` — **compilation succeeded** (`SeneaLHudLayout ->
  bin\Release\net480\SeneaLHudLayout.dll`). The build "FAILED" only on the csproj
  `PostBuild` copy into `D:\SteamLibrary\...\BepInEx\plugins` — a sandbox denial.
  Per AGENTS.md the game install is read-only, so this denial is the desired outcome;
  the game DLL stays at 1.5.25.
- DLL check: `bin\Release\net480\SeneaLHudLayout.dll` FileVersion **1.5.26**; strings
  `ConfigHousekeeping` / `OrphanedEntries` / `Config housekeeping` present.
- `tools/TestPrune.ps1` on profile 4.4.38 cfg (1.5.25-era, 121 keys): 10 unbound found
  (7 `[Character]`, `[Performance] SeneaLFpsTweaks`, 2 `[Stats]`), all removed;
  **111 keys kept, 0 value changes**; `[Character]` and `[Performance]` headers gone,
  `[Stats]` retained (live keys); 703 -> 655 lines; comments intact.
- Same test on the pack cfg (already reorganised): `unbound count: 0` — pure no-op.
- `tools/VerifyZip.ps1` — all 5 zip entries SHA256-match the `Package/` files.

### Deliverables
- **DLL:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll` (1.5.26)
- **Thunderstore zip:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\cjayride-SeneaLHudLayout-1.5.26.zip`
  (CHANGELOG.md, icon.png, manifest.json 1.5.26, README.md, SeneaLHudLayout.dll —
  root layout identical to the 1.5.25 zip)

### Notes for the user
- Existing 1.5.25-era configs (profile 4.4.38, the Gale cache copies) will self-clean on
  first launch of 1.5.26. The pack cfg is already clean, so the mod leaves it alone.
- Not yet done (user's call): upload zip to Thunderstore, git commit/tag, game-launch
  test, installing the new DLL into the Gale test profile (profile 4.4.38 runs 1.5.20,
  `test` runs 1.3.19).

## Prior task (2026-11-09): pack config section cleanup
`beta_cjaycraft/config/cjayride.SeneaLHudLayout.cfg` rebuilt to match 1.5.25 binding
sections (`[HealthFood]` food-bar keys, `[Character Window]`), BepInEx save-format
layout, all 120 values byte-preserved. Backup:
`beta_cjaycraft/config/cjayride.SeneaLHudLayout.cfg.bak-20261109`.

## Prior task (1.5.25)
Chat log auto-scrolls when the input is closed. Released and tagged:
- `dotnet build -c Release` — succeeded
- **DLL:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`
- **Thunderstore zip:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\cjayride-SeneaLHudLayout-1.5.25.zip`
- **GitHub:** https://github.com/cjayride/SeneaLHudLayout/releases/tag/v1.5.25
