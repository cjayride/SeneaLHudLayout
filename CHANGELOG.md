# Changelog

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
