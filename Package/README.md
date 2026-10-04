# SeneaL HUD Layout

Client layout companion for **SeneaL UI**. Moves the hotbar cluster to the bottom left, lifts health/food above it, keeps the compass at the top, and places Passive Powers next to the food column.

Requires **SeneaL UI**. Clients only.

## 1.1.85

- With SkillsReworked installed, the Skills button opens the vanilla window. `UseVanillaSkillsWindow`.
- The Passive Powers "Passive" label is hidden. Shared activation cooldown sits to the right of the icons; grey F / Shift+F keys while it runs. Green above an icon = passive buff time left; red under a key = that power's depletion.

## 1.1.84

- Creature level stars can be hidden. `ShowCreatureStars` (on by default).

## 1.1.83

- Gold diamonds and lines beside creature names are hidden by default. `ShowNameplateDiamonds` to bring them back.

## 1.1.82

- Talents button beside the character-window level (opens TalentTree). `ShowTalentsButton`.
- 100 px SkillsReworked XP bar under that level. `ShowLevelXpBar`.
- Fix: skills like Tenacity can take points again after a death leaves them at a partial level. Needs Tenacity and SkillsReworked. `FixSkillRespendAfterDeath`.

## 1.1.81

- Threat icons: size/lift/opacity (`ThreatIconSize` 5, `ThreatIconLift` 10, `ThreatIconOpacity` 0.71). `ShowThreatIcons` to hide them.
- Shudnal ConfigurationManager works with SeneaL `ModSettingsWindow = ConfigurationManager`.
- Nameplate: ValuesBesideLabels restores when off; single CLLC-colored star row; `HideNameplateExtras`; `ShowEnemyLevel`.

## 1.1.73

- With SeneaL `ModSettingsWindow = ConfigurationManager`, F1 opens **shudnal ConfigurationManager** (SeneaL alone only detects the official BepInEx manager GUID).

## 1.1.31

- Highlight and check color or size changes apply to items that are already selected.
- Turning Highlight or CornerPip on or off updates marks already on screen.

## 1.1.30

- Selected and equipped items get a soft gold glow. `[Slots] Highlight` defaults on. Color is `[Slots] HighlightColor`, size is `[Slots] HighlightSize`.
- A green check marks the selected hotbar item and equipped gear. `[Slots] CornerPip` defaults on. Size is `[Slots] CornerPipSize`.
- Inventory equip countdown: `[Slots] ShowEquipCue`. Hotbar countdown for worn gear only: `[Slots] ShowGearBarCue`.
- Health numbers sit just below the bar. The login server-rules window stays hidden.

## 1.1.15

- Equipped items get a brighter blue frame. `[Slots] SelectionGlow` is 0 to 3, default 1.4.
- Equipping or unequipping shows a white seconds countdown on the item. `[Slots] ShowEquipCue` defaults on.

## 1.1.8

- `[Powers] Scale` sizes the Passive Powers icons (0.5 to 1.5, default 1).
- Creature and player bars show `current/max` just above the bar. `[WorldHud] ShowHealthNumbers` defaults on.
- `[Vitals] AlwaysShowStamina`, `AlwaysShowEitr`, and `AlwaysShowAdrenaline` default off, so SeneaL UI still fades those bars.
- Item Drawer hover shows one slot and the total count.

Config: `BepInEx/config/cjayride.SeneaLHudLayout.cfg`  
`[General] Enabled = false` restores SeneaL's own placement.

SeneaL UI by **seneaL**. Layout overlay by **cjayride**.
