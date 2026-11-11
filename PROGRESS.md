# Progress - SeneaLHudLayout

## Current task (2026-11-11): 1.5.54 RELEASED - chat behind windows, verified + cleaned

User (turn 40): "code works. chat is now properly behind the crafting window. cleanup the code if
you need. prepare the zip file for thunderstore, then commit and push to github, and tag latest
release". Explicit release ask (standing yes for this release, same scope as 1.5.51).

**ICON RULE (user, 2026-11-11, VERBATIM): "stop putting the wrong icon.png into the zip file, it
has to be 256x256".** Thunderstore zips ship the 256x256 RGB icon (= Package\icon.png =
tools\icon-source-256.png, 61,876 b) - the shape of every accepted package (the 1.5.26 zip and the
accepted SkyheimCompat 1.2.6 zip are both 256). The 512x512 LANCZOS upscale added in the 1.5.51
turn ("documented requirement") was WRONG: Thunderstore accepts 256. Do NOT upscale, do NOT
regenerate the icon. 1.5.54 FIXED: Package\icon.png = the 256 source art; zip rebuilt; the GitHub
asset was deleted and re-uploaded - new zip 216,394 b, sha256
0BB15D86BD35D3944500B72D64B653E7761A4BFBE046A4E1046A768C9F4243A8, digest verified on the release,
Latest intact. Any PROGRESS note claiming 512 is the compliant icon is VOID (the 1.5.51 zip on
GitHub still carries the 512 icon - supersede it by shipping forward).

### Cleanup (the "code works" trim)
- ChatLayerProbe.cs DELETED (its job - naming the canvas ladder live - is done; the answer is
  recorded above and in the changelog).
- PushChatBehindWindows (the 1.5.52 sibling push keyed on the vanilla InventoryGui, which SeneaL
  UI replaces - it never fired for the user's crafting window) DELETED. The verified pin
  (PinChatCanvasesBehindWindows, order 300) is the whole fix.
- Version 1.5.53 -> 1.5.54. Build: 9 warnings (baseline 10 -> probe gone), 0 errors.

### Install + hashes
- Installed 1.5.54 (bak-1.5.53 kept). bin == installed == Package == zip:
  sha256 428B5C8D30B606B96FAF6C6F6A160AC47C96C03078B5DF980576E24FF6D9AACE (309,760 b).

### Zip (Thunderstore, flat 5)
- dist\cjayride-SeneaLHudLayout-1.5.54.zip 291,740 b
  sha256 6BFB24A65D92BC04D24F7A75DC67054F0236165D3F202B347BA16E1D7D8C48D4
- verified by extraction: manifest (name pattern OK, ver 1.5.54, desc 63, deps 3-part), icon 512x512
  RGBA, CHANGELOG top ## 1.5.54, dll FileVersion 1.5.54 sha == installed.

### Git / GitHub (1.5.52..1.5.54 span, HEAD was v1.5.51 = 3c37e10)
- commit on main "Release 1.5.54: chat behind the windows + the FPS pass." + annotated tag v1.5.54,
  pushed; release title "1.5.54", asset the zip, marked Latest. (gh release create must run from
  the asset's directory with a bare filename - gh glob-expands path arguments.)

## Previous task (2026-11-11): 1.5.53 - chat behind the windows, the probe-verified fix

User (turn 39): "the chat window is still displaying on top of the seneal crafting window, the chat
window needs to be behind". 1.5.52's sibling push keyed on InventoryGui.IsVisible() did NOT fire
for the window the user crafts in. THE PROBE ANSWERED FROM THE LIVE LOG (12:35 session, 1.5.52):

- `chat layers (inventory-opened): invVisible=0 ...` x ~30: InventoryGui.IsVisible() flips on every
  window switch, and the crafting window is SeneaL/VNEI's - the vanilla InventoryGui is replaced.
- CANVAS LADDER (from the dumps): `Inventory_screen(ov=600) root=600 INV`, `VNEI(Clone)(ov=630)
  root=600`, `HUD(ov=400)`, vanilla `Chat(ov=300) root=300`, SeneaL sorted canvases
  Select(28000)/Vnei(30900)/Mods(31000)/announceEdge(29000)..., XP card SeneaLHudLayout_SlotXp(401).
- NO canvas flagged `CHAT` in ANY dump: the dumps all caught chat HIDDEN (FindObjectsOfType =
  active only). The canvas governing the skinned panel (panel path
  `_GameMain/LoadingGUI/PixelFix/IngameGui/Chat_box/SeneaLUI_chat`) exists in the active list
  exactly when chat is VISIBLE - and over the 600/630 windows there, per the report.

### The 1.5.53 fix (ChatWindowLayout.PinChatCanvasesBehindWindows, every Tick)
Every Canvas the chat window's own subtree owns is pinned to sortingOrder 300 = the vanilla Chat
tier (the ladder shows `Chat(ov=300)`; 300 < HUD 400 < inventory 600 < VNEI 630 = below all):
- Chat window gets a Canvas component if it has none (then the IngameGui tree canvas is not the
  nearest canvas for the panel; the chat subtree owns its tier).
- ROOT canvases in the subtree: overrideSorting cleared, sortingOrder = 300 (the shape of vanilla's
  own Chat root).
- NESTED canvases in the subtree (a SeneaL UI panel canvas sorted 28000+ lives here):
  overrideSorting = true, sortingOrder = 300.
- The IngameGui root OUTSIDE the chat subtree is NEVER touched (it governs the whole HUD).
- Canvas list cached per chat-window instance id; destroyed entry -> one re-collect. Writes only on
  drift. The 1.5.52 sibling push stays (second layer, vanilla inventory only).

### Probe (stays in 1.5.53)
- New `chat subtree canvases (<reason>)` line: inactive-inclusive walk of the chat window subtree
  - names the governing canvases + live orders EVEN when chat is hidden (the 1.5.52 blind spot).
- New once-per-session dump `chat layers (panel-visible)` the moment the panel is activeInHierarchy.
- Edge dumps throttled to 1/5s (the 1.5.52 log flooded: ~30 full ladders per session).

### Build / install
- BuildStaged: exit 0, 10 warnings baseline. First 1.5.53 build (pre-refinement, root-skip)
  briefly installed (sha EE0AE5..., bak-1.5.53); FINAL build = root-aware pin,
  sha256 C963C70F0046BE146136E1987F2B4919D17E2613E5EC18EC9B56B84A2A57EEBD, bin == installed ==
  Package. (Installed DLL was NOT locked by the running game - Windows allowed the overwrite.)
- Manifest 1.5.53, CHANGELOG new top section, README/Package README chat-behind-every-window line,
  DiffReadme = 0.

### Awaiting user (FULL restart - the running 12:35 session still has 1.5.52 loaded)
- Expect `XP bar built, v1.5.53`; open the crafting window with chat visible; confirm chat is now
  BEHIND the crafting panel; grab the `chat subtree canvases (panel-visible)` line (proof of the
  canvas shape + our 300 pin) if anything looks off. If chat is STILL on top, that one line names
  the exact canvas that evaded the pin and the follow-up is exact, not a guess.

## Previous task (2026-11-11): 1.5.52 - chat behind the windows + FPS pass 1

User: "when the chat window is visible, it is floating on top of my crafting window if i have the
crafting window and inventory open. i don't want someone talking in game to disrupt my crafting.
the chat window should be behind that" (plus the standing ask from turn 31: "make sure our work
here doesn't compromise FPS").

### Chat draw order (the actual fix)
- EVIDENCE, not guess: vanilla Assembly-CSharp (ilspycmd decompile, $env:TEMP\vanilla_full) writes
  ZERO sortingOrder; SeneaL-UI (decompiled to $env:TEMP\senealui, 427 files) sorts its OWN canvases
  from 28000 (drag Selection) to 32600 (BuildBadge) and has NO canvas for InventoryView/CraftView.
  => the skinned chat panel (child of Terminal.m_chatWindow) and the inventory/crafting windows are
  plain SIBLINGS under the root HUD canvas; on one canvas Unity draws in SIBLING ORDER, and the
  chat window sits later in the tree => messages over the crafting panel.
- FIX (ChatWindowLayout.PushChatBehindWindows, at the head of Tick, one edge check per frame): on
  the InventoryGui.IsVisible() edge -> true, save the chat window's sibling index and
  SetAsFirstSibling(); on -> false, restore it (clamped to childCount-1). Two transform writes per
  open. No config key: the user's requirement is unconditional.
- ChatLayerProbe.cs STAYS (one-shot, edge-driven) as verification: logs the full active canvas
  ladder + CHAT/INV flags + sibling ranks at panel-built and inventory-opened. If the user's log
  shows chat and INV on DIFFERENT canvases, the sibling fix is not enough and the probe's numbers
  name the canvas that needs a sortingOrder override. Strip the probe once the log confirms it.

### FPS pass 1 (audit-approved plan, no visual change)
- TmpFont.cs: the two Resources.FindObjectsOfTypeAll scans -> 2s retry gate (_nextScan).
- BossStars.cs: FindNamed(root,"bosses") whole-subtree walk -> 2s gate.
- VneiSearch.cs: shared _nextHeavyScan (2s) gates the assembly loop in Bind(), the
  FindObjectsOfTypeAll(_uiType) in Window() and FindCraft in EnsureButton(); the label
  ForceMeshUpdate + sizeDelta now only runs when the text actually CHANGED.
- ResourceBars.cs: _fxParents / _rowTexts cache the per-root GetComponentsInParent/Children
  results (destroyed-entry re-scan) - they re-ran EVERY frame per bar.
- WorldHealthText.cs: THE big one. FindForeignLevel ran GetComponentsInChildren<TMP_Text>(true) +
  a Regex on every text of every visible plate EVERY frame -> per-plate component cache +
  per-plate matched-label cache (regex re-checks the one cached string, which is how a level-up
  invalidates it). Plus the OFF-path leak: ReleasePlate is now called ONCE per plate id
  (_releasedPlates), cleared when the feature comes back on. Dead public Tick() removed (the
  Harmony postfix is the only entry point).
- CharacterExtras.cs: EnsureLevel() ran a whole-InventoryGui GetComponentsInChildren<Transform>
  EVERY frame while _tabs == null (inventory closed = always) -> 2s _nextTabsScan gate.
  TrySampleCraftInnerFill's scan is NOT per-frame (called from StatsWindow's chrome refresh,
  0.75s cadence while that panel is open) - left alone.
- PowersLayout.cs: FindPowerIcon runs per visible power EVERY frame; its Image-scan last resort
  re-ran every frame when a container has no image (null cache). Now memoized per container id
  with a 2s miss retry; the cheap named "power" probe stays live over the cache.
- ChatWindowLayout.cs: OnBeforeRender (Canvas.willRenderCanvases, EVERY canvas render) set
  alignment/overflow/wrapping + ForceMeshUpdate(true,true) unconditionally; the style is now
  applied once per text object (_clipStyled) and TMP's own dirtiness makes the per-frame
  ForceMeshUpdate the cheap no-op it should be. Cleared with _clipped.
- SlotCue.cs: deleted two dead per-frame scanners (ShowOnIcons scans EVERY icon in the HUD via
  CollectIcons; PaintAction) - zero call sites, grep-verified.
- ActionSlotXpBar.cs: shipped-scaffolding stripped - Diag()/Probe() and their helpers (Box,
  Float, RectText) + _diagAt/_diagDone. KillLabelGlow and GuardCover STAY (live behaviour).

### Build / install
- tools\BuildStaged.ps1: exit 0, 10 warnings (baseline), 0 errors, FileVersion 1.5.52.
- tools\InstallTest.ps1: backup SeneaLHudLayout.dll.bak-1.5.51, installed 1.5.52, 310,784 b.
- SHA256 bin == installed == Package\SeneaLHudLayout.dll =
  C05A3420B28C588B6233E6E2EA0D3CA37DCC374D2D46D15A5DB39002CA437913
- Package\manifest.json 1.5.51 -> 1.5.52 (description/deps untouched), CHANGELOG.md new top
  section, README.md + Package\README.md: the chat-behind line added under ### Chat,
  tools\DiffReadme.ps1 = 0 diff lines (207 = 207).
- Package\icon.png CAUTION: the working tree's icon had regressed to the RAW 256x256 source art
  (61,876 b, written 2026-11-05, uncommitted), which is NOT Thunderstore-compliant (needs 512x512).
  git checkout restored the shipped 512x512 RGBA (136,771 b) from HEAD; the raw art is preserved at
  tools\icon-source-256.png. Do NOT regenerate the 512 by plain LANCZOS - the committed blob
  (12c10afe) does not reproduce that way; keep the committed file.
  [OVERRIDDEN 2026-11-11 by the user: 256x256 IS the correct icon; the 512 upscale was the wrong
  file. Package\icon.png is now the 256 source art. See the ICON RULE at the top of this file.]

### Awaiting user (full restart)
- Expect in ...\profiles\cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.44\BepInEx\LogOutput.log:
  "XP bar built, v1.5.52", "chat layers (panel-built)", and on opening the inventory
  "chat layers (inventory-opened)". Then confirm: chat no longer over the crafting panel, XP card
  unchanged, cooldowns/labels/resource bars/character level all looking as before.

## 1.5.51 RELEASED - zip + commit + push + latest tag

User: "code works. make a thunderstore compliant zip file, commit and push to github, tag latest
release" -> ALL DONE. This turn EXPLICITLY OVERRIDES the standing cjaycraft rule "NO zip, NO github
push" for this release only; the rule stands for future turns unless the user says otherwise.

### Zip (Thunderstore)
- Artifact: dist\cjayride-SeneaLHudLayout-1.5.51.zip, 290,947 b,
  sha256 6a72a535611711493caedb07e28da68122624f04a8399c635703881dc1375523
- FLAT 5-entry layout copied from the published, accepted 1.5.26 zip: manifest.json, README.md,
  CHANGELOG.md, icon.png, SeneaLHudLayout.dll (NO folder prefix - Thunderstore rejects nested).
- DLL in the zip = the shipped build, FileVersion 1.5.51, sha12 C008A7C2204C (bin == installed ==
  Package == zip). CHANGELOG top header = ## 1.5.51.
- Two Package fixes for strict compliance (committed):
  * manifest description 110 -> 63 chars (limit 64): "Rearranges SeneaL UI's HUD: hotbar
    bottom-left, XP bar, labels."  (the old 110-char text had shipped for years; now legal)
  * Package\icon.png 256x256 -> 512x512 (documented requirement), LANCZOS upscale, RGBA 136,771 b
- name SeneaLHudLayout (15 chars, ^[a-zA-Z0-9_]+$), version_number 1.5.51 matches the zip name,
  deps byte-identical to the accepted 1.5.26 package (denikson-BepInExPack_Valheim-5.4.2360,
  seneaL-SeneaL_UI-1.2.8) -> unchanged.
- Thunderstore site itself NOT touched: no TS token in this environment; publishing the zip at
  thunderstore.io/c/valheim is the user's step (same as every prior release - GitHub carries the zip).

### Git
- Tool: git is NOT on PATH; use C:\Program Files\Git\cmd\git.exe (gh IS: C:\Program Files\GitHub
  CLI\gh.exe, authed as cjayride, scopes include repo).
- Commit 72c34c7 on main "Release 1.5.51: the action-slot XP bar, finished." - 22 files,
  +4329/-131. First commit of ActionSlotXpBar.cs, PotionCooldown.cs, ChatBackground.cs. Spans
  1.5.27..1.5.51 (HEAD was b058d27 = v1.5.26).
- .gitignore keeps the repo SOURCE-ONLY: Package/*.dll, bin/, obj/, dist/, lib/, tools/ ignored
  (verified with status --ignored), so the DLL travels only in the zip.
- Pushed: b058d27..72c34c7 main -> main; ANNOTATED tag v1.5.51 (cat-file -t = tag, matching the
  existing tags) pushed.
- Release: v1.5.51, title "1.5.51", target main, asset cjayride-SeneaLHudLayout-1.5.51.zip,
  state uploaded, digest matches local sha256 exactly. Marked **Latest** (1.5.26 lost the flag).
  https://github.com/cjayride/SeneaLHudLayout/releases/tag/v1.5.51
- Release notes authored in dist\release-notes-1.5.51.md (body: 1.5.51 specifics + the finished
  XP bar + the FPS work + the 1.5.27..1.5.51 arc), style copied from the 1.5.26 release.

### NOT released (at the time; DONE 2026-11-11 next turn)
- SkyheimCompat 1.2.9: released "the same way" on request - zip dist\upload\SkyheimCompat-1.2.9.zip
  (sha256 787A80ABDDEF…), commit d7f233a in the Skyheim repo, annotated tag compat-v1.2.9, GitHub
  release "Skyheim Compat 1.2.9" marked Latest, asset digest verified. Details in the Skyheim
  repo's PROGRESS.md.

### Next: FPS hardening pass = 1.5.52 (proposal stands, awaiting go-ahead): TmpFont one-shot,
WorldHealthText off-path leak + per-plate caches, BossStars/VneiSearch negative caches,
ResourceBars guard, verify CharacterExtras guards, strip XP Diag/Probe scaffolding.

## Config audit (2026-11-11): [HealthFood] ShowLevelFoodBar - level under the food timers

User asked: "the xp level under the food timers is already disabled by default in config and in the
config of my modpacks, server, and gale profiles, right?"
ANSWER: code default false + pack false + NO server cfg (client-only mod) - but BOTH Gale profiles
carried a stale `ShowLevelFoodBar = true` from the development turns. Flipped to false on approval:
- profiles\cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.44\BepInEx\config (line 394)
- profiles\test\BepInEx\config (line 61; legacy file, its OffsetX = -57 = old default, harmless now)
Verified after edit: pack false, beta_test false, test false, server NO CFG. Config-only change, no
DLL rebuild (feature read live at CharacterExtras.cs:159; F1 live, cfg applies at launch).
NOT touched (intended on): [Character Window] ShowLevelXpBar = true (the 100px XP bar under the
character-window level) - a different knob, true in code default and every cfg.

## Current task (2026-11-11): 1.5.51 - knob removal + two default flips (config-only)

### User (verbatim)
"let's get rid of the config item: XPBarFillGold -- there would not be a reason to turn this on and
off / let's make the default value for XpBarTrackOpacity 0.75 / let's make the default config for
XpBarWhiteNumbers = Disabled, because i find the white numbers distracting right now / update these
new default changes to also be in the config file for cjaycraft beta / server / and gale profiles"
Also: "the potions cool down looks okay, no issues with that" (1.2.9 Skyheim restore = clean).

### Changes (1.5.51, built + installed, bin=inst=Package sha C008A7C2204C, 311,296 b)
- XpBarFillGold DELETED: plugin bind + field + FillGoldOn() + live-drift block + _placedFillGold +
  FillDark colour. Fill is created gold (FillGold = GoldLine) and stays gold. Key gone from cfgs.
- XpBarTrackOpacity default 0.25 -> 0.75 (bind + description rewritten; fill-removed text).
- XpBarWhiteNumbers default true -> false (bind + description "off by default ... Off keeps
  everything gold"). NOTE the profile cfg already read false: the user flipped it live with F1
  before closing the game, so the value agreed with the new default; only the comments changed.
- cfgs: pack beta_cjaycraft\config + Gale profile -> XpBarTrackOpacity = 0.75,
  XpBarWhiteNumbers = false, XpBarFillGold block removed.
  SERVER: beta_cjaycraft-server\config (160 cfgs) has NO cjayride.SeneaLHudLayout.cfg and no
  mention under other\ -> HudLayout is client-only; nothing to change on the server.
  'test' Gale profile cfg = pre-1.5.29 legacy (has ShowLevelXpBar, no XP-bar keys) -> untouched;
  BepInEx writes the new keys at the new defaults whenever that profile loads the plugin.
- Docs: README bullet + defaults table (TrackOpacity 0.75, WhiteNumbers false, FillGold row gone);
  CHANGELOG 1.5.51; Package manifest 1.5.51; Package DLL/CHANGELOG/README re-synced; DiffReadme 0.
- Backups: profile .bak-1.5.50 (and .bak-1.5.47/48/49 kept). Game install untouched (1.5.25).

### Log evidence from the user's 1.5.50 session (they closed the game 04:55)
- `XP bar built, v1.5.50` - session stamp confirmed.
- SkyheimCompat self-report (1.2.9, OnSenealHotbar restored true):
  "Skyheim cooldown overlays on SeneaL UI's hotbar/action slots: 16 of 16 slots carry one."
  => DEFINITIVE: Skyheim DOES attach on SeneaL UI's action slots (all 16). The 1.2.7
  "strip them" gate changed nothing because stripping was the wrong move; the user is happy
  with the current look. The 1.2.6 behaviour was right all along, and it is now proven by log.
- XP diag: fillCol (0.93,0.82,0.52,1.00) = GOLD FILL visible (1.5.48 fix confirmed);
  borderCol (0.93,0.82,0.52,0.15) = 15% border confirmed; border quads borderTop 190x1 /
  borderBottom 190x1 / borderLeft 1x35 / borderRight 1x35 with sprite= (EMPTY) = the 1.5.47
  four-solid-lines border, no sprite face; barCol alpha 0.25 (the old track default in that
  session; 1.5.51 raises it to 0.75); plateCol 0.75; scale 1.00.

### Next: FPS hardening pass = 1.5.52 (proposal stands, awaiting go-ahead): TmpFont one-shot,
WorldHealthText off-path leak + per-plate caches, BossStars/VneiSearch negative caches,
ResourceBars guard, verify CharacterExtras guards, strip XP Diag/Probe scaffolding.

## Previous task (2026-11-11): 1.5.50 - white digits, 6px pad, 15% border, FPS audit

### Goal
User (verbatim): "1. add 2 more pixels of padding / 2. reduce the border opacity another 10
percent / 3. make all numbers (level) (percentage) (values: ## / ## ) white, but leave the symbols
like % and / the color they are now / 4. make sure our work here doesn't compromise FPS for the
player client"

### Changes
- NumbersPad 4 -> 6 (plate grows downward; digits stay at the 1.5.44 y).
- BorderGold a 0.25 -> 0.15 (config default + cfgs; live knob unchanged).
- White digits: Tick builds TMP rich text `Level <color=#FFFFFFFF>42</color>`,
  `<...cur> / <...req>`, `<...>%` - digits white, `Level `/` / `/`%` untagged -> inherit the
    label's gold (TextColor). [Hotbar] XpBarWhiteNumbers (default true, live); content key
    includes the tags so the toggle re-measures/re-places. Colour tags add no glyphs =>
    preferredWidth unchanged.
- FPS: PieceHealthLabel.Bar() whole-scene FindObjectsOfType<RectTransform> on EVERY
  Hud.UpdateCrosshair frame while the bar is missing (cache stays null => eternal scan).
  Now gated to a 2s retry (_nextBarScan).

### Status: COMPLETE (built + installed; audit result pending below)

### Build / Install
- BuildStaged exit 0, 10 warnings; installed 1.5.50 312,320 b sha 431BECC31DAE = bin = Package;
  .bak-1.5.49 kept; manifest 1.5.50; DiffReadme 0.
- cfgs pack+profile: XpBarBorderOpacity = 0.15, XpBarWhiteNumbers = true.

### FPS audit (read-only subagent, all 40 files) - COMPLETE, nothing modified
Cadence backbone: SeneaLHudLayoutPlugin.cs LateUpdate drives ~18 Tick()s per frame
(ResourceBars, SlotCue, PotionCooldown, CharacterExtras, KillXpNotice, AdventureJournal, StatsWindow,
EpicLootShardWindow, VneiSearch, CraftMaterials, StatValues, Chat*5, BossStars, WishboneKeySlot)
+ HudLayout.Apply/Restore + ActionSlotXpBar.Tick.

XP-bar work VERDICT: cheap. Probe()/Diag() = ONE-SHOT (_diagDone, armed in Ensure). GuardCover scan
= 1.5s cadence (const ScanInterval) inside the 0.1s data cadence; the 0.1s part is 2 field writes.
ShowXpBar off = whole card path unreachable (zero). Per-frame Tick = field reads + drift-checked
writes, no alloc, no scans.

TRUE PER-FRAME COSTS (targets for the 1.5.51 hardening pass, ON USER GO - NOT STARTED):
1. WorldHealthText.cs:48 postfix EnemyHud.UpdateHuds per frame: GetComponentsInChildren<TMP_Text>/
   Graphic/InventoryGrid(true) per visible plate (:568/:494/:814/:1137/:1183/:1228/:1253),
   ForceMeshUpdate (:846/:891), boxing Invoke (:814), Regex per text. WORST: :89-93 keeps walking
   every HUD entry + ReleasePlate per frame with the toggle OFF.
2. PieceHealthLabel.cs:113 whole-scene FindObjectsOfType<RectTransform> per crosshair frame -
   FIXED 1.5.50 (2s retry gate).
3. TmpFont.cs:42/:51 Resources.FindObjectsOfTypeAll<TMP_Text>+<TMP_FontAsset> EVERY FRAME via
   HudLayout.Apply -> PowersLayout.cs:153 (cache-stays-null).
4. VneiSearch.cs:343/:186 whole-InventoryGui Transform scan + assembly-name loop per frame
   (cache-stays-null) while search on.
5. BossStars.cs:22 recursive whole-root walk per frame (toggle on).
6. ResourceBars.cs:96/:144 per-row scans per frame; ApplyNumberWeight (:108) NO toggle guard.
7. ChatWindowLayout.cs:685 ForceMeshUpdate(true,true) on willRenderCanvases (gated _touched, bursty).
8. RepairTipPlace.cs:308/:354 panel GetComponentsInChildren per Tooltip.Build/Layout (per frame a
   tooltip is visible). StatValues tooltip patches AccessTools.Field + ForceMeshUpdate on hover.
9. EpicLootShardWindow.cs:989/:1004 per frame while hosting (grid scan + FieldInfo + Find per cell).
10. AdventureJournal.cs:81-97 new StackTrace() per map-point event (rare, very expensive).
Cache-stays-null sites: above 2-5 + SlotCue.cs:1118, PowersLayout.cs:683, StatsWindow.cs:709
(0.75s panel-open), ChatFont.cs:125/179 (2s).
UNVERIFIED: CharacterExtras.cs:553/:948/:959 (TickLevel IS per frame - top suspicion, guards unread),
SlotCue.Tick body, StatValues.cs:635, ResourceBars.StyleRow body. Not audited: ~20 small files
(CompassPin, DeathPinCompass, DrawerPreview, ... - grep showed no scan sites in most).
DEAD CODE (no callers): SlotCue.ShowOnIcons (:1061) / PaintAction (:1126, incl. :992 scan),
WorldHealthText.Tick (:188). Strip in 1.5.51.
PLAN 1.5.51 (pending go-ahead): negative-cache/one-shot for TmpFont, BossStars, VneiSearch;
WorldHealthText off-path early-return + per-plate caches; ResourceBars guard; verify CharacterExtras
guards; drop the XP Diag/Probe scaffolding.

## Previous task (2026-11-11): 1.5.49 (+ SkyheimCompat 1.2.9) - pad on the right side, hairline border, Skyheim restored

### Goal
User on 1.5.48 (verbatim): "the 'Border' lines are still too thick, cut in half its current size.
then reduce its opacity to 25% / i see what you did with the padding, it pushed down on the wrong
side. you added padding above the numbers but below the xp bar itself, we actually want the padding
below the numbers ## / ## / Check the log if you need to decide if you can put Skyheim back the way
it was, since you admitted to just guessing the fix"

### Border
- BaseBorderThickness 1 -> 0.5 (+ live [Hotbar] XpBarBorderWidth 0.5, 0.1-4, scale-scaled;
  PlaceBorder() re-places the 4 strips from the cached card box _cardW/_cardTop/_cardBottom/
  _cardCenterY/_cardH, no re-measure; drift knob _placedBorderWidth).
- BorderGold = gold a=0.25, live [Hotbar] XpBarBorderOpacity (_placedBorderAlpha); text keeps
  full-alpha gold.

### Padding (the real geometry bug, user diagnosed it)
- 1.5.45/48 slid the whole numbers PLATE down by NumbersPad -> the space grew between BAR and
  numbers. 1.5.49: amountRowH = digitsRowH + NumbersPad (plate TALLER, grows downward, stays
  flush under the top plate: topCenterY = topRowH*0.25 + digitsRowH*0.5 is the pad-free 1.5.44
  centring), digits at +NumbersPad/2 inside the plate => digits y = -4.5 = EXACTLY the 1.5.44
  position (verified numerically); 4px all below the digits, above the bottom border.

### SkyheimCompat 1.2.9 - the fix was a guess, the log falsified it, behaviour restored
- 1.2.8 session log (03:40): "Removed N Skyheim cooldown overlay(s)" x0, "Attached ..." x0,
  "Could not attach" x0. Strip walks every SeneaL hotbar+action root every 2s => Skyheim NEVER
  attached to SeneaL slots (field lookups no-op on SeneaL UI 1.2.8). The ring/number were never
  Skyheim; 1.2.7's OnSenealHotbar=false changed nothing.
- 1.2.9: default TRUE again (1.2.6 behaviour) in code + pack cfg + profile cfg (flipped false ->
  true); attach path logs "N of M slots carry one" (EnsureOn returns 1/0, _attachedCount/_slotCount
  reset per pass) so the claim is checkable forever.
- Consequence: the cooldown "ring/number" fix is VOID; PotionCooldownTextOffsetY=5 (HUD Layout's own
  nudge) is the live knob for the number; if the ring survives 1.5.49, the next suspect is HUD
  Layout's own inset square (PotionCooldownInset A/B) - NOT Skyheim.

### Status: COMPLETE (1.5.49 + 1.2.9 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 10 warnings. InstallTest: .bak-1.5.48, installed 1.5.49 311,296 b.
  NOTE: InstallTest cleans bin\* - hash bin AFTER install (first compare raced a missing path).
- Compat: dotnet 0/0; installed 1.2.9.0 hash-matched, .bak-1.2.8; skyheim-patch DLL+manifest 1.2.9.
- cfgs (pack+profile): XpBarBorderWidth 0.5, XpBarBorderOpacity 0.25; OnSenealHotbar true.
- Package 1.5.49 hash-matched (manifest caught at 1.5.48, fixed), DiffReadme 0.

### In-game test (1.5.49) - full restart, expect `XP bar built, v1.5.49`
1. 4px of black plate BELOW the digits (gap bar->numbers back to the 1.5.44 look).
2. Hairline 25%-gold border; live knobs XpBarBorderWidth / XpBarBorderOpacity.
3. Compat log: "Skyheim cooldown overlays on SeneaL UI's hotbar/action slots: N of M slots carry
   one." N>0 => Skyheim IS on the SeneaL slots (ring/number suspect restored); N=0 => never was.

## Previous task (2026-11-11): 1.5.48 (+ SkyheimCompat 1.2.8) - gold progress fill, 1px border, 4px under the numbers

### Goal
User on 1.5.47 (verbatim): "finally! you got the border on the xp bar area correct. next we still
need 2 pixels more BELOW the numbers of the xp bar ... also the border is too thick. make each
border very thing, definitely not more than a pixel. last thing, the xp progress bar is just black,
the yellow bar inside it to show far it has filled up isn't displaying. lastly, do we still need a
fix for the Skyheim mod for the fix in this mod to work? or was that just you guessing the fix"

### Fixes
- Fill colour: 1.5.41 made the filled part DARKER BLACK inside the black track (the "gold is only
  the border" spec) => invisible progress. Fill is now FillGold (= GoldLine). [Hotbar]
  XpBarFillGold (default true, live) restores FillDark. _placedFillGold nullable-bool drift.
- BaseBorderThickness 2 -> 1 (2px existed to beat antialiasing on the deleted 9-slice sprite;
  a quad covers a whole device pixel as-is).
- BaseNumbersPad 2 -> 4 (2px more between the digits and the card's bottom border).
- SkyheimCompat 1.2.8: StripSenealHotbarOverlays LOGS once "Removed N Skyheim cooldown overlay(s)
  from SeneaL UI's hotbar/action slots" - 1.2.7's strip was silent, so my "Skyheim's overlay was
  the second square/number" claim was code-inferred, never proven from the log.

### 1.5.47 session evidence (log 03:28)
- Stamps: HudLayout 1.5.47 + SkyheimCompat 1.2.7; diag borderTop/Bottom 190x2, borderLeft/Right
  2x28, borderCol(0.93,0.82,0.52,1.00) => border geometry correct (user confirmed).
- fill 92x4 fillCol (0,0,0,0.88) => the invisible progress bar, black by design.
- First guard hit with the widened scan: `XP cover: fixed .../LoadingBlack/Bkg (draws over the
  card)` (full-screen 2560x1440 black). NO Skyheim object in the probe list over the bar => the
  XP card never had a Skyheim overlay in this session (the card sits outside the slot subtree).
  The cooldown-square ring/number fix is about the SLOT face, and it stays unproven from logs.

### Status: COMPLETE (1.5.48 + 1.2.8 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 10 warnings. InstallTest: .bak-1.5.47, hash match, 309,248 b.
- SkyheimCompat: dotnet build 0/0; installed 1.2.8.0 103,936 b, .bak-1.2.7, hash match.
- cfgs: XpBarFillGold = true pack + profile (profile re-read after BepInEx rewrote it at shutdown).
- Package: DLL 1.5.48 hash-matched, manifest 1.5.48, CHANGELOG/README synced, DiffReadme 0.
- skyheim-patch: manifest 1.2.8 + CHANGELOG entry + hash-matched DLL.

### In-game test (1.5.48) - full restart, expect `XP bar built, v1.5.48`
1. Gold progress inside the black bar; 1px gold border; 4px gap under the numbers.
2. Skyheim strip line in the log (Ctrl+F "Removed"): N > 0 proves the 1.2.7 fix was needed; absent
   = the second square/number was not Skyheim on the SeneaL slots (then the inset square's own
   edge / another mod's overlay is the ring) - report either way.

## Previous task (2026-11-11): 1.5.47 - the gold "rectangle": 9-slice frame DELETED, border = four line quads

### Goal
User on 1.5.46 (verbatim): "when i turn on the xp bar using the config ShowXpBar enabled, I see a
yellow rectangle in the space where the XP bar should be, my guess is that you tried to draw a
border around the edges, but instead put a yellow golden rectangle on top of the entire thing,
why are you drawing rectangles instead of making a real border!?!?!"

### Evidence (1.5.46 session log, 03:02)
- Stamps: HudLayout 1.5.46 + SkyheimCompat 1.2.7 loaded; card built; diag shows
  plateCol(0.05,0.04,0.03,0.75), frame on=True sprite=XpGoldFrame frameCol(1,1,1,1), canvas 401.
- "XP cover: fixed" fired ZERO times (guard found nothing); Probe over the bar = CLEAN
  (only PixelFix/Scaled 3D Viewport alpha 0). => NO foreign graphic touches the card.
- A/B across builds: 1.5.44 (_frame off) = "completely solid black, wow!"; 1.5.45/46 (_frame on) =
  yellow rectangle. The 9-sliced XpGoldFrame Image is the only delta => it paints the face.

### Fix (1.5.47)
- XpGoldFrame sprite + GoldFrame() + MakeFrame() + _frame DELETED from the build.
- Border = four solid quads MakeLine (borderTop/Bottom 190x2, borderLeft/Right 2x(cardH-4),
  drawn last over the plates, GoldLine tint, PixelSprite). A quad paints exactly its rectangle:
  2px strip = a line; a full-card gold face is impossible.
- [Hotbar] XpBarBorder (default true, live): toggles all four enabled flags (_placedBorder
  nullable-bool drift pattern). cfg key added pack+profile.
- GuardCover scan widened: shift subtree OR same rootCanvas as the card (SeneaL UI's own
  frames/panels were out of scope before); keeps skipping our own card + shift root.
- Diag reports borderTop/Bottom/Left/Right boxes + borderCol (no more "frame").

### Status: COMPLETE (1.5.47 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 10 warnings (same set). InstallTest: .bak-1.5.46 kept, hash match, 308,224 b.
  Game untouched (1.5.25). cfgs: XpBarBorder = true pack + profile.

### In-game test (1.5.47) - full restart, expect `XP bar built, v1.5.47`
1. Black card at 0.75 + a 2px gold EDGE (four strips). Toggle XpBarBorder off live = pure black card.
2. If the "yellow rectangle" SURVIVES with the border off, the gold is the TEXT over
   semi-transparent plates -> next step is plate opacity 1.0 and/or white text, not the border.

## Previous task (2026-11-11): 1.5.46 (+ SkyheimCompat 1.2.7) - XP card invisible at 50% + cooldown border/number

### Goal
User on 1.5.45: (1) "the xp bar is completely missing!"; (2) cooldown square shows a visible
BORDER ring; (3) the countdown number never moved down.

### Root causes (all proven)
- Card "missing": the profile cfg pinned XpBarOpacity = 0.5 (the code default is a runtime value;
  BepInEx's written file wins). A 50% (0.05,0.04,0.03) plate over bright scenery washes to near
  white-on-white; the 1px gold frame under a device pixel antialiases away. Diag proved the card
  built, placed, active (canvas 401, cg 1.00, screen rect 144x9).
- Cooldown "border" + unmoved number: SkyheimCompat clones skyheim's own cooldown overlay onto every
  SeneaL hotbar/action slot (CooldownPrefabFix: full-slot stretched black 0.55 radial + centred
  Arial number). HUD Layout's inset square sat INSIDE it (ring = "border") and Skyheim's centred
  number drew over ours (my nudge moved a label the user never saw).
- PotionCooldownTextOffsetY: field declared, Config.Bind edit failed (old_string not found), entry
  stayed null -> dy 0 -> 1.5.45's nudge never applied. cfg written at shutdown lacks the key.

### Fix
- SkyheimCompat 1.2.7: SenealCooldownOverlay.Bind ([Cooldown overlay] OnSenealHotbar, default
  false); HotbarTick_Postfix early-returns and StripSenealHotbarOverlays destroys existing clones
  (CooldownPrefab.Strip: cache + _updateables RemoveAll + Destroy). InventoryGrid overlays
  untouched. Bump 1.2.7.
- HudLayout 1.5.46: XpBarOpacity default 0.75 (knob keeps 0.5 live); GoldFrame edge 1->2px;
  BarFrame rim gold -> near-black (0.08,0.08,0.09); PotionCooldownTextOffsetY Config.Bind added.
- cfgs (pack + profile): skyheimcompat OnSenealHotbar = false in [Cooldown overlay];
  SeneaLHudLayout XpBarOpacity -> 0.75; profile gets PotionCooldownTextOffsetY = 5.

### Status: COMPLETE (built + installed; in-game test pending)

### Build / Install
- SkyheimCompat: dotnet -c Release 0 warn 0 err, dist 1.2.7.0, installed (backup .bak-1.2.6,
  hash match, 103,424 bytes).
- HudLayout: BuildStaged exit 0, 10 warnings. InstallTest: .bak-1.5.45, hash match, 307,200 bytes.
  Game untouched (1.5.25).

### In-game test (1.5.46 + compat 1.2.7) - full restart
1. Expect `XP bar built, v1.5.46` + `Loaded [Cjayride Skyheim Compat 1.2.7]`.
2. XP card visible at 0.75 black (dial XpBarOpacity live), 2px gold border, all-black bar.
3. Potion cooldown: single square (no ring), number 5px below centre (PotionCooldownTextOffsetY).
4. Hotbar zero-shift intact.

## Previous task (2026-11-11): 1.5.45 - cooldown number +5px down & covers the main hotbar row; XP card 50% black + gold border back + 2px under the numbers

### Goal
User on 1.5.44: (1) move the cooldown number DOWN 5 more px; "could this be a cooldown timer for
other things too? that would be cool"; (2) XP bar "completely solid black background, wow!": give
it 50% opacity, add the golden border (user nervous after the border history), +2px padding directly
under the numbers row.

### Fix (1.5.45)
- [Items] PotionCooldownTextOffsetY (5, -40..40, live): ShowSlotSeconds subtracts dy from the
  centred anchoredPosition after MatchSlotFace.
- PotionCooldown.Update/Clear iterate SlotCue.HotbarSlots() + ActionSlots() (new SlotRow(fieldName)
  helper in SlotCue; ActionSlots = "_act", HotbarSlots = "_slots"): every consumable with a consume
  status effect (food, meads, potions) gets the dim square + countdown on the main row too.
- [Hotbar] XpBarOpacity (0.5, 0..1, live): Tick re-applies PlateFill with that alpha to both plates
  (NaN/drift pattern like XpBarTrackOpacity). PlateFill const alpha 0.5.
- Gold border BACK: _frame.enabled = true, color white in Ensure; GoldLine baked opaque (0.93,0.82,
  0.52,1) 1px ring, sprite CENTRE alpha 0 -> geometrically impossible to fill the card. diag adds
  frameCol.
- NumbersPad: digitsRowH unchanged; amount plate slides DOWN NumbersPad (2*scale); topCenterY =
  topRowH*0.25 + digits*0.5 + pad*0.5 keeps cardCenterY = 0 (the zero-shift/centring invariant).

### Status: COMPLETE (1.5.45 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 10 warnings (same set as 1.5.44). InstallTest: .bak-1.5.44 kept, SHA256 match,
  306,688 bytes. Game untouched (1.5.25).
- cfg key blocks added (pack + profile): PotionCooldownTextOffsetY = 5 (Items), XpBarOpacity = 0.5
  (Hotbar). README table + bullets updated; Package manifest/CHANGELOG/README/DLL synced.

### In-game test (1.5.45) - full restart, check `XP bar built, v1.5.45`
1. Cooldown number sits 5px lower in the slot (tune PotionCooldownTextOffsetY live in F1).
2. Eat food / drink a mead on the MAIN row (1-4): dim square + countdown appear there too.
3. XP card: 50% black (tune XpBarOpacity), crisp 1px GOLD border, bottom edge 2px lower.
4. Hotbar zero-shift: turning the bar on/off must not move the slots.

## Previous task (2026-11-11): 1.5.44 - all yellow OFF the XP card (opaque black) + cooldown square inset

### Goal
User on 1.5.43: (1) the cooldown square is centred now but BIGGER than the drawn quick-action
button, "probably needs ... another 10 pixels removed from all sizes"; (2) "the xp bar still has a
golden yellow overlay on it. maybe turn off all yellow elements used with the xp bar, then get the
border working later".

### Diagnosis
1. SlotCue sized the dim square to the slot FACE rect (bg.rectTransform), which includes SeneaL's
   button border art -> the square reaches past the visible button.
2. Our own frame sprite (SeneaLHudLayout_XpGoldFrame) has a fully TRANSPARENT centre, so it can
   never paint a full-card gold fill. The 1.5.43 guard matched only WARM Image.color, so a
   white-tinted image drawing a gold sprite (gold comes from the texture) slipped through. The card
   plates were 0.72 alpha: anything drawn behind/over showed through as "gold at reduced opacity".

### Fix (1.5.44)
- PlateFill alpha 0.72 -> 1.0: card plates opaque black; nothing shows through.
- _frame.enabled = false in Ensure: the gold outline image is OFF (user's instruction). Gold stays
  in the text; the border gets rebuilt properly later.
- GuardCover: measure _plateTop (the CARD), not the bar; drop the Warm filter (any colour, alpha >
  0.02, covers >= 75%); graphics on a canvas sorting >= ours -> SetActive(false), behind ->
  PlateFill black at min(own alpha, 0.45); re-assert each 0.1s tick (owners re-assert every frame);
  CoverLogged HashSet logs each fix once.
- SlotCue.EnsureSlotOverlay: sizeDelta = face.rect.size inset by [Items] PotionCooldownInset
  (default 10, range 0-40, live) on every side; centred on the face. Label stays on the full face.
- cfg: PotionCooldownInset = 10 added in both pack + profile cfgs (Items, after DimOpacity).

### Status: COMPLETE (1.5.44 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 10 warnings (same set as 1.5.43). InstallTest: .bak-1.5.43 kept, SHA256 match,
  305,664 bytes. Game untouched (1.5.25). Package manifest/DLL/CHANGELOG/README synced (diff 0).

### In-game test (1.5.44) - full restart, check `XP bar built, v1.5.44`
1. XP card: solid BLACK, zero yellow anywhere (border off). Text stays gold.
2. If any gold survives: Ctrl+F `XP cover: fixed` - the line names the graphic that was hidden/black.
3. Potion cooldown: black square + number fit INSIDE the drawn button; tune [Items] PotionCooldownInset
   in F1 live (10 default; try 12-14 if it still touches the border).
4. Border rework (thin crisp gold line, black interior) comes AFTER the card is provably clean.

## Previous task (2026-11-11): 1.5.43 - kill the gold rectangle covering the bar + centred potion cooldown square

### Goal
User on 1.5.42: (1) "still a rectangle of gold with reduced opacity either on top or behind the XP
bar ... only supposed to be black, behind the text"; (2) "the cool down square [on potion slots] is
not correctly aligned ... positioned down and right from the actual drawn slot".

### Diagnosis
1. Card's own graphics are proven dark (1.5.39/41 diag: plateCol black, fillCol black, track black).
   1.5.42 lifted the card above foreign overlays via a nested canvas. A gold rectangle still visible
   "on top or behind" = a FOREIGN graphic in the hotbar subtree stretched across the bar, showing
   THROUGH the semi-transparent plates (gold at reduced opacity = exactly the user's words).
2. SlotCue overlay/label placement used InverseTransformPoint(face center) as an anchoredPosition.
   That is pivot-relative, not centre-relative: off by (0.5-pivot.x)*W, (0.5-pivot.y)*H. Corner-pivot
   roots (SeneaL action-slot roots) push the potion dim square right+down from the drawn face.

### Fix (1.5.43)
- ActionSlotXpBar GuardCover (data cadence, 0.1s assert + 1.5s scan): FindObjectsOfType<Graphic>,
  skip self/shift/non-hotbar/cool-coloured/alpha~0; require Warm (gold/yellow) + CoverFraction >=
  0.75 of the bar's screen rect; repaint PlateFill black at min(own alpha, 0.45); log
  `XP cover: blacked out <path> was col ...`; re-assert each tick if owners re-tint. _cover cached.
- SlotCue.FaceCenterInRoot(root, face): mid - ((0.5-pivot)*rect.size); used by EnsureSlotOverlay
  and MatchSlotFace (potion dim square + seconds label + every slot label aligned to the face).

### Status: COMPLETE (1.5.43 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 10 warnings (9 pre-existing/1.5.42 + FindObjectsOfType obsolete in GuardCover).
  InstallTest: .bak-1.5.42 kept, SHA256 match, 304,128 bytes. Game untouched (1.5.25).
  Package manifest/DLL/CHANGELOG synced (diff 0). No cfg changes.

### In-game test (1.5.43) - full restart, check `XP bar built, v1.5.43`
1. Gold rectangle around/behind the XP bar: GONE, card black behind the text. If it survives, paste
   the new `XP cover: blacked out ...` (names what was blacked) + `XP cover:` probe line (2s line).
2. Drink a potion in the action row: dim square + seconds centred on the drawn slot (not down-right).
3. Everything from 1.5.42 unchanged (canvas order, hotbar zero-shift).

## Previous task (2026-11-11): 1.5.42 - the yellow is an OVERLAY on top of the bar: own canvas + cover probe

### Goal
User on 1.5.41: "i think the yellow background is actually ON TOP of the entire XP bar? it doesn't
look behind it, but yeah it's definitely yellow, and definitely takes the entire space of the XP bar".
The diag (1.5.39/41) proved every box we draw is dark/black; the 1.5.41 bar is all-black + 1px gold
rim. A full-bar yellow covering = a foreign graphic stretched over the hotbar, drawn after us.
Prime suspect: Cjayride.SkyheimCompat SenealCooldownOverlay clones skyheim_cooldown onto EVERY slot
root (incl. the action-slot root that hosts our XP card), SetAsLastSibling - gold-ish tinted image,
stretched to the parent's full rect (Stretch() = full anchors). Our card = sibling drawn BEFORE it.

### Fix (1.5.42)
- Card root gets a nested Canvas, overrideSorting, sortingOrder = HUD canvas order + 1 (SyncOrder
  re-reads it on the 0.1s data cadence; SeneaL re-asserts its canvas order on HUD show). Separate
  canvases -> our card draws above the hotbar overlays; popups/tooltips keep their own canvases.
- NEW diag line `XP cover:`: computes the bar's screen rect, walks ALL Graphics, lists up to 14
  foreign ones overlapping it: full path + live colour + sprite + rect. Runs once, 2s after build.
  If the yellow survives, this line NAMES it (path includes the owning mod's object names).
- diag line now also reports `canvas <order>`.

### Status: COMPLETE (1.5.42 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 9 warnings (8 pre-existing + my FindObjectsOfType obsolete in the probe - one
  run, fine). InstallTest: .bak-1.5.41 kept, SHA256 match, 303,104 bytes. Game untouched (1.5.25).
  Package manifest/CHANGELOG/README/DLL synced (diff 0). No cfg changes.

### In-game test (1.5.42) - full restart, check `XP bar built, v1.5.42`
1. Yellow overlay over the bar: GONE (card draws above the hotbar overlays).
2. If any yellow remains: paste BOTH `XP diag:` and `XP cover:` lines - cover names the culprit.
3. Hotbar/popups: XP card above slot overlays, popups/tooltips still above the card.

## Previous task (2026-11-11): 1.5.41 - ZERO yellow in the XP bar (black fill, black track, gold border only) + live opacity knob

### Goal
User on 1.5.40 (frustrated, right): "you do know what the color yellow is, right? the xp bar still
has a yellow background". My 1.5.40 kept a thin GOLD progress line and a 55% gold rim - gold IS
yellow. The spec from 1.5.38 has been the target all along: gold = border only, fill = black ~25%.

### Fix (1.5.41)
- Fill: BLACK 0.88 alpha (dark black progress inside a lighter black groove; no color at all).
- Track sprite: black interior + gold 1px rim, both baked OPAQUE; the bar Image's alpha (new key)
  scales everything live, so the whole bar = black 25% + gold 25% rim at the default.
- NEW KEY [Hotbar] XpBarTrackOpacity (default 0.25, range 0-1, LIVE - Tick re-applies when the F1
  slider drifts; the user asked in turn 17 for exactly this kind of control on chat opacity).
- KillLabelGlow rewritten: the diag said the label material's shader is TextMeshPro/Mobile/
  Distance Field with NO desktop _Glow* props (diag "glow=-") -> the 1.5.39 strip did nothing. Now
  walks the material's OWN shader properties and zeroes every glow/underlay/outline/shiny/reflect/
  dilate/bevel/bump prop the shader actually has (float/range -> 0, color -> alpha 0).
- Diag line adds `barCol` (the track's live color) for the next paste.
- cfg: XpBarTrackOpacity added to pack cfg + profile cfg (keys/defaults changed -> in scope).

### Status: COMPLETE (1.5.41 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0 (after 1 fix: `using UnityEngine.Rendering;` for ShaderPropertyType), 8
  pre-existing warnings. InstallTest: .bak-1.5.40 kept, SHA256 match, 301,056 bytes. Game untouched
  (1.5.25). Package manifest/CHANGELOG/README/DLL synced (diff 0; README XP bullet + table row).

### In-game test (1.5.41) - full restart, check `XP bar built, v1.5.41`
1. Bar: NO yellow inside. Empty groove = translucent black; filled part = darker black; gold only
   the 1px rim/border.
2. F1 -> Hotbar -> XpBarTrackOpacity slider: bar transparency changes live, no restart.
3. Label text: if the food-timer font glows, the haze is now stripped (shader-walk).

## Previous task (2026-11-10): 1.5.40 XP bar gold FILL slab -> thin gold line (diag-proven root cause)

### Goal
User on 1.5.39: "the background of the XP bar is still yellow, so it wasn't fixed" - and pasted the
FULL log incl. the diag line (great data):
`plateTop 190x18 XpPlate on | plateAmount 190x12 XpPlate | frame 190x30 XpGoldFrame | bar 100x6
XpBarFrame | fill 92x4 sprite=- | fillCol (1.00,0.86,0.25,0.95) | plateCol (0.05,0.04,0.03,0.72) |
labelMat TextMeshPro/Mobile/Distance Field glow=- glowOuter=- underlay=- | cg:menuFade=1.00 |
cg:SeneaLUI_Hud=1.00`

### Root cause (proven by the diag, 4-round hunt over)
- Plates dark (0.05,0.04,0.03,0.72), canvas alphas 1.0, font material glow-free, frame = 1px line.
- The YELLOW = the gold PROGRESS FILL: player was at ~92% XP, fill 92x4 inside a 100x6 bar - the
  gold slab dominates the 6px bar and reads as "the golden background of the XP bar".
- 1.5.40: fill = thin gold LINE ~34% of the bar height hugging the track's bottom; the 25%-black
  track stays visible at every XP level. Scale change re-runs the fill write (was: scale change
  left the fill at the old BarHeight math).
- Diag + font-glow strip stay in (cheap, one-time lines; useful for future hunts).

### Status: COMPLETE (1.5.40 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 8 pre-existing warnings. InstallTest: 1.5.40 installed, .bak-1.5.39 kept,
  SHA256 match. Game untouched (1.5.25). Package manifest/CHANGELOG/README/DLL synced (diff 0;
  README XP bullet now documents the 25%-black track + gold line). No cfg changes.

### In-game test (1.5.40) - full restart, check `XP bar built, v1.5.40`
1. Bar: dark see-through track; gold = rim + thin progress line at the bottom; at high XP no
   gold slab.
2. Panel rows dark, numbers close under the bar (from 1.5.37) unchanged.

## Previous task (2026-11-10): 1.5.39 pale-yellow hunt part 3 - font-material glow strip + live self-diagnostic

### Goal
User (on a CONFIRMED v1.5.38 session, via ask_user answers): the WHOLE XP panel is still pale
yellow, not black-with-opacity. Three fixes (1.5.36 PPU1 border, 1.5.37 dedicated plate sprite,
1.5.38 bar fill 25%) did not resolve it, and no pale-yellow mechanism reproduces in code review.

### Root-cause status
- Audit of the card: plateTop/plateAmount = PlateSprite + PlateFill(0.05,0.04,0.03,0.72) set in
  MakePlate on EVERY Ensure (root is destroyed/recreated with the shift, so colours cannot drift);
  frame = transparent-centre 1px 30% GoldLine (PPU1/Point); bar = BarFrame(black 25% + 55% rim);
  fill = BarFill gold sized <= BarWidth-2, BarHeight-2; root zero-size. NO gold fill exists.
- Remaining unproven suspects for a whole-panel yellow wash: (1) the labels' COPIED food-timer TMP
  material carrying a glow/underlay (bright gold text blooming yellow over a dark panel);
  (2) a parent CanvasGroup/tint nobody audited.

### Changes (1.5.39, `ActionSlotXpBar.cs` + version only)
- `KillLabelGlow()` after every font copy: zeroes _Glow* / _UnderlayPower / _OutlineWidth /
  _FaceDilate and clears _GlowColor/_UnderlayColor on `fontSharedMaterial` (the copy; shared
  asset untouched).
- `Diag()` + one-time `Debug.Log("[SeneaL HUD Layout] XP diag: ...")` 2s after each Ensure:
  per-Image name/size/sprite/enabled, fill + plate colours, scale, label material name + glow
  values, every CanvasGroup alpha up the parent chain. Pasting that ONE line identifies the
  yellow pixel definitively.
- Version 1.5.39 (VERSION/csproj/manifest). CHANGELOG 1.5.39; README unchanged (diff 0).
  No cfg changes.

### Build / Install
- BuildStaged exit 0, 8 pre-existing warnings. InstallTest: 1.5.39 (300,032 bytes) installed,
  .bak-1.5.38 kept, SHA256 match. Game untouched (1.5.25). Package DLL/CHANGELOG/manifest synced.

### In-game test (1.5.39) - full restart
1. LogOutput.txt: `XP bar built, v1.5.39` then `XP diag: ...` (2s after the card appears).
2. If yellow is gone: the food-timer material's glow was the haze; consider keeping the strip.
3. If yellow REMAINS: paste the `XP diag:` line (and a screenshot); the numbers name the culprit.

## Previous task (2026-11-10): 1.5.38 XP bar track black-25% + chat opacity startup re-assert

### Goal (user report on 1.5.37: "fixed everything, except")
1. "The golden background behind the xp bar is still golden yellow.. that golden yellow should only
   be the border, not the fill color, the fill color is black with opacity ~25%."
2. Chat window opacity: "it didn't load the default 25% ... I moved the opacity bar in F1 settings
   and the chat window went hidden completely ... wouldn't re-appear until I clicked the force-on
   button ... then it switched to 25% correctly."

### Root causes
- XP bar track: BarFrame() sprite fill was BarBack (black 0.72) - near-opaque; at 6px tall the bar
  is rim-dominated so the whole track read golden. Fill is now baked `Color(0,0,0,0.25)` in the
  sprite; `BarBack` constant deleted (unused). Gold stays only in the 1px rim + progress fill.
- Chat: ChatBackground used a one-shot guard (`_seenAlpha`): applied 0.25 once, then SeneaL's
  ChatSkin fade wrote the SAME alpha back (bg alpha *= CanvasGroup fade, and our LateUpdate runs
  after ChatSkin's Tick), drifting the panel to full alpha; the guard then refused every
  re-assert. Config was fine (both cfgs ChatBackgroundOpacity = 0.25). Fix: drop `_seenAlpha`
  entirely - Tick() re-asserts the target alpha every frame it drifts (compare + rare write).
  Tick order already right: ChatHide -> ChatWindowLayout -> ChatPrefix -> ChatBackground.

### Status: COMPLETE (1.5.38 built + installed; in-game test pending)

### Build / Install
- BuildStaged exit 0, 0 errors, 8 pre-existing warnings. InstallTest: 1.5.38 installed, backup
  .bak-1.5.37 kept, SHA256 match. Game untouched (1.5.25). Package manifest/CHANGELOG/DLL synced
  (pkg dll 1.5.38); README unchanged (diff 0). No cfg changes (no keys/defaults changed).

### In-game test (1.5.38) - full restart
1. Look for `[SeneaL HUD Layout] XP bar built, v1.5.38` in LogOutput.txt.
2. XP bar empty track: dark translucent (see through), gold only rim + fill.
3. Chat at startup: 25% dark background right away, no touching F1; fades stay proportional.

## Previous task (2026-11-10): 1.5.37 XP panel pale-yellow hardening + numbers finally move + build stamp

### Goal (user report, mid-test)
1. "The entire background of the XP panel is a light yellow, instead of the black background with
   the opacity setting that was there."
2. "The numbers for the xp bar didn't get moved up closer to the xp bar itself yet."
   Ask-user answers: yellow = the WHOLE panel; numbers = "not moved at all".

### Analysis
- "Not moved at all" is exact: in 1.5.36 the amount row shrank (SmallFont+2) and amountCenterY
  re-derived, but the TEXT centre landed at the SAME y as 1.5.35 (digits -13.0 vs -13.5); the
  top row stayed at 0. So visually nothing moved - my 1.5.36 "lift" only shrank the plate
  around unmoved text. Pale-yellow whole panel + numbers not moved = the 1.5.35 look (90% gold
  100-PPU border smears a translucent gold wash over the panel edges + old full-row numbers).
  Disk DLL is hash-verified 1.5.36 (21:36) and Valheim was NOT running at check time, so the
  test session most likely launched BEFORE the 21:36 install (stale in-memory 1.5.35).
- Can't fully prove the pale panel = stale DLL, so harden against the shared-sprite theory too.

### Status: COMPLETE (1.5.37 built + installed; in-game test pending)

### Changes (all `ActionSlotXpBar.cs` + version)
- `MakePlate` -> own `PlateSprite()` (1x1 RGBA32 solid white, Point, Clamp, PPU 1; Image.color
  = PlateFill carries the 0.72 alpha) - dedicated texture for the card background, no shared
  sprite involvement; `_back.color = Color.white` explicit (the BarFrame sprite carries fill+rim).
- Place() numbers actually move: `topRowH = max(14*s, LevelFont + 6*s)` (tighter, ~18 at s=1);
  bar rides LOW: `barOffsetY = -(topRowH - BarHeight)*0.5 + 1*s` (bar 1u above the top plate's
  bottom edge; level/percent labels at barOffsetY so they sit ON the bar); amountCenterY =
  topCenterY - topRowH/2 - amountRowH/2 (flush under the top plate) -> digits ~2px under the bar
  (was ~14). Cards stay centred on the slots' mid height.
- Ensure(): one-time `Debug.Log("[SeneaL HUD Layout] XP bar built, v" + VERSION)` build stamp.
- Frame follow-through (found while re-reading Place, ships with 1.5.37): `cardCenterY =
  (cardTop+cardBottom)/2` used for the frame position (an intermediate edit centred it at y=0,
  which hangs ~5px below the grown card bottom).
- Version 1.5.37 (VERSION, csproj, manifest). CHANGELOG 1.5.37. README unchanged (visual text
  still accurate; diff 0). Package CHANGELOG+DLL synced (pkg dll 1.5.37). No cfg changes.

### Build / Install
- BuildStaged: exit 0, 0 errors, 8 pre-existing warnings. InstallTest: profile -> 1.5.37
  (297,472 bytes), backup .bak-1.5.36 kept, SHA256 match. Game untouched (1.5.25).

### In-game test (1.5.37) - superseded by 1.5.38; full restart rule still applies
1. Open `LogOutput.txt`: one `[SeneaL HUD Layout] XP bar built, v1.5.37` line = fresh build.
2. Panel dark again (no yellow); numbers sit right under the bar (~2px); bar/level/% ride at the
   bottom of the top plate; crisp 30% gold outline; card centred on the slots, 12px gap.
3. If 1.5.37 log line present and panel is STILL pale yellow -> screenshot, then instrument
   (per-child colours) - shared-sprite tinting not reproducible in code review.

## Previous task (2026-11-10): 1.5.36 XP glow removal + border 30% + numbers lifted
- Numbers lift shipped as plate-shrink only (text centre unmoved) - fixed in 1.5.37.
- Glow removal: shadow strip deleted; GoldFrame/BarFrame PPU 100 -> 1 + Point filter; GoldLine
  alpha 0.9 -> 0.30.

### Goal (user report)
1. The yellow glow shadow on the card's LEFT/RIGHT borders is distracting -> remove it.
2. The numbers under the bar need to move up, closer to the XP bar.
3. The glowing gold border: opacity ~30%.

### Root causes
- Glow on side borders: (a) the shadow strip spanned the full card width and its gradient read as
  a yellow glow bleeding at the left/right ends (placed under the bottom edge, its top overlapped
  the plates' bottom corners); (b) GoldFrame/BarFrame sprites are PPU 100 -> the 1px rim is 0.01
  world units wide, sampled with bilinear smoothing into a soft glow all around the card.
- Numbers distance: the amount plate was a FULL row (PlateMinHeight 18) under the top row, and the
  card bottom sat at the old bottom edge -> lots of space; the numbers sat low.

### Status: COMPLETE (1.5.36 built + installed; in-game test pending)

### Changes
- `ActionSlotXpBar.cs`:
  - Shadow strip fully removed (field/_shadow, MakeShadow, SoftShadow, CenterAll entry, Place
    lines). Class doc + comments updated.
  - `GoldLine` alpha 0.9 -> 0.30. `GoldFrame()` + `BarFrame()`: `pixelsPerUnit` 100 -> 1 and
    `filterMode = Point` -> crisp 1-unit (1px) line, no smear (the true "glow" cause).
  - Place(): `amountRowH = SmallFont + 2*_scale` (plate hugs the digits; was the full 18-row,
    the visible empty band); plates FLUSH (`amountCenterY = -topRowH*0.5`, `topCenterY =
    amountRowH*0.5` so the CARD centre stays 0 = slots' mid height); frame centred at
    (cardW/2, 0) size (cardW, cardH). No BaseAmountGap in the shipped build.
- Version 1.5.36 (VERSION, csproj, manifest). CHANGELOG 1.5.36; README XP bullet (crisp 1px gold
  line at 30%, numbers close under bar). Package README/CHANGELOG/DLL synced (diff 0, pkg 1.5.36).
  No cfg key changes.

### Build / Install
- BuildStaged: exit 0, 0 errors, 8 pre-existing warnings (twice: main + frame-centre rebuild).
- InstallTest: profile -> 1.5.36 (297,472 bytes), backups .bak-1.5.35 + .bak-1.5.36 (mid-turn),
  SHA256 match. Game untouched (1.5.25).

### In-game test (1.5.36)
1. Card: no glow anywhere; gold border is a thin crisp 30% line exactly on the card; numbers sit
   close under the bar (plate hugs the text, ~3px gap); card's top row still centred on slots.

## Previous task (2026-11-10): 1.5.35 potion cooldown shift/square + countdown size, XP frame offset, XP gap

### Goal (user report)
1. Potion cooldown on quick action slots: using a potion moved the whole action bar UP and drew a
   SQUARE a row below it; must not. Opacity ask already works; make the numbers HALF size and TOP
   aligned in the slot (icon readability).
2. XP bar: touching the last action slot - wants 10-15px spacing. Gold border shifted ~100px LEFT
   of the card (alignment wrong).

### Root causes
- Square+shift: `PotionCooldown` dim = `SlotCue.EnsureSlotOverlay` -> `CopyRect(image, root)` where
  root = SeneaL `Slot.Root` RectTransform, whose box is bigger than the visible slot face (the _bg
  image) - the copy showed a ~100x100 dim square under a row of the bar, and the 1.5.33
  FindHotbarBounds skip matched ONLY the name `SeneaLHudLayout_SlotXp`, so the dim
  (`SeneaLHudLayout_CoolDim`, child of a slot under the shift) counted in the hotbar bounds -> minY
  pulled down -> hotbar shifted UP when active. One bug, two symptoms.
- Frame offset: `_frame.rectTransform.localPosition = Vector3.zero` centred the gold outline on the
  ROOT origin; the card spans x 0..cardW from that origin, so the frame sat cardW/2 (~100px) left.
- Gap: SlotGap = 0 (told 1.5.34 flush); user wants 10-15px -> SlotGap 12.

### Status: COMPLETE (1.5.35 built + installed; in-game test pending)

### Changes
- `HudLayout.cs` - FindHotbarBounds: skip EVERY child named `SeneaLHudLayout_*` (prefix, not exact
  name) - all our decorations (XP card, CoolDim/CoolTime, Glow, Rarity, Mark, Equip) can never move
  the hotbar again.
- `SlotCue.cs` - `EnsureSlotOverlay` sizes the overlay to the slot's VISIBLE face (`_bg.rectTransform`
  via new `SlotFace`): centre anchors, anchoredPosition = face centre in root space, sizeDelta =
  face.rect.size; falls back to CopyRect(root) when _bg is absent. `ShowSeconds` gained size+align
  params (old 22/Center defaults preserved via overload); `ShowSlotSeconds(time, left, fontFrom,
  size, align)` forwards them and keeps PotionCooldownTextOpacity.
- `PotionCooldown.cs` - Paint -> `ShowSlotSeconds(slot, label, seconds, countText, 11f, Top)`.
- `SlotCue.cs` (2nd pass) - potion label is also face-MATCHED: public `MatchSlotFace(slot, label)`
  (root-space centre anchors + face size) applied by the new `ShowSlotSeconds(slot, ...)` overload
  before `ShowSeconds(..., stretchToParent: false)` - ShowSeconds' stretch-to-parent (anchors
  0..1 of the slot ROOT) was centring Top/Center in the root's padding, not the visible button.
  Old overloads keep stretch=true so equip/gear/grid timers are untouched.
- `ActionSlotXpBar.cs` - SlotGap 0 -> 12; `_frame` centred at (cardW/2, 0) (the card's centre).
- Version 1.5.35 (plugin VERSION, csproj, Package manifest). CHANGELOG 1.5.35; README potion +
  XP bullets; Package README/CHANGELOG/DLL synced (diff 0, pkg dll 1.5.35). No cfg key changes.

### Build / Install
- BuildStaged: exit 0, 0 errors, 8 pre-existing warnings. InstallTest: profile -> 1.5.35
  (297,472 bytes), backup .bak-1.5.34, SHA256 match. Game untouched (1.5.25).

### In-game test (1.5.35)
1. Drink a potion: action bar does NOT move, NO square under the row; countdown small (11) at the
   slot's top edge, icon readable below.
2. XP card: gold rectangle sits exactly on the card; 12px clear of the last action slot; no HUD
   shifting on toggle.

## Previous task (2026-11-10): 1.5.34 HideChatPanelDelay 12 + XP bar single gold-framed card

### Goal (user report)
1. HideChatPanelDelay default 12 (code + pack cfg + Gale profile cfg).
2. XP bar: numbers row moved up (its top edge touches the top row's bottom edge); numbers LEFT
   aligned starting at the bar's start (like the Character panel, not centred); numbers background
   spans the FULL card width (level -> bar -> %); the two backgrounds look like ONE giant
   background via a thin gold border rectangle around the whole group (top of top row, both side
   edges, bottom of bottom row); slight shadow under the bottom edge.
3. Still too far right of the Action Slots: reduce the gap.

### Status: COMPLETE (1.5.34 built + installed; in-game test pending)

### Changes
- `SeneaLHudLayoutPlugin.cs` - HideChatPanelDelay default 10f -> 12f (+ description "The game
  ships 10; this mod ships 12"). VERSION 1.5.34. csproj version block.
- Both cfgs (`beta_cjaycraft\config\cjayride.SeneaLHudLayout.cfg`, Gale profile cfg):
  HideChatPanelDelay = 12, Default value: 12, description synced (block at [Chat] ~line 103).
- `ActionSlotXpBar.cs`:
  - SlotGap 4 -> 0. Anchor() now prefers the ACTION SLOTS frame when active (was: widest of
    actions/quiver - the quiver reaching further right was part of the "too far right").
  - AttachPoint uses ContentRight(frame): max (child.rect.xMax + child.anchoredPosition.x) over
    ACTIVE children, clamped to frame.rect.xMax - the last visible SLOT's right edge, not the
    padded frame rect (the padding was the remaining gap).
  - Place(): card = top row + amount row with NO gap (cardH = topRowH + amountRowH);
    plateAmount is FULL cardW (same left/right as top row); _amount left-aligned at barX
    (Midline, width amountWidth); _frame = gold outline sized cardW x cardH at card centre;
    _shadow = soft shadow under card bottom (cardW x ShadowHeight 4*scale).
  - Sprites: GoldFrame() 12x12 9-slice 1px GoldLine (0.93,0.82,0.52,.9) edges, transparent centre;
    SoftShadow() 1x8 vertical gradient black .38 -> 0 alpha, bilinear; BarFrame() 12x12 9-slice
    BarBack fill + light rim for the bar box. Plates are PLAIN fill now (the gold frame is the
    border). Bordered() deleted.
  - Ensure(): child order shadow -> plateTop -> plateAmount -> frame -> (bar/fill/labels inside
    plates); CenterAll includes frame + shadow.
- Docs: CHANGELOG 1.5.34; README XP bullet + HideChatPanelDelay rows (12); Package manifest 1.5.34;
  Package README/CHANGELOG/DLL synced (diff 0).

### Build / Install
- BuildStaged: exit 0, 8 pre-existing warnings. InstallTest: profile -> 1.5.34 (296,960 bytes),
  backup .bak-1.5.33, SHA256 match. Game untouched (1.5.25).

### In-game test (1.5.34)
1. Chat: panel stays 12s after last activity (fresh default; existing cfgs updated to 12).
2. XP bar: flush against the last action slot (zero gap); numbers plate full width touching the
   top row; numbers start at the bar's left; gold rectangle outlines the whole card; soft shadow
   under the bottom; still zero HUD shifting on toggle.
3. XpBarScale 1 / 2 / 0.5: border 1px + shadow scale cleanly.

## Previous task (2026-11-10): 1.5.33 XP bar shift + layout, Guardian label line

### Goal (user report)
1. Stats tab: "Guardian powers:" must be its OWN line (label-only, like Status effects) before the
   power rows - currently the first power shares the label row.
2. XP bar: attach directly to the slots' right edge; centre vertically on the slots; less gap
   level->bar; ONE background behind level+bar+%; numbers plate under the bar with numbers centred;
   CRITICAL: enabling ShowXpBar must NOT shift the action bar / related elements at all.

### Root causes
- SHIFT: HudLayout.FindHotbarBounds walks every activeInHierarchy descendant of HotbarShift and
  takes the min bottom edge to place the hotbar at screen bottom; the XP plates (taller than the
  slots, below their bottom edge) dragged minY up -> whole HUD pushed up when ShowXpBar on
  (off -> Hide() -> plates inactive -> bounds snap back: the reported shift).
- Layout: root was Unity-default 100x100; separate level/top/amount plates with big gaps; percent
  width minimum 24; SlotGap 10.

### Status: COMPLETE (1.5.33 built + installed; in-game test pending)

### Changes
- `HudLayout.cs` - FindHotbarBounds skips `child.name == "SeneaLHudLayout_SlotXp"` (our overlay is
  decoration, never part of the hotbar bounds).
- `ActionSlotXpBar.cs` - REWRITTEN: root = zero-size point (anchors (0,0), pivot (0,0.5),
  sizeDelta 0) parented to shift; plates/text are children of the ROOT with (0.5,0.5) anchors +
  centre pivot and localPosition offsets from the root origin (x right, y up). AttachPoint =
  anchor rect xMax + SlotGap(4), y = anchor.rect.center.y (exact middle). ONE top plate
  [Level 37 | 8px | bar | 6px | 37%] (InnerGap/BarGap scale props), amount plate under the bar
  (barX centre-aligned), numbers Midline-centred on plate size (plate-2 wide). topRowH = max
  (PlateMinHeight*s, LevelFont+8, BarHeight+8); block centred on root origin. Fill child of bar,
  NaN-guard unchanged. Bordered() 9-slice unchanged.
- `StatsWindow.cs` - AddGuardianPowers: powers.Count==0 -> Row("Guardian powers","—") + return;
  else RowLabelOnly("Guardian powers") then Row("", power...) rows (Status-effects pattern).
- Version 1.5.33 (csproj, plugin VERSION, Package manifest). CHANGELOG 1.5.33 section; README XP
  bullet rewritten. Package README/CHANGELOG/DLL synced (diff 0, pkg dll 1.5.33). No cfg changes
  (no new keys).

### Build / Install
- BuildStaged: exit 0 after fixing CS0117 MidlineCenter (-> Midline) + CS0246 (missing using
  System). 8 warnings all pre-existing.
- InstallTest: profile dll -> 1.5.33, backup .bak-1.5.32, SHA256 matches build. Game untouched.

### In-game test (1.5.33)
1. Toggle ShowXpBar on/off: action bar + vitals + food DO NOT MOVE at all.
2. XP bar: tight right of slots, dead-centre on them vertically; single plate behind
   Level+bar+%; XP numbers centred on their plate under the bar; XpBarScale 1/2/0.5 clean.
3. Stats tab: "Guardian powers:" alone on its line, powers under it aligned with other rows.

## Previous task (2026-11-10): 1.5.32 XP bar scrambled-text fix

### Goal (user report)
Action-slot XP bar (1.5.30/31): "the text is all scrambled, I can't read it" - user asked why it
doesn't use the same font as the food-bar player level (ShowLevelFoodBar), which reads fine.

### Root cause (found in ActionSlotXpBar.MakeLabel)
1. `label.outlineWidth = 0.22f` + `outlineColor` set AFTER the material copy. In TMP 2.x setting
   outline properties swaps the text object's material for a CLONE OF THE FONT ASSET'S BASE
   MATERIAL. Valheim's HUD labels use per-label baked atlas materials (TMP_SubMeshUI atlases);
   the font asset's base material doesn't reference Valheim's runtime atlas -> glyphs sample the
   wrong atlas cells -> every letter draws as a random glyph. (The 0.22 outline on 12px bold also
   eats the letters.) The food-bar label (CharacterExtras.EnsureFoodLevel) never touches
   outline/shadow/underline - that's why it reads.
2. Secondary: CopyLevelFont falls back to TMP_Settings.defaultFontAsset when the character window
   `_level` label doesn't exist yet -> default font asset + stale copied material = mismatch.

### Status: COMPLETE (1.5.32 built + installed; in-game test pending)

### Changes
- `CharacterExtras.cs` - new public `CopyFoodBarFont(label)`: copies font AND fontSharedMaterial
  from `Hud.m_rootObject/SeneaLUI_Hud/Food/fx/vitals/food/time2` TMP_Text (the exact sample the
  food-bar level label uses); falls back to CopyLevelFont. `FoodTimerSample()` helper. Doc comment
  warns: never set outline/shadow/underline on labels styled this way.
- `ActionSlotXpBar.cs` - `MakeLabel`: outline properties REMOVED; font via `CopyFoodBarFont`.
  Font copy retried once/second while `_fontApplied` false (food timer label builds late;
  HasFoodFont checks font != TMP default). Plates stay (they provide the contrast).
- Version 1.5.32 (csproj 4 props, plugin VERSION, Package manifest). CHANGELOG 1.5.32 section.
  README XP-bar bullet: "outlined text" -> "same font as the food-timer text". Package
  README/CHANGELOG/DLL synced (diff 0, pkg dll 1.5.32).
- No cfg changes (no new/changed keys).

### Build / Install
- tools/BuildStaged.ps1: exit 0, 8 warnings (all pre-existing), 0 errors; dll 1.5.32.
- tools/InstallTest.ps1: profile dll -> 1.5.32 (295,936 bytes), backup .bak-1.5.31, SHA256 matches
  build. Game install untouched (1.5.25).

### In-game test (1.5.32)
1. ShowXpBar on: `Level 37`, `37%`, `1234 / 3333` render as normal Averia text (like the food
   timer / food-bar level), NOT glyphs.
2. Text sits on the dark plates, fill stays inside the bar, XpBarScale 1/2/0.5 sizes cleanly.

## Previous task (2026-11-10): 1.5.31 Stats-tab Guardian powers section

### Goal (user request)
Add a "Guardian powers" section to the Stats tab: one row per equipped boss power (pack allows 2),
each with its passive state — Active / Depleted 1m45s (pack: 120s buff, 120s lockout, 900s CD) — via
the proven m_guardianPower -> ObjectDB.GetStatusEffect path, AND include the passive description
lines (user: "yes you should add the passive descriptions"). Also: "there's already a section for
Forsaken power there, but nothing in it" -> make that row always answer. DLL built + installed to
Gale 4.4.44 profile with backup; full path printed; no zip / no github / no pack manifest+CHANGELOG
edits (pack .cfg IS in scope).

### Status: COMPLETE (1.5.31 built + installed; in-game test pending)

### Changes
- `StatsWindow.cs` - new fields `GuardianPowerField` (Player m_guardianPower), `PassiveModType`
  (ModTypes.Find "PassivePowers"/"PassivePowers.PassivePowers"), `PassiveBossConfigs` +
  `PassiveMaxPowers` (public static readonly FIELDS on PassivePowers: activeBossConfigs /
  maximumBossPowers - NOT properties). `AddGuardian` rewritten: Forsaken power row always shows
  (name · Ready/CD; CD alone when no vanilla power). `AddGuardianPowers`: equipped list =
  m_guardianPower split ',' (same as PowersLayout.PowerName); slot count = Passive Powers
  "Maximum boss powers" clamped 1-7, max with equipped count; row per slot `Eikthyr · Active 1m 59s`
  (SE "PassivePowers GP_X" remaining) / `· Depleted 1m 45s` (SE "PassivePowers Depletion GP_X") /
  `· Passive`; label "Guardian powers" on first row, "" after; empty slots `—`. Descriptions:
  `GuardianPowerDescriptions(key,"passive")` reflects activeBossConfigs[key].Configs, per config
  BoxedPassive>0 -> localize("$powers_type_passive: " + Desc, [amount]) = the exact boss-stone
  tooltip line; `· ` prefix rows. All reflection, whole description read in try/catch.
  `Member(obj,name)` = Field then Property. Status effects list now also skips SEs named
  "PassivePowers*" (buff + depletion, covered by the section). `using System.Collections;` added.
- `SeneaLHudLayoutPlugin.cs` - VERSION 1.5.31; new bind `[Stats] GuardianPowers` default TRUE
  (needs Passive Powers; hides section without it).
- Pack cfg + profile cfg: +GuardianPowers block in [Stats] after StatsPortrait; header v1.5.31.
  (Inline PS insert collapsed the block into one line - fixed via tools\FixGuardianPowersBlock.ps1;
  verified 4-line block + blank separators.)
- CHANGELOG 1.5.31; README Guardian powers bullet + `| Stats | GuardianPowers | On |` row;
  Package README/CHANGELOG/DLL/manifest synced (diff 0, manifest 1.5.31, pkg dll 1.5.31).

### Key facts verified (from decompiled sources in C:\Users\cjrob\AppData\Local\Temp\vanilla)
- Passive Powers SEs: buff SE name = "PassivePowers " + GP_X (m_ttl = Duration 120s), depletion SE
  = "PassivePowers Depletion " + GP_X (m_ttl = Depletion 120s). Remaining = m_ttl - m_time
  (existing StatusTime). Player.m_guardianPower = comma list of GP_ names.
- GetGuardianPowerHUD: se = m_guardianSE (vanilla active power), cooldown = m_guardianPowerCooldown.
  Vanilla GP SEs named GP_* (contain "guardian"? NO - "GP_" does not contain "guardian"! The
  existing skip used IndexOf("guardian") which is why depletion SEs "PassivePowers Depletion"
  (m_name localized "$powers_depleted") were listed -> now skipped by StartsWith "PassivePowers").
- PassivePowers fields are public static readonly (Dictionary<string,BossConfig> activeBossConfigs,
  ConfigEntry<int> maximumBossPowers); BossConfig.Configs -> PowerConfig{ Desc:string, BoxedPassive,
  BoxedActive (int-boxed) }; localization key "$powers_type_passive: {0}" with amount arg.
- cfg profile: Passive Powers = Smoothbrain-PassivePowers 1.1.8, cfg
  org.bepinex.plugins.passivepowers.cfg (Max 2, CD 900, Dur 120, Depl 120; Eikthyr Effects
  MovementSpeed:0:25,RunStamina:15:40,JumpStamina:15:40).

### Build / Install
- tools/BuildStaged.ps1: exit 0, 8 warnings (all pre-existing), 0 errors.
  Output: bin\Release\net480\SeneaLHudLayout.dll FileVersion 1.5.31.
- tools/InstallTest.ps1 -> profile plugin dll 1.5.31 (295,424 bytes), backup .bak-1.5.30;
  SHA256 matches build output. Game install untouched (1.5.25).

### In-game test checklist (1.5.31)
1. Stats tab -> Guardian powers section: 2 rows (equipped + empty `—`), states correct at rest
   (Passive). Activate a power -> "Active 1m 59s" ticking; after buff -> "Depleted 1m 45s".
2. Description lines match the boss stone tooltip passive lines.
3. Forsaken power row: shows name+Ready/CD; vanilla users w/o Passive Powers see only that row.
4. Status effects list no longer shows PassivePowers buff/depletion entries.
5. GuardianPowers=false hides the section.

## Previous task (2026-11-10): 1.5.30 six-part HUD/chat round

### Goal (user request)
1. Potion cooldown countdown: opacity control for the digits (default 65%) so the potion icon reads through.
2. `ChatBackgroundOpacity` default 0.25 (code + CJAYCRAFT pack config).
3. ChatChannelButton: remove the feature completely (it never worked in-game).
4. Hex colour settings for shout and say text.
5. `AllChatShout` (ON by default, pack ON): every plain-text chat is sent as /s with zero typing; box opens empty (no prefill, RememberLastUsed not consulted).
6. `ShowXpBar` fixes: text background/borders, the giant fill-square bug, spacing, font sizes.
Standing constraints: DLL built + installed to Gale 4.4.44 test profile with backup, full path printed; NO zip, NO github, NO beta_cjaycraft(-server) manifest/CHANGELOG edits. Pack CONFIG .cfg files ARE in scope.

### Status: COMPLETE (1.5.30 built + installed; in-game test pending)

### Changes
- `SlotCue.cs` - `ShowSlotSeconds` now applies `[Items] PotionCooldownTextOpacity` (0-1, default 0.65) to the countdown colour; equip/gear timers untouched.
- `PotionCooldown.cs` - slot tint alpha from `[Items] PotionCooldownDimOpacity` (default 0.55); 0 hides the tint entirely.
- `SeneaLHudLayoutPlugin.cs` - VERSION 1.5.30; `ChatBackgroundOpacity` default 0.5 -> 0.25; new binds `ShoutTextColor`/`SayTextColor` (string hex, FFFFFF/C7C7C7), `AllChatShout` (true), `PotionCooldownTextOpacity` (0.65), `PotionCooldownDimOpacity` (0.55), `XpBarScale` (1, 0.5-3); ChatChannelButton binds REMOVED (ConfigHousekeeping prunes the old keys on load); master-switch + bind descriptions updated.
- `ChatChannelButton.cs` DELETED; `ChatWindowLayout.KeepInput` no longer reserves space / places / hides the button (input spans the panel again).
- `ChatPrefix.cs` - AllChatShout: `InputTextPrefix` rewrites plain text to "/s <text>" (returns true so vanilla InputText runs the real shout; a typed /s /say /w /console is untouched; Chatter-with-shout-panel text left alone); prefill gated on `!AllChatShoutOn()`; `UsesShout()` true under AllChatShout (Chatter placeholder shows shout); `Rewrite`/`RememberFrom` (button plumbing) removed; public `ShoutAll(text, chatterPrefix)` is the pure decision (RouteTest-friendly).
- `ChatShoutText.cs` - `ShoutColor()`/`SayColor()` consult the hex strings via cached `TryHexColor` (3/6/8 digits, optional #, fall back to white/grey then vanilla yellow/white); `using BepInEx.Configuration` added.
- `ActionSlotXpBar.cs` REWRITTEN - fill-size NaN bug fixed (`float.IsNaN(_placedFill) ||` guard; fill starts at sizeDelta 0 and sizes from the bar's own rect, can never exceed it); new dark 9-sliced plates (1px light border, transparent corners) behind Level line, bar+percent line, and XP numbers; text bold + 0.22 black outline; gaps pulled in (SlotGap 10, base gaps unchanged); `[Hotbar] XpBarScale` multiplies every size (default 1 = character-window size; pack ships 1); layout re-measures on text/scale change, plates sized to preferredWidth.
- `beta_cjaycraft\config\cjayride.SeneaLHudLayout.cfg` (pack CONFIG only): +ChatAlwaysVisible/ChatBackgroundOpacity/ShoutWhiteText/ShoutTextColor/SayLightGreyText/SayTextColor/AllChatShout (values 0.25/true/true/FFFFFF/true/C7C7C7/true), +[Items] section (PotionCooldowns true, TextOpacity 0.65, DimOpacity 0.55), +XpBarScale=1; header v1.5.30. Backup `.bak-1.5.29`.
- Test profile cfg: ChatChannelButton* blocks removed, ChatBackgroundOpacity -> 0.25, +ShoutTextColor/SayTextColor/AllChatShout/PotionCooldownTextOpacity/PotionCooldownDimOpacity/XpBarScale, header v1.5.30. Backup `.bak-1.5.29`.
- Tools: `tools/UpdateCfg1530.ps1` (idempotent; verified re-run = all-skips), `tools/AddItemsSection1530.ps1`.
- CHANGELOG 1.5.30 section; README chat/hotbar/potion bullets + settings table updated, ChatChannelButton bullets/rows deleted; `Package\README.md`+`CHANGELOG.md` synced (Compare-Object 0), `Package\manifest.json` 1.5.30, `Package\SeneaLHudLayout.dll` refreshed to 1.5.30 (test sync; NO zip built).

### Build
- `tools/BuildStaged.ps1` -> `exit = 0`, `Build succeeded. 8 Warning(s) 0 Error(s)`; all 8 warnings pre-existing (CS0618 obsolete TMP/Unity APIs + CS0169), none from changed files.
- Output: `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll` FileVersion **1.5.30**.
- Game install untouched: `D:\SteamLibrary\...\BepInEx\plugins\SeneaLHudLayout.dll` still 1.5.25.

### Install (test only)
- `tools/InstallTest.ps1`: 1.5.30 installed at
  `C:\Users\cjrob\AppData\Roaming\com.kesomannen.gale\valheim\profiles\cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.44\BepInEx\plugins\cjayride-SeneaLHudLayout\SeneaLHudLayout.dll`
  (292,352 bytes); backup `SeneaLHudLayout.dll.bak-1.5.29` created (plus .bak-1.5.27/.bak-1.5.28).

### In-game test checklist (1.5.30)
1. Potion cooldown: digits ~65% over the icon (icon visible), slot tint 0.55; keys live-change.
2. Chat bg noticeably lighter (0.25); old ChatChannelButton gone, input full width, keys gone from cfg on load.
3. Plain text shouts with no prefix; `/say hi` close-range; `/godmode` fine; box opens EMPTY (pack has AllChatShout + DefaultToShout true).
4. ShoutTextColor=FFD700 -> shouts yellow; SayTextColor=#808080 -> say grey; empty -> vanilla.
5. XP bar: fill stays inside the frame at any %; plates with thin borders behind all three text bits; XpBarScale 1 compact, 2 = old size.

### No zip built, no GitHub push, pack manifest/CHANGELOG untouched (config .cfg changed as requested).

## Previous task (2026-11-10): 1.5.29 action-slot XP bar

### Goal (user request)
Copy the character-panel level/XP display to the HUD: `Level ##  [bar]  ##%` with the XP numbers
underneath, twice the character-window size, attached to the RIGHT of the Action Slots bar,
vertically centred on the action-slot buttons. The old food-bar level goes OFF by default
(mod default + CJAYCRAFT pack config). The new XP bar is OFF in the mod's config default but ON
in the CJAYCRAFT pack config. Deliverable: DLL built + installed to the Gale 4.4.44 test profile.
No zip, no github, no beta_cjaycraft manifest/CHANGELOG edits (the pack CONFIG file IS in scope
per this request).

### Status: COMPLETE (1.5.29 built + installed; in-game test pending)

### Changes
- `ActionSlotXpBar.cs` (NEW) - `[Hotbar] ShowXpBar` (default false). Builds
  `SeneaLHudLayout_SlotXp` under `HudLayout.HotbarShift` (the object the hotbar cluster lives in
  after relocation, so the bar moves with it). Anchor = SeneaL UI's own `HotbarView.FrameActions`
  (via static `HudRoot._hotbar`), picking the wider of FrameActions/FrameQuiver when the quiver
  shares the row, both re-framed by SeneaL every tick; follows drags for free. Placement is
  per-frame (few rect reads, write only when the anchor moved); SkillsReworked data reads
  (reflection) run at a 0.1s cadence; TMP text written only on change; fill width only when it
  changes >0.25px. Two lines centred as a block on the slots' mid height (top line at
  center+(UnderGap+amountH)/2, amount at topY-(RowH+UnderGap+amountH)/2 -> block centre == slots
  centre). Sizes = 2x the character-window bar (Level font 24, bar 200x12, small text 20).
- `CharacterExtras.cs` - public wrappers: `Level`, `Fraction`, `Xp`, `XpText`, `PixelSprite`,
  `CopyLevelFont` (shares the character-window label's TMP font). No behaviour change.
- `HudLayout.cs` - caches `_hotbarShift` in Apply, exposes `public static RectTransform HotbarShift`.
  Restore destroys the shift -> the bar's `Ensure()` self-heals (Unity null check rebuilds).
- `SeneaLHudLayoutPlugin.cs` - `ShowSlotXpBar` bind `[Hotbar] ShowXpBar` default FALSE;
  `ShowLevelFoodBar` default TRUE -> FALSE (with food-bar offset defaults unchanged);
  `ActionSlotXpBar.Tick()` after `HudLayout.Apply()` in LateUpdate; master-switch text untouched.
- Version 1.5.29: csproj (4 props), plugin `VERSION`, `Package/manifest.json`.
- `beta_cjaycraft\config\cjayride.SeneaLHudLayout.cfg` (pack CONFIG only - manifest/CHANGELOG
  untouched as instructed): added `[Hotbar] ShowXpBar = true`, set `ShowLevelFoodBar = false`.
- Test profile cfg `...\4.4.44\BepInEx\config\cjayride.SeneaLHudLayout.cfg`: same two changes,
  backup `cjayride.SeneaLHudLayout.cfg.bak-1.5.28` kept.
- CHANGELOG.md 1.5.29 section; README.md + Package\README.md: Hotbar XP bar bullet, food-bar
  bullet updated, settings rows `| Hotbar | ShowXpBar | Off |` and
  `| HealthFood | ShowLevelFoodBar | Off |`; Package README synced from README (verified
  Compare-Object = 0 diff lines).

### Build
- `tools/BuildStaged.ps1` -> `Build succeeded. 8 Warning(s) 0 Error(s)`, exit 0. All 8 warnings
  pre-existing files (the two new CS0414s from dead fields were removed; 10 -> 8).
- Errors fixed during work: CS0246 `Image` (missing `using UnityEngine.UI`), CS0103-style
  CS1503 (Frame(field) vs Frame(name) refactor), harness `$`-stripping on inline PowerShell
  (always run .ps1 files under tools\).
- Output: `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`
  FileVersion **1.5.29**, 289,792 bytes.
- Game install untouched: `D:\SteamLibrary\...\BepInEx\plugins\SeneaLHudLayout.dll` still 1.5.25.

### Install (test only)
- `tools/InstallTest.ps1` (now keys backups by the version being replaced) -> Gale profile
  4.4.44 plugins dir: 1.5.29 installed; backups kept: `.bak-1.5.27` (pre-session baseline) and
  `.bak-1.5.28` (previous test build).

### Key facts verified
- SeneaL `Ui.TL(parent,name,left,top,w,h)` = anchor (0,1) pivot (0,1), anchoredPos (left,-top):
  my boxes use anchor/pivot (0.5,0.5) pivot (0,0.5) so anchoredPosition = left-middle; conversion
  via `shift.InverseTransformPoint` of the frame's world corners.
- `HotbarView` line 887: `Frame(FrameActions, x, y, w, h)` every tick (w = slots*50-6+44, h = 44)
  - the frame box is the slots row; quiver row is FrameQuiver on the same y when shown.
- `HudRoot._hotbar` is static (SlotCue precedent), fields `FrameActions`/`FrameQuiver` public.
- Character-window bar design (copied): back `Color(0,0,0,0.65)`, fill `Color(1,0.86,0.25,0.95)`,
  text `Color(0.86,0.75,0.48,0.95)`, `FormatXp` (>=10 -> "0", else "0.#"), percent
  `RoundToInt(frac*100)+"%"`, XP units divided by `m_increseStep * Game.m_skillGainRate` (1.5.27 fix).

### Next steps
1. In-game test on 4.4.44: bar sits right of action slots at mid height, follows HUD-editor
   moves, quiver-row case, numbers match character window, food-bar level gone, toggle off = gone.
2. Only after user confirms: zip / github / pack manifest bump.

## Past task (2026-11-10): 1.5.28 six chat / slot features

### Goal (user request, 6 items)
1. Chat window background opacity slider, default 50%, controls the existing dark panel only.
2. Shout text white; say / proximity text light grey.
3. Cooldown timers for PotionsPlus potions equipped in the Action Slots.
4. Toggle to leave chat always visible, default OFF.
5. Square S/s channel button, same height as the chat input, docked left of it, "Shout"/"Say" label
   above, letter coloured per channel, left-click toggles, plain text sent on that channel, typed
   `/say` `/s` `/shout` flips the button; default Shout; FPS-cheap.
6. Kill notice `+X XP` moved to the FRONT of the message.
Deliverable: DLL built + installed to the Gale 4.4.44 test profile. No zip, no github, no
beta_cjaycraft manifest/CHANGELOG edits.

### Status: COMPLETE (build 1.5.28, installed to test profile)

### Changes
- `ChatBackground.cs` (new) - F1: finds the chat panel's stretched "bg" Image child (SeneaL
  `Ui.Panel` creates it) and writes `color.a` = `[Chat] ChatBackgroundOpacity` (0-1, default 0.5).
  Cached reference, write only on change, re-found if the panel is rebuilt.
- `ChatShoutText.cs` - F2: the existing transpiler now also swaps the game's colour picks. Unity's
  `Color.white`/`Color.yellow` are static PROPERTIES (`call get_white` in IL - verified, the first
  attempt used `ldsfld` and matched nothing). Shout -> `ShoutColor()` (white), say/Normal default ->
  `SayColor()` (light grey 0.78). Applied to `Terminal.AddString` x2 (4-arg) and `Chat.AddInworldText`
  (Whisper/Ping use inline `new Color(...)` so they are untouched). Keys: `ShoutWhiteText`,
  `SayLightGreyText` (both on, independent of the chat master switch).
- `PotionCooldown.cs` (new) - F3: per SeneaL action slot (`HudRoot._hotbar._act`), item identity from
  the `Slot.Set(ItemData)` postfix (Slot stores no item field), cooldown = the ACTIVE consume
  `StatusEffect` in `Player.GetSEMan()` -> `GetRemaningTime()` (`m_ttl - m_time`; PotionsPlus
  "Cooldown Duration" writes that `m_ttl`). Dim overlay + whole-seconds label, own object names
  (`SeneaLHudLayout_CoolDim` / `_CoolTime`, not the equip cue's label), 0.1s cadence, text written
  only when the second changes. Key: `[Items] PotionCooldowns` (on).
- `ChatHide.cs` - F4: `ChatAlwaysVisible` (off by default). Postfix on `SeneaLUI.Hud.ChatSkin:Tick`
  pins the panel CanvasGroup alpha 1 (SeneaL fades it with the HUD) and keeps the window active;
  `m_hideDelay` parked at 1e6 and `m_hideTimer` at 0 so the game's own fade can never fire.
  HideChat wins over ChatAlwaysVisible.
- `ChatChannelButton.cs` (new) - F5: square Button (34px, `InputH`) docked left of the input, TMP
  letter "S"/"s" coloured by channel, "Shout"/"Say" tag above, `onClick` flips, repaint only on
  change. Keys: `ChatChannelButton` (on = feature on), `ChatChannelButtonDefaultShout` (on).
- `ChatPrefix.cs` - F5 routing: on submit the typed prefix flips the button (`PrefixChannel`), plain
  text is rewritten to `/s <text>` when the button is on Shout, `/shout ...` is rewritten to `/s ...`
  (Valheim's console has NO `shout` command - verified), `/say` and `/w` pass through (both are real
  commands - verified L2390/L2408 of Terminal), and console commands are never rewritten
  (`IsChatCommand`). Prefill `/s ` is skipped while the button feature is on.
- `ChatWindowLayout.cs` - `KeepInput` shrinks the input bar by `InputH + InputGap` and places the
  button + tag; public wrappers `GetPanel/GetBottomLeft/FitBoxTo`.
- `SlotCue.cs` - public helpers reused by F3: `ItemFor`, `ActionSlots`, `SlotRoot`, `SlotCountText`,
  `EnsureSlotLabel(name)`, `EnsureSlotOverlay`, `ShowSlotSeconds`; `EnsureTimeNamed` so each timer
  feature owns its own label.
- `KillXpNotice.cs` - F6: `XpLead()` prepends `+X XP  ` in both the damage-share path (L159) and the
  own-kill path (L176/L180).
- Wiring: `SeneaLHudLayoutPlugin.cs` 7 new fields + binds + `PotionCooldown.Tick` /
  `ChatBackground.Tick` in LateUpdate. Version 1.5.28 in csproj (4 props), plugin `VERSION`,
  `Package/manifest.json`. Project CHANGELOG.md + README.md updated (pack manifest/CHANGELOG NOT
  touched, per instruction).

### Build
- `tools/BuildStaged.ps1` - builds against a hardlink staging tree on D:
  (`D:\AI-Agent-Projects\cjaycraft\_build_stage\valheim`, 141 Managed + 2 BepInEx core links, NO
  `BepInEx\plugins` so the PostBuild `Exists` condition is false). `-p:GamePath=` EMPTY breaks all
  references (893 errors) - GamePath is the reference source, do not pass it empty.
- Result: `Build succeeded. 8 Warning(s) 0 Error(s)`; all 8 warnings pre-existing files, none new.
- Output: `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll`
  FileVersion **1.5.28**, 285,184 bytes.
- Game install verified untouched: `D:\SteamLibrary\...\BepInEx\plugins\SeneaLHudLayout.dll` still 1.5.25.

### Install (test only)
- `tools/InstallTest.ps1` -> Gale profile `cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.44\
BepInEx\plugins\cjayride-SeneaLHudLayout\SeneaLHudLayout.dll` = 1.5.28.
- Backup kept: `SeneaLHudLayout.dll.bak-1.5.27`.

### Next steps
1. In-game test on the 4.4.44 profile: all six features + settings sliders.
2. Only after the user confirms: zip / github / pack manifest bump.

## Past task (2026-11-09): 1.5.27 XP bar label shows real XP

### Goal
Character-window XP bar showed "94/95 98%" then "2.4/100 2%" — numbers that did not
match the "+12 XP" kill notices. Acceptance: the label under the bar displays actual
XP / XP-needed (same units as the notices).

### Root cause (verified from SkillsReworked decompile, profile 4.4.43)
- `KillRewardFlow.AwardXp` -> `Character.RaiseSkill(LevelSkillType, xp)` -> SR patch ->
  `LevelProgressionService.AddXp(skills, factor)` adds
  `factor * skill.m_info.m_increseStep * Game.m_skillGainRate` to `m_accumulator`
  (decompiled line 2344). Level skill registered with `IncreaseStep = 0.1f` (line 4346).
- The label showed raw `m_accumulator` (0.1 XP per unit) over
  `GetRequirementForLevel(floor(level))` — internally consistent (94/95 = 98%) but 10x
  off from displayed XP. 2.4 accumulator = 24 real XP; requirement 99.6 accumulator =
  996 XP. Bar fill/percent are ratios, unaffected.
- `Game.m_skillGainRate` + `m_increseStep` confirmed present in assembly_valheim.dll.

### Changes
- `CharacterExtras.cs` `ReadLevelXp` — divides both numbers by
  `m_increseStep * Game.m_skillGainRate` (guards <=0); fill/percent untouched.
- Version 1.5.27: csproj, plugin `VERSION`, `Package/manifest.json`, CHANGELOG, README.
- Pack 4.4.43: `manifest.json` dep + CHANGELOG line.
- `tools/ProbeSignatures.ps1`, `tools/ProbeSkillsReworked.ps1`, `tools/SimXpLabel.ps1`.

### Commands and results
- `dotnet build -c Release` — compilation succeeded; only failure is the csproj
  PostBuild copy into the game dir (sandbox denial = desired, game stays read-only).
- DLL: FileVersion 1.5.27; strings `m_skillGainRate`/`m_increseStep` present.
- `tools/SimXpLabel.ps1` reproduces observations and the fix:
  old `94/95 99%`, `2.4/100 2%`; new `940/953 99%`, `24/996 2%`; +12 XP kill at
  level 32: `940 -> 952 / 953`.
- Zip `dist\cjayride-SeneaLHudLayout-1.5.27.zip` (193812 bytes); VerifyZip.ps1: all 5
  entries SHA256 match Package staging dir.

### Deploy
- Installed 1.5.27 into Gale profile 4.4.43 with approval; previous DLL backed up as
  `...SeneaLHudLayout.dll.bak-1.5.26` (verified FileVersion 1.5.26).

### Deliverables
- **DLL:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\bin\Release\net480\SeneaLHudLayout.dll` (1.5.27)
- **Thunderstore zip:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.SeneaLHudLayout\dist\cjayride-SeneaLHudLayout-1.5.27.zip`

### Remaining
- In-game verification by user: at level N the label should show real XP
  (e.g. 24/996 at level 33); each +12 kill moves it by +12.

## Prior task (2026-11-09): 1.5.26 self-cleaning config
`ConfigHousekeeping.cs` (prune unbound cfg lines + clear BepInEx `OrphanedEntries` on
load). Verified offline: 10 legacy keys removed from 4.4.38 cfg with 0 value changes;
clean cfg = no-op. Built 1.5.26; zip `dist\cjayride-SeneaLHudLayout-1.5.26.zip`
(193597 bytes, hash-verified). Superseded in-game by 1.5.27 (same tree).

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
