# Changelog

## 1.5.16

- `[Performance] SeneaLFpsTweaks` is **experimental** and **off by default** (was on in 1.5.15). No noticeable FPS gain reported in testing.

## 1.5.15

- `[Performance] SeneaLFpsTweaks` (on): throttle optional SeneaL UI ticks (ore scout, map pins, text sizers, debug/inspector, inventory skin while closed). Vitals, hotbar, hover, and messages stay every frame. Turn off if something looks delayed.

## 1.5.14

- Item tooltips show a small item sprite on the type line (`[ItemTooltips] ShowItemIcon`, on). Same row height; not SeneaL's large 52px TooltipIcon box.

## 1.5.13

- Hide shouted chat above player heads (`[Chat] HideShoutOverHead`, on by default). Shouts still appear in the chat log.
- Chat log can scroll again after shrinking/moving the window: mouse wheel, and Page Up / Page Down while the chat box is open.
- Item tooltip bullet/stat lines no longer show a shield (or other) icon in front of each row.

## 1.5.12

- Thunderstore: use the correct `icon.png` in the package (replacing the placeholder from 1.5.11).

## 1.5.11

- Character Stats: fixed HarmonyX console warning `Could not find method ... StatusEffect.GetRemainingTime`. Remaining status time now uses Valheim’s `m_ttl` and `m_time` fields (there is no `GetRemainingTime` on `StatusEffect`).

## 1.5.10

- Food-bar level no longer slides left when you eat. It follows the food **icon slots**, not the `time*` labels that appear and move on eat/death. OffsetX/Y stay a manual nudge. The food HUD itself can still move.
- Item tooltips no longer show a Path of Valheiman (Buck & Doe) icon on every stat line.
- Fixed Harmony warning: `Tooltip.RowUi` has no `Head` field — that flag is on `Tip.Row`.

## 1.5.9

- Thunderstore package no longer includes `config/cjayride.SeneaLHudLayout.cfg`. BepInEx still generates it on first run.

## 1.5.8

- Default `[Character] ShowLevelFoodBarOffsetX` is **12** (slight right nudge from slot center). Existing configs keep their saved value.

## 1.5.7

- Default `[Character] ShowLevelFoodBarOffsetX` is **0** (was -45). Existing configs keep their saved value. With slot-based centering from 1.5.6, 0 is the intended neutral.

## 1.5.6

- Default `[HealthFood] OffsetY` is **-75** (was -54). Existing configs keep their saved value.
- Food-bar level (`ShowLevelFoodBar`) is pinned to the food-timer *slots* (anchored positions), not the live text/mesh bounds. Digit changes, dying, and shrinking the label no longer slide `ShowLevelFoodBarOffsetX`.

## 1.5.5

- **`[HealthFood] HealthBarMaxHeight` (default 250):** clamp SeneaL's HealthCapacity bar length *after* it is calculated, then let fill / ghost / keep-space run as current / max of that height. No more snapping the RectTransform (that caused flicker). SeneaL ticks, numbers, shapes, and Path of Valheiman shield overlay stay as SeneaL draws them.

## 1.5.4

- **`HealthBarMaxHeight`:** stop fighting SeneaL's health-bar lerp (that was the flicker at 250+ HP). The cap is now SeneaL's own max bar height; fill stays current / max in that same space. HP numbers and tick marks still show extra capacity.

## 1.5.3

- Default `[Character] ShowLevelFoodBarOffsetX` is **-45**.

## 1.5.2

- Food-bar level uses the same bottom-left anchor space as the food timers, so the label centers on the timer group at `ShowLevelFoodBarOffsetX = 0` instead of sitting ~30px too far right.

## 1.5.1

- Food-bar level stays centered under all food timers when HP, food, or death state change the HUD. Reset `[Character] ShowLevelFoodBarOffsetX` to **0** (old -33 / -63 defaults are migrated). Offset keys are only a manual nudge now.

## 1.5.0

- **Character Stats sheet:** new tab on the SeneaL character panel with a live vitals, defense, buffs, and skills view. Settings: `[Stats] StatsTab` (on), `[Stats] StatsHotkey` (unbound), `[Stats] StatsPortrait` (Steam portrait, on). Client-only; reads vanilla player data.
- **`[HealthFood] HealthBarMaxHeight`** (default 250): caps SeneaL player health bar height at very high max HP; fill still tracks current / max.
- **Item tooltips:** descriptions wrap at normal width; width grows only for long Magic Effects rows. Repair tips still widen as before. Magic effect lines stay on one line where possible.
- Item tooltip layout settings moved from `[Stats]` to **`[ItemTooltips]`** (`ValuesBesideLabels`, `LeftOverflow`). Existing `[Stats]` values migrate on first load.
- Default `[Character] ShowLevelFoodBarOffsetX` is **-33** (was -63).

## 1.4.1

- **Death pin on compass:** when you die, the grave pin is marked for the SeneaL compass automatically (`[Compass] DeathPinOnCompass`, on by default). Needed when SeneaL UI `[Map] CompassPins` is **Marked** — Valheim adds death pins unchecked, so they otherwise stay off the compass until you tick them in the pin dialog.
- Food-bar SkillsReworked level stays centered under the timers as the digit count changes. Default `[Character] ShowLevelFoodBarOffsetX` is **-63**.

## 1.4.0

- **Chat window layout** (`ChatWindowPosX`, `ChatWindowPosY`, `ChatWindowWidth`, `ChatWindowHeight`): move and resize the whole SeneaL chat — glass, log, and typing bar together. **0 is SeneaL's original** on each axis; negative width/height shrinks. Values are **integer pixels** with 1-step F1 sliders (-400..400).
- Shorter height **clips the log** inside the window and **pins the newest lines** above the input bar. **Reset all four keys to 0** rebuilds SeneaL's original chat, including after a restart once the panel has settled.
- **`ChatWindowRecommendedLayout`**: optional preset (PosX 84, PosY -88, Width -88, Height -9). While on, the four manual keys are ignored but remain saved in the cfg.

## 1.3.29

- New `[Chat] ChatSettingsEnabled` (off): master on/off for every Chat setting except `HideChat`. Off leaves shout prefix, last-used channel, shout capitalization, hide delay, and chat font at the game's own behavior. `HideChat` is unchanged.
- `[Chat] RememberLastUsed` and `[Chat] chatMessageFontEnabled` now default off.

## 1.3.28

- Removed the character Stats window (tab, hotkey, portrait). Tooltip `[Stats] ValuesBesideLabels` / `LeftOverflow` are unchanged.

## 1.3.27

- `[Powers] HideSenealSlot` now hides SeneaL's guardian-power slot even when Passive Powers is not installed. With Passive Powers present, only the unused circle is hidden so the extra icons still layout.

## 1.3.26

- Removed chat window move and resize (handles, gold overlay, F1 Pos/Width/Height, lock/reset). Chat font, shout prefix, capitalization, hide delay, and HideChat stay.

## 1.3.25

- Chat drag follows the mouse in the panel's own plane (no more screen-pixel / local-offset mix that flung the window). Moving the chat moves the whole window — text, input, and background together. The gold frame covers that whole union. Corners still resize.

## 1.3.24

- Chat move/resize no longer depends on Unity UI handles. Hold left Ctrl and a gold IMGUI frame is drawn on the real SeneaL chat panel; drag the body to move, corners to resize. The panel is written into SeneaL's own `_tgtPos`/`_tgtSize` so Glide cannot fight it.
- F1/`ChatWindowPosX`/`Width`/`Height` apply immediately. Width or Height 0 keeps the live size for that edge; you no longer need all four values set before anything moves.

## 1.3.23

- Chat adjust: holding left Ctrl (or right Ctrl) after opening chat with Enter now shows a gold frame on the visible chat panel so you can drag corners to resize and the body/edges to move. Left Control is detected even when the chat box has focus.
- Chat window **PosX, PosY, Width, Height** are in the cfg (`[Chat] ChatWindowPosX/Y`, `ChatWindowWidth`, `ChatWindowHeight`). Dragging writes them; you can type values by hand. Width/Height 0 means "use the game's size until you set one".

## 1.3.22

- Build-menu search memory is hammer-only. Switching to the Hoe or Cultivator forgets the last hammer search and clears the box, so leftover text cannot empty those grids.

## 1.3.21

- Chat adjust: the handle canvas now has a GraphicRaycaster, a full-panel drag target while Ctrl (or RMB) is held, and StartAttack is skipped over the chat so the click resizes/moves the window instead of using the hotbar item. Gold handles only take clicks while you are actually adjusting.
- Piece health labels assign a Valheim TMP font before the text component is created, so Unity no longer warns that LiberationSans SDF is missing on `SeneaLHudLayout_PieceHealth`.

## 1.3.20

- Chat window controls are on again: shout prefix, shout capitalization, chat font, hide delay, and move/resize handles (hold left Ctrl or the right mouse button; lock and reset hotkeys).
- Stats window is on again. It lists your own worthwhile stats only (defense, vitals, mobility, resistances, effects, weapons, skills). The other-players comparison section is gone.
- Stats tab search and row refresh avoid extra per-frame work so the inventory/stats UI stays cheap while you play.

## 1.3.19

- **New Stats window - a Diablo-style character sheet.** A new **Stats** button appears in the character window tab bar, on the left of Texts. It opens a draggable window with your **character name and Steam portrait at the top**, then **two organized columns** of stats:
  - **Defense** (left): total armor and armor per piece (helmet/chest/shoulders/legs), vitals (health, stamina, eitr, adrenaline, carry weight, comfort), mobility (equipment move-speed and stealth modifiers), all eight resistances (slash, pierce, blunt, fire, frost, lightning, poison, spirit, colour-coded Weak/Normal/Resistant/Immune), and your active status effects with their timers.
  - **Offense** (right): your equipped weapons with per-type damage and block power, all your skills sorted highest first, and an **"On the server"** section listing the other players online with their level and total skill so you can see how you stack up (green = you lead, red = they do).
  - The window **drags by its title bar** and remembers where you put it (game prefs, never the config). It scrolls, sits above the inventory, and has its own close X.
  - Registered through **SeneaL UI's own public mod API** (`SeneaL.API.CharacterTabs`), so the tab is SeneaL-styled, gets the `shield-outline` icon, and plays nice with other mods that add tabs. A small postfix reorders the tab strip so Stats lands left of Texts.
  - Settings: `[Stats] StatsTab` (on, master switch), `[Stats] StatsHotkey` (unbound), `[Stats] StatsPortrait` (on), `[Stats] StatsRivals` (on). Client-only. Needs SeneaL UI for the tab button; the window itself reads only vanilla Valheim data.

## 1.3.18

- **The chat window handles are fixed.** They used to be parented to the chat window itself, which is why holding Ctrl showed nothing: the game hides that window about ten seconds after you open it, and SeneaL UI fades its canvas and covers it with its own panel, so the handles were invisible and unclickable. The handles now live on the chat canvas, above SeneaL UI's panel and above the fade, and they track the **visible chat panel's** on-screen rectangle - not the window node's. (Verified in game: with SeneaL UI, `Terminal::m_chatWindow` is a zero-size anchor node, so mirroring its corners drew the four corner handles as a dot at the screen centre - the "4 yellow squares around my crosshair".) The handles are always click-catchable and only their colour is toggled, so a press-and-drag always starts; the root hides with the chat and never draws over a hidden window. Open chat (and talk), hold the adjust key (left Ctrl) or press and hold the right mouse button, and the gold handles appear on the corners and edges; release to hide them. Corner handles resize, edge handles move.
- New `[Chat] HideChatPanelDelay` (default **10**): how many seconds the chat panel stays visible after the last chat activity. It writes the game's own chat hide timer, so this is the value Valheim ships with; raise it to keep the panel up longer, lower it to close sooner. Client-only.
- **`ShoutKeepsCapitalization` now reads the way it is named, and defaults to off.** Off (default) = the shout does NOT keep Valheim's shout capitalization: it shows the letters exactly the way you typed them, so a shout of "hello there" stays "hello there". On = Valheim's own ALL CAPS shout. Whispers stay lower case, as Valheim makes them.
- A chat font that is not loaded now logs the font assets the game actually has loaded, so a wrong name is easy to spot in the log. The name match also accepts a loaded asset that starts with the name you set (the shipped `Valheim-AveriaSansLibre` object is not always named exactly that in game).

## 1.3.17

- New chat font settings, in the style of Chatter's own ChatMessage.Text.Font keys: [Chat] chatMessageTextFontAsset (default Valheim-AveriaSansLibre) and [Chat] chatMessageTextFontSize (default 16, range 6-64). They set the font and size of the chat log text. The asset name is matched against the TMP font assets loaded in the game, so any of Valheim's shipped fonts works (Valheim-AveriaSansLibre, Valheim-AveriaSerifLibre, Valheim-Norse, Valheim-Norsebold, Valheim-Prstartk, the NotoSans/NotoSerif language sets, LiberationSans SDF, and the rest), plus any font another mod loads. [Chat] chatMessageFontEnabled (on) turns the pair off if you want the chat UI's own font back. Chatter is not touched: its own keys keep driving Chatter's panel.
- New: **resize the chat window by dragging a corner, move it by dragging an edge.** While the chat window is open, hold the adjust key (default left Ctrl) - or use the **right mouse button**, no key needed - and small gold handles appear on the window's corners and edges. Drag a corner handle to resize, drag an edge handle to move. The window stays on screen (clamped) and never smaller than a usable minimum. Position and size are saved in the game's own player prefs file, so they survive restarts, and they never dirty the config file. With SeneaL UI installed, a taller window also shows more chat lines (the visible line count follows the height).
- New: **lock the chat window in place.** [Chat] ChatWindowLocked (off). While locked, dragging the handles does nothing and the saved position and size are re-applied every frame, so the window cannot drift. [Chat] ChatWindowLockKey (unbound by default) toggles the lock in game; [Chat] ChatWindowResetKey (unbound) puts the window back at the game's own placement and forgets the saved rect.
- [Chat] ChatWindowAdjust (on) is the master switch for the drag/resize feature; off leaves the chat window exactly where SeneaL UI / Valheim put it. Client-side only; nothing about how chat is sent is changed.

## 1.3.16

- `HideChat` now defaults to **off**. SeneaL UI's chat window shows by default, and you turn it off yourself when a chat mod like Chatter is doing the showing.
- Chat shout redone around Valheim's real commands. There is no `/y` in Valheim, so the `/y` alias is gone. `/s` is Valheim's shout and plain text is Valheim's say; this mod no longer invents or redirects anything.
- With `[Chat] DefaultToShout` on, the chat box **opens with `/s ` typed in it**, so your first Enter shouts and you never type the prefix. Delete the `/s ` and the message is a normal close-range say.
- `[Chat] RememberLastUsed` (on): the box prefills `/s ` only while shout is the channel in use — after a message sent with `/s`, and not after one sent as plain text. The channel is stored in the same player prefs file the game uses for its chat key binds, so it is still remembered after you quit and come back. Pressing Enter on an untouched `/s ` sends nothing, so it leaves no stray line in chat.
- New `[Chat] ShoutKeepsCapitalization` (on by default): Valheim prints shouted chat in ALL CAPS, in the chat log and above your character. This keeps the letters the way the player typed them, so a shout of "hello there" stays "hello there". Whispers stay lower case, as Valheim makes them, and what reaches other players' wires is unchanged.
- `[Chat] SlashSMeansSay` is gone, with the `/y` work it belonged to.
- Off by default and client-side only: with `DefaultToShout` off the chat box is Valheim's, untouched.

## 1.3.15

- New `[Chat] DefaultToShout`, off by default. With it on, the chat box speaks the server's language: `/y` is yell (the long-range shout that shows above your character), `/s` is say (close range), and **a message with no prefix is sent as yell**, so you never have to type the prefix again. `/w` is left to Valheim, and nothing else in the chat box changes.
- `[Chat] RememberLastUsed` (on): the next message with no prefix uses the channel you used last — yell after `/y` or `/shout`, say after `/s` or `/say`. It is stored in the same player prefs file the game uses for its chat key binds, so it is still remembered after you quit and come back.
- The channel shows in the chat box. When Chatter is installed, its grey placeholder text moves to `/shout` when the next plain message will yell and `/say ` when it will say, using Chatter's own setter. Nothing in Chatter is patched.
- `[Chat] SlashSMeansSay` (on): `/s` sends a close-range say, the way it reads on a server. Valheim's built-in `/s` has always meant shout, so turn this off to hand `/s` back to Valheim and keep `/y` as the only yell alias.
- Nothing is invented. Valheim has no `y` command — this mod supplies it, and it yields the moment a server registers its own `/y`. The shout is sent through the game's own shout command, so the range check and the message RPC are Valheim's.
- Off by default and client-side only: no behaviour change, no server-side change, and no change to any other mod's chat until you turn it on.

## 1.3.14

- The shard window is now wide enough for its own words. The panel width is measured from the item name (with rarity) and the socket hint line, so neither is cut off; long names widen the window up to a cap, and short ones stay compact.

## 1.3.13

- E works everywhere again. The shard hotkey releases the Use press whenever the crosshair is on a world `Interactable` (signs, portals, campfires, chests, carts, pieces) or on a creature, so nothing is swallowed for socketed-item windows. Items in a foreign container (chest, cart, Haldor) also keep their container mod's Use key: the real container flag is passed to Epic Loot, which yields the press exactly like its stock behaviour.
- The shard panel now opens at the screen center and is draggable: left-click and drag the title bar to move it. The window stays on screen (clamped).
- The panel is compact. Its width fits the item's socket count (plus a margin), and its height is title band, one socket row, hint, and padding — 1-2 socket items are much smaller than before.
- The item's own icon now sits to the left of the item name in the panel title.

## 1.3.12

- The wishbone keeps working in a key slot. With `WishboneInKeySlot` on, the finder ping (the same equip effect a worn wishbone grants) now runs while the wishbone sits in a key slot, because the game only grants it to worn utility items. The effect is removed when the wishbone leaves the key slot, and vanilla takes over cleanly when you wear it in the charm cell.

## 1.3.11

- E sets portal tags again. The shard hotkey only runs while the inventory is open and the cursor is on a socketed item. It no longer swallows Use for the equipped item.

## 1.3.10

- Dragged shard/item icons stay in front of the shard window (SeneaL drag layer 28000; panel is 27000).

## 1.3.9

- Shard window sorts above SeneaL inventory/craft (SeneaL selection layer is 28000; this panel is ~29000).

## 1.3.8

- Shard sockets open in the hosted SeneaL window again. Epic Loot's default panel is no longer left behind the crafting UI. Sell/recycle still does not open sockets.

## 1.3.7

- Selling or recycling a socketed item no longer opens the shard window or reparents the player inventory into it. E at Haldor/recycle stays a sell/recycle confirm. The shard panel is only hosted when you open it on purpose, and it closes if that item leaves the bag.

## 1.3.6

- `[Stats] LeftOverflow` balances two-column item tooltips by moving whole sections (set, magic, stats) between columns for similar height. Combat always stays on the left. `Set` allows set on the left; `SetThenMagic` (default) also allows magic. `Off` restores SeneaL's layout.
- Rebuilds from a snapshot of SeneaL's row order so toggling the setting and re-hovering an item is safe.

## 1.3.5

- Shard window hint is only "Drag shard stones or runes into empty sockets."
- Gold border on each shard slot, matching the panel frame.

## 1.3.4

- Removed the inventory **Sockets** button. Hover a socketed item and press E (or the configured hotkey).

## 1.3.3

- Shard window no longer shows Epic Loot's cloned vanilla inventory wood background (`Bkg`) or extra title inside the panel. The SeneaL-styled frame stays; only shard slot cells remain.

## 1.3.2

- Fixed Harmony crash on Epic Loot `ShardUI.Open` (patch used `gui`; the method parameter is `invGui`). That exception aborted the rest of the shard-window patches, so E did nothing.

## 1.3.1

- Shard socket hotkey now intercepts Use (E) before the inventory closes, reads the hovered cell from SeneaL's cursor selection, and stops Epic Loot from immediately closing the socket panel because the vanilla player grid is hidden.

## 1.3.0

- Epic Loot shard socket window for SeneaL CustomUI. With `[EpicLoot] ShardSocketWindow` on (default), hover a socketed item in inventory and press `ShardSocketHotkey` (default E) or the Sockets button to open Epic Loot's shard row in a dedicated window. Add/remove uses Epic Loot 0.15.4 (`ShardUI.Open`); no need to turn CustomUI off. Client-only. Off restores previous behavior.

## 1.2.4

- Clicking the wishbone in a key slot picks it up instead of swapping it with the belt. With `WishboneInKeySlot` on, SeneaL treated it as a utility item (belt cell) because it was no longer a charm.
- `[BuildPanel] Scale` default is 0.8. The selected-piece card is drawn above the vitals without adding a Canvas (that hid hammer pieces and spammed the log).
- Wishbone right-click no longer equips it (that countdown unequipped the belt) and no longer drops it from a key slot. Left-click still picks it up.
- Stopped scanning the inventory on every change (that hitch when equipping a hip lantern).

## 1.2.3

- `[BuildPanel] Scale` (default 0.85) shrinks SeneaL's selected-piece build card from its current size. 1 is SeneaL's size. SeneaL also has `[Sizes] BuildPanel` in `seneaL.valheim.ui.cfg`; this setting is independent so you can scale it from Configuration Manager under this mod.

## 1.2.2

- `[Inventory] WishboneInKeySlot` (off by default). Turned on, the wishbone uses a SeneaL key slot instead of the charm / utility cell. Needs SeneaL key slots enabled. Stored in a key slot it is not worn, so the ping effect stays off until you put it back on the charm cell.

## 1.2.1

- Clarified `[Hover] ShowPieceHealth` and `ShowPieceHealthPercent`: they sit beside SeneaL's crosshair piece health bar and only appear when that bar is shown. On SeneaL UI 1.2.8+, keep **TooltipExtra** and **TooltipPlates** off in `seneaL.valheim.ui.cfg` (or use an equivalent layout), or the numbers stay hidden even when these options are on.

## 1.2.0
- UPDATED TO WORK WITH SENEAL UI VERSION 1.2.8
- Updated "Hide Wind Direction and Time"
- Updated Vitals (Adrenaline, Eitr, Stamina bars)
- Fixed Vitals 'always on' setting - previously wasn't working.
- Added Vitals position config settings.
- Fixed Guardian Powers (Passive Powers) location and countdown text.
- Fixed EpicLoot legendary set ability button positions.
- Fixed Center Message alignment.
- Fixed Repair Window to auto-expand width to prevent word wrapping.
- Removed CropHarvestHint.cs - Don't need this code anymore, SeneaL UI has built in integration now.
- Removed from config: OldSkillsWindow, UseVanillaSkillsWindow - Not needed anymore (runs automatic/obsolete).
- Removed OldSkillsWindowBind.cs (obsolete).

## 1.1.114

- Improved element finding for SeneaL UI v1.2.1 compatibility
- Enhanced vitals bar synchronization to handle nested UI elements
- Better hotbar positioning for bottom-left alignment
- Improved wind/server day hiding with more path options
- More robust center message gap calculation
- Fixed guardian power hotkeys display

## 1.1.113

- SeneaL UI v1.2.1 compatibility: guardian power hotkeys and timers are visible again. `[Powers] HideSenealSlot` still hides SeneaL's unused guardian-power circle and F key.
- Vitals bar size synchronization: `[Vitals] SyncVitalsSize` (on by default) makes the adrenaline and eitr bars match the stamina bar's size and position. `[Vitals] VitalsWidth` and `VitalsHeight` set the target dimensions (default 300×24 pixels). `[Vitals] VitalsOffset` adds an extra nudge. Fixes misalignment in SeneaL UI v1.2.1.

## 1.1.112

- The hotbar slot countdown no longer crashes when the inventory icon list and the scan list differ in length.

## 1.1.111

- SeneaL's piece health color bar stays left of the cursor.
- `[Hover] ShowPieceHealth` and `ShowPieceHealthPercent` default off. Turned on, they show only the current/max number or only the percent beside that bar. The piece name is not added. `ShowPieceHealthSize` and `ShowPieceHealthPercentSize` set each text size.
- A Creature Level and Loot Control effect name such as Curious is kept to the name's size. With HideNameplateExtras on, that word is removed.

## 1.1.110

- Nameplates, boss stars, and the equip countdown no longer scan the whole screen every frame.

## 1.1.109

- Piece health text is rebuilt only when that piece's health changes. The crop harvest key is read once.

## 1.1.108

- Crop and beehive hover text shows the real Plant Easily bulk-harvest keys. Left Shift was being labeled Q. Shift + the use key is what actually picks the area.

## 1.1.107

- With the hammer out, optional numbers can sit beside SeneaL's piece health bar. See 1.1.111. Both default off.

## 1.1.106

- Rebirth and Prestige already explain a failure and a success, but that text is drawn on the HUD while the skills window covers it. A failed confirm now replaces "This cannot be undone" with the reason, such as the missing coins. Success is written into chat.

## 1.1.104

- SeneaL UI 1.1.9 draws its own star row (`sstars`) inside the health bar instead of on the plate root, so ours no longer replaced it. It is hidden again and our stars come back at `[Creature/Player HUD] StarSize` with the Creature Level and Loot Control effect colour.
- Creatures of level 2 and 3 get our star row too. SeneaL UI now blanks the vanilla `level_2` and `level_3` rows with a zeroed CanvasGroup, and we were reusing those rows for low levels.
- New `[Creature/Player HUD] StarAlign`: Left (default), Center or Right under the bar.
- Ships, carts and portals stay off the compass while SeneaL UI's `[Map] LiveShips`, `LiveCarts` and `LivePortals` keep them on the map and minimap. `[Compass] ShowShips`, `ShowCarts` and `ShowPortals`, all off by default.

## 1.1.103

- SeneaL UI's top-left equip number is hidden. That number is the queue position, not a timer. `[Slots] ShowEquipQueue` defaults off.

## 1.1.102

- `[Slots] RarityFill` defaults off, so SeneaL's rarity border stays.
- The equip countdown defaults off. `[Slots] ShowEquipCue` and `[Slots] ShowGearBarCue`.

## 1.1.101

- SkillsReworked newer than 1.8.2 keeps SeneaL UI's skills panel. 1.8.2 and older open the vanilla skills window, which still has the Skills and Active skills tabs and point spending. The saved `UseVanillaSkillsWindow` value is not the switch.

## 1.1.100

- `UseVanillaSkillsWindow` defaults on again. On keeps the SkillsReworked skills window. The extra Skills and Active skills buttons added on the SeneaL panel are gone.

## 1.1.98

- If SeneaL UI cannot hook SkillsReworked, bind the older points window so SeneaL still draws it. `[Character] OldSkillsWindow`, on by default. Off leaves the game's window.

## 1.1.97

- HideWindAndServerDay also hides SeneaL UI 1.1.9's wind and day card (`info`).
- Boss bar stars can be removed. `[BossBar] HideStars`, on by default.
- `[Creature/Player HUD] Enabled` turns our nameplate overrides off and leaves them to SeneaL UI. On by default.
- Hotbar OffsetY default is 14.5 so the bar clears the longer quick-action labels. An existing config keeps its saved value.

## 1.1.96

- Quests window has a 1 pixel brass edge. The fill stays on `[Quests] Background` / `BackgroundAlpha`.

## 1.1.95

- Quests window uses a solid fill. `[Quests] Background` sets the color. `BackgroundAlpha` sets how see-through it is (1 is solid).

## 1.1.94

- The Quests list scrolls with the mouse wheel and has a scrollbar on the right.

## 1.1.93

- Quests and Talents keep the dark fill. Only a 1 pixel brass edge is gold.
- The quest totals read Bounties: 2/8 and Treasure: 2.

## 1.1.92

- Bounty count uses EpicLoot's Max Bounties Per Player. Treasure maps show the number you have; that config has no cap.
- Quests text is not bold. Quests and Talents share a 1 pixel brass edge.

## 1.1.91

- Quests and Talents are text only, both 88 pixels wide, with the same plate and gold text.
- The Quests list still fills when the bounty and treasure totals cannot be read.

## 1.1.90

- Quests uses the same plate and text color as Talents. Talents has a diamond mark and Quests has a scroll mark, to the left of the word.
- The Quests window shows active bounties and treasure maps against how many you can hold, on the tab row.

## 1.1.89

- Talents moves to the top right of the character window, in the slot Quests used. Quests sits to its left when adventure mode is on.
- Accepting a bounty or treasure map keeps the map closed (`Quests` / `KeepMapClosedOnAccept`, on by default).
- Closing the map after a Quests Map button returns to the same open Quests list.

## 1.1.88

- Quests button shows only while EpicLoot adventure mode (bounties and treasure maps) is on.
- Accepting a bounty or treasure map from Haldor no longer opens the map. Map from the Quests window does, and closing that map returns to the open Quests window.
- Talents sits under the character level. The level XP bar sits where Talents was, beside the level. The Talents button spans the level text plus the XP bar.
- Passive Powers: the shared cooldown timer is written on the power icons, so "power not ready" has a visible timer.

## 1.1.87

- Character panel: Quests button to the left of the settings gear. Opens a Bounties / Treasure tab list of active EpicLoot contracts, with a Map button for each target.

## 1.1.86

- Passive Powers: shared activation cooldown only on the right of the icons (not duplicated under F / Shift+F). Red timers under a key are that power's depletion only.
- Passive Powers: red depletion timer reads the live `PassivePowers Depletion …` effect (120s passive loss after active ends), not only the HUD buff list.

## 1.1.85

- New: `UseVanillaSkillsWindow` (Character, on by default). When SkillsReworked is installed, the Skills button opens the vanilla window so its spend-points controls are visible. SeneaL's skills list stays hidden.
- Passive Powers: the shared "Passive" label is hidden. While the shortcut keys are grey, the shared cooldown is shown under them.
- Passive Powers key, buff, and cooldown labels use the in-game font instead of LiberationSans.

## 1.1.84

- New: `ShowCreatureStars` (Creature/Player HUD, on by default) hides creature level stars, including CLLC-colored rows.

## 1.1.83

- New: `ShowNameplateDiamonds` (Creature/Player HUD, off by default) hides SeneaL UI's gold diamonds and lines beside creature names.

## 1.1.82

- New: Talents button in the character window, 10 px right of the level. Opens TalentTree (its own button sits in the inventory, which SeneaL hides). `ShowTalentsButton`.
- New: 100 px SkillsReworked XP bar 10 px under the character-window level. `ShowLevelXpBar`.
- Fix: after a death, Tenacity (and other SkillManager skills) could be left at a partial level like 29.1, and SkillsReworked would refuse to spend points into it until Rebirth. Fractional levels are now rounded down when the Skills window opens. Only active when both Tenacity and SkillsReworked are installed. `FixSkillRespendAfterDeath`, on by default.
- Package now includes a default `config/cjayride.SeneaLHudLayout.cfg`.

## 1.1.81

- `ThreatIconOpacity` default is now 0.71.
- SeneaL `ModSettingsWindow = ConfigurationManager` works with shudnal ConfigurationManager.
- Nameplate fixes: ValuesBesideLabels restore, threat icons toggle/size/lift/opacity, single CLLC-colored star row, HideNameplateExtras, ShowEnemyLevel.

## 1.1.80

- Fix: `ThreatIconSize` actually resizes (stretch anchors were ignoring sizeDelta; size changes no longer slide the icon).
- Fix: `ThreatIconOpacity` applies to all graphics on the icon.
- `ThreatIconSize` default is now 5.

## 1.1.79

- Threat icon defaults: `ThreatIconSize` 6, `ThreatIconLift` 10. Size slider now goes down to 2.
- New: `ThreatIconOpacity` (default 1). Lower it to fade the Alerted/Aware icons.

## 1.1.78

- Threat icons (Alerted/Aware): smaller by default (`ThreatIconSize` 12, was SeneaL ~26) and lifted above the creature name (`ThreatIconLift` 4).

## 1.1.77

- Fix: only one star row on nameplates (stops gold + red duplicate stacks). CLLC affix coloring still applies to that row.

## 1.1.76

- Restored CLLC affix star colors (Aggressive red, Regenerating green, etc.). Stars stay left-aligned under the bar.

## 1.1.75

- New: `ShowThreatIcons` (default true). Set false to hide Alerted/Aware icons on nameplates.

## 1.1.74

- `ValuesBesideLabels` restores right-aligned numbers when turned off (no rejoin).
- `ShowHealthNumbers` no longer hides the Alerted/Aware threat icons.
- Creature stars stay gold and left-aligned under the bar; CLLC affix color no longer recolors them, and star layout no longer depends on health numbers being on.
- `ShowEnemyLevel` shows a `[Lvl:N]` label when CLLC is not already providing one.
- New: `HideNameplateExtras` hides affix/mutation/empty caption lines above the creature name.

## 1.1.73

- When SeneaL `ModSettingsWindow = ConfigurationManager`, F1 / Esc → Mods now open shudnal ConfigurationManager (SeneaL only recognized the official BepInEx GUID before).

## 1.1.72

- `[Vitals] NumberBold` defaults to 0.8.

## 1.1.71

- Vitals numbers use a much heavier stroke at high `[Vitals] NumberBold`, and the style is reapplied after SeneaL redraws the labels so 1.0 is visible in game.

## 1.1.70

- Stamina, eitr, and adrenaline numbers can be made heavier with `[Vitals] NumberBold` (0 is SeneaL's weight, 1 is extra bold). Default 0.4. Changing the slider restyles three labels; it does not add per-frame work.

## 1.1.69

- Creature health bars and names keep one size when you move closer or farther. A far nameplate no longer stretches the bar or swells the name.

## 1.1.68

- Crafting stat numbers sit beside their labels the first time the window opens, without waiting for the text to finish measuring.

## 1.1.67

- Creature stars use one size, `[Creature/Player HUD] StarSize` (default 10), and stay matched to the health bar when you move closer or farther.

## 1.1.66

- Crafting stat numbers sit beside their labels again.
- Creature stars sit against the health bar and use a softer effect color.

## 1.1.65

- Tooltip and crafting stat numbers sit beside their labels, including on wide EpicLoot item panels.
- Creature stars stay the normal small size and take their Creature Level and Loot Control effect color. The extra glow behind them is gone.

## 1.1.64

- The VNEI button on the crafting panel sits 7 pixels higher so it no longer covers the crafting station stars.
- `[Minimap] HideWindAndServerDay` replaces `HideStatusPills` and defaults to off, so the wind and day/time pills under the minimap stay visible.
- `[Status Effects] MoveToCorner` can place SeneaL's status list in a screen corner, with `Corner`, `OffsetX`, and `OffsetY`. It is off by default. Compact Status Squares still owns that list when it is enabled.
- Looking up the SkillsReworked level no longer scans every loaded mod, so a missing EpicLoot reference in VNEI does not throw.

## 1.1.63

- After opening a crafting material, Back sits at the top right of the item details and returns to the previous recipe.

## 1.1.62

- Creature and player nameplates use `[Creature/Player HUD] NameTextScale` (default 0.75) and `HealthTextScale` (default 0.94). 1 is SeneaL's size.
- `[CenterMessage] SendToNotices` defaults to off. The large middle message stays on the banner.

## 1.1.61

- Crafting-window stat numbers sit just after the stat name. A value such as 13 no longer covers the word Slash.
- Clicking a material that this station can craft opens that recipe, and Back returns to the recipe you came from.

## 1.1.60

- Nameplate stars keep their gold color, and a glow behind them uses the creature's Creature Level and Loot Control effect color. Regenerating is green, Aggressive is red, Armored is blue, Curious is cyan, Quick is magenta, and Splitting is white. Stars with no effect stay plain gold.

## 1.1.59

- Creature and player nameplates have `[Creature/Player HUD] NameScale` and `HealthScale`. 1 keeps the current size.

## 1.1.58

- In the crafting window, a requirement that is crafted at the same station can be clicked to open its recipe. Back returns to the recipe you came from. `[Crafting] ClickMaterials` turns this off.

## 1.1.57

- Stat numbers sit directly beside their names in item tooltips and the item window. `[Stats] ValuesBesideLabels` turns that off and restores the right-aligned numbers.

## 1.1.56

- The hammer build search keeps the last text when you leave to place a piece and open the hammer again.

## 1.1.55

- Deposit All, Deposit Similar, and Stack All never take coins out of the coin purse. Move coins into your inventory first.
- Deposit Similar no longer errors when the chest has no coins.

## 1.1.54

- The repair list stays under the repair button as items are repaired. It no longer jumps above the button when the list gets short.

## 1.1.53

- Creature and player nameplate settings live under `[Creature/Player HUD]`: `BarWidth`, `LevelSize`, `ShowEnemyLevel`, and `ShowHealthNumbers`.
- `BarWidth` defaults to 1. A wider bar keeps a full health value filled to the end of the bar.

## 1.1.52

- Coin purse deposits split only as many stacks as the chest can hold. A full chest no longer dumps stacks of 999 into the inventory.
- `[Wallet] SplitForChest` turns that deposit split on or off. It is on by default.

## 1.1.51

- Rarity fill default strength is 0.10.

## 1.1.50

- With the compass hidden, the boss bar moves up into that space.
- The Raid event card sits below the boss bar instead of on top of it.
- Rarity fill works on inventory slots again. Default strength is 0.20.

## 1.1.49

- Rarity fill clears off empty slots after death, and comes back when the items are in those slots again.

## 1.1.48

- Center message defaults are gap 47, half size, and sent into SeneaL's notices.
- Creature stars stay on one line.

## 1.1.47

- Creature, player, and boss nameplates use the ink of the letters, so the lines sit together without a font-box gap.
- The alert and aware marks above creatures stay hidden.

## 1.1.46

- Boss, creature, and player nameplates sit tighter: the gap is measured from the letters themselves, not the text box.
- The Lv label beside creature and boss names is hidden.

## 1.1.45

- Creature and player nameplates are measured from the health bar itself: name, then health numbers, then the bar, then stars. Levels above 2 get their own star row.
- Boss name and health numbers sit a little further off the boss bar.
- Creature and player bars can be widened with `[WorldHud] EnemyBarWidth` (default 1.5). `[WorldHud] ShowEnemyLevel` adds an Lv label.
- The large middle message can be scaled with `[CenterMessage] Scale`, or sent into SeneaL's notices with `[CenterMessage] SendToNotices`.

## 1.1.44

- Creature and player nameplates stack as name, health numbers, health bar, then stars. Health numbers stay above the bar.
- Food-bar level default X is -39.
- VNEI opens with its top-right corner against the bottom-right corner of the VNEI button.

## 1.1.43

- Food-bar level defaults to X -41 and Y -20.4. Extra digits grow to the right, so the label does not slide.
- Creature stars above two stay in a short row under the health bar, and the health numbers sit on the next line. The old drop used the bar's stretched height, which pushed the numbers and the stars off the nameplate.

## 1.1.42

- SeneaL's chat window stays hidden. `[Chat] HideChat` is on by default, so another chat mod such as Chatter can draw the messages.

## 1.1.40

- The crafting panel has an Items button that opens VNEI's full item search. The button then says Close. Closing the inventory closes the search too. `[Crafting] ShowItemSearch`.

## 1.1.39

- Selected-item glow is off by default. `[Slots] Highlight`.
- Health numbers for creatures with more than 2 stars sit one short row under the stars, instead of being pushed far down over them.
- Character-window level reads `Level 22` and sits a little lower in the frame.
- Food-timer level is its own switch, `[Character] ShowFoodLevel`. It sits directly under the timers, sized to the words `Level 22`.

## 1.1.38

- Creature health numbers drop below the star row when a creature has more than 2 stars, so the extra stars stay visible.

## 1.1.37

- Rarity fill defaults to 0.05 strength.
- SeneaL's rarity edge stays hidden while the fill is on, so the only glowing edge is the selected or equipped item.

## 1.1.36

- The character-window level sits 5 pixels lower, clear of the frame.
- The same level also sits under the food timers, on the row with the health number.
- `[Minimap] HideStatusPills` now hides the wind and day/time pills under the minimap. The biome name on the map stays.

## 1.1.35

- Magic items fill the inside of the slot with their EpicLoot color, and SeneaL's rarity border is hidden. The gold selected and equipped glow stays. `[Slots] RarityFill` is on by default. `[Slots] RarityFillStrength` is how solid that fill is (default 0.5). Turn RarityFill off to restore SeneaL's border.

## 1.1.34

- SkillsReworked level sits in the top left of the character window (Skills, Texts, Trophies, PvP). `[Character] ShowLevel` is on by default.
- The wind and day/time pills under the minimap can be hidden. `[Minimap] HideStatusPills` is on by default. The biome name on the map stays.

## 1.1.33

- Power timers no longer throw every frame when there is no local player.

## 1.1.32

- Ctrl-click and put-all split a wallet coin count above 999 into real stacks of 999 before moving them into a chest. If there are not enough free inventory slots to split the whole amount, the leftover stays in the wallet.

## 1.1.31

- Changing highlight or check color and size updates marks that are already on screen. You do not have to reselect the item.
- Turning Highlight or CornerPip on marks whatever is already selected, including at login. Turning them off clears those marks.

## 1.1.30

- Selected hotbar items and equipped gear get a soft gold glow around the button's own edge. `[Slots] Highlight` is on by default. `[Slots] HighlightColor` defaults to gold (`FFD700`). `[Slots] HighlightSize` is how far it spreads, default 4.
- A green check sits on top of the item border for the selected hotbar item and equipped gear. `[Slots] CornerPip` is on by default. `[Slots] CornerPipSize` defaults to 18. `[Slots] CheckColor` is the check color.
- Turning Highlight or CornerPip off clears marks that are already on screen. Turning them on marks whatever is already selected.
- Equip countdown stays in the inventory window (`[Slots] ShowEquipCue`). `[Slots] ShowGearBarCue` also shows it on the hotbar, only while worn gear is being equipped or unequipped. Weapon and tool swaps do not show it.
- Health numbers sit just below the bar, so the name stays above it.
- SeneaL UI's login server-rules window stays hidden. `[Notices] HideServerRules` is on by default. Server limits still apply.

## 1.1.15

- Equipped and selected weapons, tools, and armor get a brighter blue frame. Config: `[Slots] SelectionGlow` (0 to 3, default 1.4). `0` leaves SeneaL's own glow.
- Equipping or unequipping an item shows a white whole-second countdown on the item icon (`5`, `4`, `3`, `2`, `1`). Config: `[Slots] ShowEquipCue` (on by default).

## 1.1.8

- Creature and player health bars show `current/max` just above the bar. Config: `[WorldHud] ShowHealthNumbers` (on by default).
- Fixed a crash while creating that label. TextMesh Pro threw when an outline was applied to a font with no outline material.
- Passive Powers size slider: `[Powers] Scale`, from 0.5 to 1.5. `1` is the previous size.
- Optional always-visible stamina, eitr, and adrenaline bars. All default off, so SeneaL UI still fades them:
  - `[Vitals] AlwaysShowStamina`
  - `[Vitals] AlwaysShowEitr`
  - `[Vitals] AlwaysShowAdrenaline`
- Hovering a Grillspett Item Drawer shows one slot and the total stored count, instead of every stack.
- Fixed the drawer-preview patch failing to load. SeneaL UI names the container parameter `c`.

## 1.1.3

- Hotbar, action slots, and food quick slots move to the bottom left.
- Health and food sit above that hotbar. Compass stays at the top.
- Boss bars and center messages are nudged. Minimap cluster shifts left.
- SeneaL's unused guardian-power circle can be hidden. Passive Powers sit beside the food column.
