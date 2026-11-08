# SeneaL HUD Layout

A client-side layout and quality-of-life companion for [SeneaL UI](https://thunderstore.io/c/valheim/p/seneaL/SeneaL_UI/). Repositions HUD elements, improves readability, and adds configurable interface enhancements throughout Valheim.

**Requires SeneaL UI. This mod does not replace it and does nothing without it.**

## Requirements

- BepInEx
- [SeneaL UI](https://thunderstore.io/c/valheim/p/seneaL/SeneaL_UI/)
- Optional: Smoothbrain Passive Powers

**Client-side only. Dedicated servers do not need this mod.**

## Features

### HUD Layout

- Moves the hotbar, action slots, and food quick slots to the bottom-left corner.
- Positions health, food, and guardian power elements above the hotbar.
- Keeps the compass pinned to the top of the screen.
- Marks your death/grave pin for the SeneaL compass automatically (`[Compass] DeathPinOnCompass`).
- Adjusts boss bars, center messages, and event banners to prevent overlapping.
- Shifts the minimap cluster to the left.
- Positions Passive Powers icons beside the food column.
- Optionally hides SeneaL UI's unused guardian power slot and F-key hint.
### Vitals

- Optional always-visible stamina, Eitr, and adrenaline bars.
- Synchronizes Eitr and adrenaline bar sizes and positions with stamina.
- Configurable bar dimensions and positional offsets.
- Adjustable number font weight.

### Creature & Player HUD

- Displays current and maximum health beneath creature and player health bars.
- Configurable name, health number, and bar sizes.
- Keeps level stars consistent in size at different viewing distances.
- Colors stars according to Creature Level and Loot Control effects:
  - **Green:** Regenerating
  - **Red:** Aggressive
  - **Blue:** Armored
  - **Cyan:** Curious
  - **Magenta:** Quick
  - **White:** Splitting
  - **Gold:** No effect

### Hotbar & Equipment

- Adds a soft gold glow to selected hotbar items and equipped gear.
- Adds a checkmark to selected and equipped items.
- Optionally fills magic item slots with their EpicLoot rarity color.
- Optional equip and gear countdown timers.
- Configurable highlight colors, sizes, and effects.

### Character & Skills

- **Character Stats** tab on the SeneaL character panel: live health, stamina, eitr, armor, resists, food, status effects, weapon skills, and more (`[Stats] StatsTab`, optional hotkey and Steam portrait).
- Displays the SkillsReworked level in the character window (`[Character Window]`).
- Optionally displays that level under the food timers (`[HealthFood] ShowLevelFoodBar` and offset keys).
- On a creature kill, shows how much SkillsReworked Level XP you gained next to the damage-share percent (`[Notices] ShowKillXp`).

### Crafting & Building

- Adds an Items button to the crafting panel to open VNEI's item search.
- Allows crafting materials made at the current station to be clicked to open their recipes.
- Adds a Back option to return to the previous recipe.
- Preserves hammer piece-search text when closing the search to place a building piece and reopening it.
- Positions crafting stat values beside their labels.

### Minimap & Status Effects

- Optionally hides wind direction and day/time indicators beneath the minimap while keeping the biome name visible.
- Can move SeneaL UI's status effect list to a screen corner with configurable offsets.
- Compact Status Squares retains control of status effects when enabled.

### Inventory & Item Information

- Collapses the Grillspett Item Drawer hover preview into one slot displaying the total stored quantity.
- Prevents chest deposit operations from taking coins directly out of the coin purse.
- Positions stat values beside their labels in item tooltips and crafting panels.
- Balances two-column item tooltips by moving whole sections between columns for similar height (`[ItemTooltips] LeftOverflow`).
- Shows a small item sprite on the tooltip type line (`[ItemTooltips] ShowItemIcon`), for example a hoe next to **Tool**.
- With Epic Loot loaded, opens shard (and rune) sockets in a dedicated window so they can be filled or emptied while SeneaL CustomUI is on. Hover the gear and press E. Turn `[EpicLoot] ShardSocketWindow` off to leave this to Epic Loot alone.

### Interface & Notifications

- Old config entries left by renamed sections and removed settings are cleaned out of the config file on load, so the file keeps only settings this version uses.
- Hides SeneaL UI's login server-rules window. Server restrictions still apply.
- Hides SeneaL UI's chat window so compatible chat mods can display messages.
- Adjusts center messages to avoid overlapping the compass and boss health bar.
- Improves the repair window layout to prevent unwanted text wrapping.
- Supports additional EpicLoot interface adjustments, including legendary set ability buttons.

### Chat

- `ChatSettingsEnabled` (off by default) is the master switch for every Chat setting except `HideChat`, `HideShoutOverHead`, `ChatAutoScrollOnNewMessage`, `ChatWindowRecommendedLayout`, and the chat window size/position keys. Turn it on to use shout prefix, last-used channel, shout capitalization, hide delay, and chat font.
- `HideShoutOverHead` (on by default) hides the yellow shout/yell text above player heads. The shout still goes to the chat log.
- `ChatAutoScrollOnNewMessage` (on by default) jumps the log to the latest lines when the chat box is closed and a new message arrives. While the box is open, scroll position is kept.
- `ChatWindowRecommendedLayout` (off by default): one switch for a built-in recommended chat layout (PosX 84, PosY -88, Width -88, Height -9). While on, the four keys below are ignored; your manual values stay in the cfg for when you turn it off.
- `ChatWindowPosX` / `ChatWindowPosY` / `ChatWindowWidth` / `ChatWindowHeight`: **0 is SeneaL's original chat.** All four are pixel integers (1-step in F1, for example PosY -76). Pos moves the background, log, and input together. Negative width/height shrinks. Resetting all four to 0 rebuilds the original window. Client-only.
- `DefaultToShout` (off by default) opens the chat box with `/s ` already typed in it, so your first Enter shouts — the long-range message that shows above your character — and you never have to type the prefix.
- Delete the `/s ` and the message is a normal close-range say, exactly Valheim's own `/s` and plain text. The box stops prefilled and stays that way until you type `/s` again.
- `RememberLastUsed` keeps that channel between messages and across restarts, in the game's own player prefs file.
- `ShoutKeepsCapitalization` (off by default) reads exactly as named: off does NOT keep Valheim's shout capitalization, so a shout shows the letters the way you typed them ("hello there" stays "hello there"). On keeps Valheim's ALL CAPS shout. Whispers stay lower case, as Valheim makes them.
- `HideChatPanelDelay` (10) is how many seconds the chat panel stays visible after the last chat activity. It is the game's own chat hide timer; Valheim ships 10.
- With Chatter installed, the grey placeholder in its chat box follows the channel, showing `/shout` or `/say`.
- `chatMessageTextFontAsset` and `chatMessageTextFontSize` set the chat log's font and size. Any TMP font asset the game has loaded is accepted by name — Valheim's own `Valheim-AveriaSansLibre` (default), `Valheim-AveriaSerifLibre`, `Valheim-Norse`, `Valheim-Norsebold`, `Valheim-Prstartk`, the `NotoSans`/`NotoSerif` language sets, `LiberationSans SDF`, and fonts other mods add. Size is 6–64 (default 16). `chatMessageFontEnabled` turns the pair off. Chatter is not touched; its own font keys keep driving Chatter's panel. If the name matches nothing loaded, the log lists the font assets the game actually has.

## Configuration

Configuration file:

`BepInEx/config/cjayride.SeneaLHudLayout.cfg`

Settings are organized by category. Key options include:

| Category | Setting | Default |
| --- | --- | --- |
| General | `Enabled` | On |
| Vitals | `SyncVitalsSize` | On |
| Vitals | `AlwaysShowStamina` | Off |
| Vitals | `AlwaysShowEitr` | Off |
| Vitals | `AlwaysShowAdrenaline` | Off |
| Vitals | `NumberBold` | 0.8 |
| Creature/Player HUD | `ShowHealthNumbers` | On |
| Creature/Player HUD | `NameTextScale` | 0.75 |
| Creature/Player HUD | `HealthTextScale` | 0.94 |
| Creature/Player HUD | `StarSize` | 10 |
| Powers | `Scale` | 1.0 |
| Slots | `Highlight` | On |
| Slots | `CornerPip` | On |
| Slots | `RarityFill` | Off |
| Crafting | `ShowItemSearch` | On |
| Crafting | `ClickMaterials` | On |
| Minimap | `HideWindAndServerDay` | Off |
| Status Effects | `MoveToCorner` | Off |
| Chat | `HideChat` | Off |
| Chat | `HideShoutOverHead` | On |
| Chat | `ChatAutoScrollOnNewMessage` | On |
| Chat | `ChatSettingsEnabled` | Off |
| Chat | `DefaultToShout` | Off |
| Chat | `RememberLastUsed` | Off |
| Chat | `ShoutKeepsCapitalization` | Off |
| Chat | `HideChatPanelDelay` | 10 |
| Chat | `chatMessageFontEnabled` | Off |
| Chat | `chatMessageTextFontAsset` | Valheim-AveriaSansLibre |
| Chat | `chatMessageTextFontSize` | 16 |
| Chat | `ChatWindowRecommendedLayout` | Off |
| Chat | `ChatWindowPosX` | 0 |
| Chat | `ChatWindowPosY` | 0 |
| Chat | `ChatWindowWidth` | 0 |
| Chat | `ChatWindowHeight` | 0 |
| Wallet | `SplitForChest` | On |
| EpicLoot | `ShardSocketWindow` | On |

Additional settings control positioning, sizing, colors, visibility, and other behaviors.

To restore SeneaL UI's original placement behavior, set `[General] Enabled` to `false`.

## Installation

1. Install BepInEx and SeneaL UI.
2. Place `SeneaLHudLayout.dll` in `BepInEx/plugins`.
3. Launch Valheim once to generate the configuration file.
4. Customize settings in `BepInEx/config/cjayride.SeneaLHudLayout.cfg`.

The Thunderstore zip does **not** ship a config file; BepInEx creates it when the game runs.

Install on each client that needs the layout changes.

## Credits

- **SeneaL UI:** Created by [seneaL](https://thunderstore.io/c/valheim/p/seneaL/SeneaL_UI/).
- **SeneaL HUD Layout:** Created by cjayride.