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
- Optional hull health bar under the sailing wind disc while you are at the helm (`[Ship] ShowHealth`, off by default; `[Ship] ShowNumbers` prints current/max).
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
- PotionsPlus potions and other consumables show their consume-cooldown countdown (`[Items] PotionCooldowns`, on by default) on the quick-action row AND the main hotbar row (food, meads, anything with a consume cooldown): the slot is tinted (`PotionCooldownDimOpacity`, 0.55) and the seconds are drawn at `PotionCooldownTextOpacity` (0.65 default), `PotionCooldownTextOffsetY` (5) pixels below the slot's centre, so the potion icon stays visible. The tint square is centred on the drawn button and inset by `[Items] PotionCooldownInset` (10 default) so it stops at the button's edge, not past it. With Cjayride Skyheim Compat installed, set its `[Cooldown overlay] OnSenealHotbar = false` (1.2.7 default) so Skyheim's own full-slot square + centred number don't stack on top of these.
- Shows your level and full XP bar flush on the action slots' right edge (a small gap clear of the last slot), centred on them (`[Hotbar] ShowXpBar`, off by default): `Level 42 · bar · 37%` on the top row and the `1234 / 3333` XP numbers under the bar with 6px of plate BELOW them, full card width, left-aligned from the bar's start like the character panel. All the text is gold; `[Hotbar] XpBarWhiteNumbers` (off by default, live) makes just the NUMBERS white and leaves the words/symbols (`Level `, ` / `, `%`) gold. The card is black at `[Hotbar] XpBarOpacity` (0.75 default, live), outlined by a hairline gold border - 0.5px at 15% opacity, drawn as FOUR solid line quads (top/bottom/left/right strips), never a sprite that can fill the card (`[Hotbar] XpBarBorder`, `[Hotbar] XpBarBorderWidth`, `[Hotbar] XpBarBorderOpacity`, all live). The bar's track is a black groove (track opacity `[Hotbar] XpBarTrackOpacity`, 0.75 default) and the FILLED part shows as a gold progress bar. In the same font as the food-timer text, sized by `[Hotbar] XpBarScale` (1.0 default). The card keeps itself on top of hotbar overlays and blacks out any foreign graphic stretched across it. Turning it on never moves the action slots or any other HUD element.
- Three XP bar positions (`[Hotbar] XpBarAnchor`, live): `RightOfSlots` is the shipped look above - the gap is its own knob, `[Hotbar] XpBarPaddingX` (12 default): raise it to push the card farther RIGHT, clear of the QUIVER frame (arrows), which sits exactly where the card hangs when a bow is equipped. `TopLeft` pins the card to the screen's top-left corner (insets `[Hotbar] XpBarPaddingX` / `XpBarPaddingY`, 12 default). `UnderFoodBar` hangs a CONDENSED one-row card under the food timers: `Lv 42 · bar · 37% · 1234/3333` on a single plate, 0.75x smaller, the bar narrowed to `[Hotbar] XpBarCompactBarWidth` (70 default) - the CJAYCRAFT pack ships this anchor with `XpBarPaddingX = -100`, `XpBarPaddingY = 26`. `[Hotbar] XpBarCompact` (off default) turns the same one-row condense on for ANY anchor (1.5.61: it now drives `UnderFoodBar` too, where it used to be forced, and the card's pad is `[Hotbar] XpBarCompactBottomPad`, 4 default, below the text row plus a fixed 3px off its top). `[Hotbar] XpBarPaddingY` moves the card vertically on every anchor, `RightOfSlots` included - 12 stays the shipped centred look, up above it, down below.
- **Move the quick slots out of the card's way** (`[Hotbar] SlotsOffsetX` / `[Hotbar] SlotsOffsetY`, 0 default, 1.5.61): slides ONLY the action-slot group and the QUIVER frame right/up, leaving the item bar and the food quick slots alone - so the XP card can sit in the spot the slots used to occupy (park it there with a negative `[Hotbar] XpBarPaddingX`; the card anchors to the slots, so it rides the nudge and measures from their new edge).
- **One XP display at a time** (`[Hotbar] XpBarOneAtATime`, on by default, 1.5.60): the action-slot card and the character-window bar (`[Character Window] ShowLevelXpBar`) no longer stack - only the highest-priority enabled one draws, and the log says which. Turn it off to show both at once. The level plate under the food timers (`[HealthFood] ShowLevelFoodBar`) is NOT part of this rule (1.5.62): it shows whenever its own toggle is on.
- Configurable highlight colors, sizes, and effects.

### Character & Skills

- **Character Stats** tab on the SeneaL character panel: live health, stamina, eitr, armor, resists, food, status effects, weapon skills, and more (`[Stats] StatsTab`, optional hotkey and Steam portrait).
- Displays the SkillsReworked level in the character window (`[Character Window]`).
- Optionally displays that level under the food timers (`[HealthFood] ShowLevelFoodBar` and offset keys). Off by default. This plate is INDEPENDENT of `[Hotbar] XpBarOneAtATime` (1.5.62) - turn it on and it shows, alongside the action-slot card.
- On a creature kill, shows how much SkillsReworked Level XP you gained next to the damage-share percent (`[Notices] ShowKillXp`).
- **Guardian powers** rows on the Stats sheet (`[Stats] GuardianPowers`, on by default): one row per boss power you have equipped in Passive Powers, each with its live passive state — `Active 1m 59s` during the activation buff, `Depleted 1m 45s` during the lockout, `Passive` at rest — and the power's passive effect lines from the Passive Powers config underneath. Needs Passive Powers; vanilla's single power stays on the Forsaken power row.
- The level XP bar (`[Character Window] ShowLevelXpBar`) shows the same XP as those kill notices, so a "+12 XP" kill moves the numbers under the bar by the 12 XP it is worth.

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
- **EXPERIMENTAL: one slot row in the inventory** (`[Inventory] MergeSlotRows`, off by default, 1.5.62): the FOOD quick slots, the ACTION slots and the QUIVER slot share one horizontal row of the SeneaL inventory panel - FOOD | ACTION | QUIVER | BACKPACK left to right - instead of the action row stacked under the food row, so the panel is two grid rows shorter. Needs `[Inventory] QuickSlotCount` + `ActionSlotCount` + 2 (quiver, backpack) cells inside the 8-cell row width (the pack's 3 + 3 fits exactly); when the counts do not fit the inventory keeps SeneaL's own stacked rows. The merge rides SeneaL's own row math, so drags, sorts, stacks, right-clicks, number/Alt keys and the accept rules all follow the new band. Off restores the shipped layout. Client-only; tell us what breaks - item safety first, panel height next.

### Interface & Notifications

- Old config entries left by renamed sections and removed settings are cleaned out of the config file on load, so the file keeps only settings this version uses.
- Hides SeneaL UI's login server-rules window. Server restrictions still apply.
- Hides SeneaL UI's chat window so compatible chat mods can display messages.
- Adjusts center messages to avoid overlapping the compass and boss health bar.
- Improves the repair window layout to prevent unwanted text wrapping.
- Supports additional EpicLoot interface adjustments, including legendary set ability buttons.

### Chat

- `ChatSettingsEnabled` (off by default) is the master switch for every Chat setting except `HideChat`, `HideShoutOverHead`, `ChatAutoScrollOnNewMessage`, `ChatWindowRecommendedLayout`, `ChatAlwaysVisible`, `ChatBackgroundOpacity`, `ShoutWhiteText` / `ShoutTextColor`, `SayLightGreyText` / `SayTextColor`, `AllChatShout`, and the chat window size/position keys. Turn it on to use shout prefix, last-used channel, shout capitalization, hide delay, and chat font.
- `HideShoutOverHead` (on by default) hides the yellow shout/yell text above player heads. The shout still goes to the chat log.
- `ChatAutoScrollOnNewMessage` (on by default) jumps the log to the latest lines when the chat box is closed and a new message arrives. While the box is open, scroll position is kept.
- `ChatWindowRecommendedLayout` (off by default): one switch for a built-in recommended chat layout (PosX 84, PosY -88, Width -88, Height -9). While on, the four keys below are ignored; your manual values stay in the cfg for when you turn it off.
- `ChatWindowPosX` / `ChatWindowPosY` / `ChatWindowWidth` / `ChatWindowHeight`: **0 is SeneaL's original chat.** All four are pixel integers (1-step in F1, for example PosY -76). Pos moves the background, log, and input together. Negative width/height shrinks. Resetting all four to 0 rebuilds the original window. Client-only.
- The chat window draws **behind** every window - inventory, the SeneaL crafting window, all of them: with a window open, an incoming message never paints over your crafting panel. Automatic, no setting; the chat window keeps its normal slot in the HUD. Client-only.
- `AllChatShout` (on by default) makes every chat message a shout, with no prefixes to type at all: the box opens empty (nothing is prefilled and `RememberLastUsed` is not consulted) and plain text you type is sent as `/s`. A typed `/say` (or `/w`) sends that one message close range, and console commands are untouched.
- `DefaultToShout` (off by default, ignored while `AllChatShout` is on) opens the chat box with `/s ` already typed in it, so your first Enter shouts — the long-range message that shows above your character — and you never have to type the prefix.
- Delete the `/s ` and the message is a normal close-range say, exactly Valheim's own `/s` and plain text. The box stops prefilled and stays that way until you type `/s` again.
- `RememberLastUsed` keeps that channel between messages and across restarts, in the game's own player prefs file. Not consulted while `AllChatShout` is on.
- `ShoutKeepsCapitalization` (off by default) reads exactly as named: off does NOT keep Valheim's shout capitalization, so a shout shows the letters the way you typed them ("hello there" stays "hello there"). On keeps Valheim's ALL CAPS shout. Whispers stay lower case, as Valheim makes them.
- `HideChatPanelDelay` (12) is how many seconds the chat panel stays visible after the last chat activity. It is the game's own chat hide timer; Valheim ships 10.
- `ChatAlwaysVisible` (off by default) leaves the chat window always on: it never fades out after the last activity. `HideChat` wins when both are on.
- `ChatBackgroundOpacity` (0.25) sets the opacity of the chat window's dark background — the panel behind the messages and input. 1 is SeneaL UI's solid glass, 0.25 is a quarter as opaque, 0 hides the fill. The chat text is never faded.
- `ShoutWhiteText` (on by default) prints shouts in the shout colour (Valheim ships yellow). `SayLightGreyText` (on by default) prints say / proximity chat in the say colour, so shouts stand out (Valheim ships white).
- `ShoutTextColor` (FFFFFF) and `SayTextColor` (C7C7C7) are hex RGB colours — with or without `#`, 3/6/8 digits — for shouts and say lines. An empty value restores vanilla yellow / white.
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
| Chat | `AllChatShout` | On |
| Chat | `DefaultToShout` | Off |
| Chat | `RememberLastUsed` | Off |
| Chat | `ShoutKeepsCapitalization` | Off |
| Chat | `HideChatPanelDelay` | 12 |
| Chat | `ChatAlwaysVisible` | Off |
| Chat | `ChatBackgroundOpacity` | 0.25 |
| Chat | `ShoutWhiteText` | On |
| Chat | `ShoutTextColor` | FFFFFF |
| Chat | `SayLightGreyText` | On |
| Chat | `SayTextColor` | C7C7C7 |
| Items | `PotionCooldowns` | On |
| Items | `PotionCooldownTextOpacity` | 0.65 |
| Items | `PotionCooldownDimOpacity` | 0.55 |
| Items | `PotionCooldownInset` | 10 |
| Items | `PotionCooldownTextOffsetY` | 5 |
| Hotbar | `ShowXpBar` | Off |
| Hotbar | `XpBarScale` | 1 |
| Hotbar | `XpBarTrackOpacity` | 0.75 |
| Hotbar | `XpBarOpacity` | 0.75 |
| Hotbar | `XpBarBorder` | true |
| Hotbar | `XpBarBorderWidth` | 0.5 |
| Hotbar | `XpBarBorderOpacity` | 0.15 |
| Hotbar | `XpBarWhiteNumbers` | false |
| Hotbar | `XpBarAnchor` | RightOfSlots |
| Hotbar | `XpBarPaddingX` | 12 |
| Hotbar | `XpBarPaddingY` | 12 |
| Hotbar | `XpBarCompact` | false |
| Hotbar | `XpBarCompactBarWidth` | 70 |
| Hotbar | `XpBarCompactBottomPad` | 4 |
| Hotbar | `XpBarOneAtATime` | true |
| Hotbar | `SlotsOffsetX` | 0 |
| Hotbar | `SlotsOffsetY` | 0 |
| HealthFood | `ShowLevelFoodBar` | Off |
| Inventory | `MergeSlotRows` | Off (experimental) |
| Character Window | `ShowLevelXpBar` | Off (pack ships On) |
| Stats | `GuardianPowers` | On |
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