# SeneaL HUD Layout

Client layout companion for **SeneaL UI**. Moves SeneaL's hotbar cluster to the bottom left, lifts health/food above it, keeps the compass pinned to the top of the screen, and places Passive Powers next to the food column.

This mod does **not** replace SeneaL UI. Without SeneaL UI, it does nothing.

## Requires

- BepInEx
- [SeneaL UI](https://thunderstore.io/c/valheim/p/seneaL/SeneaL_UI/)
- Optional: Smoothbrain Passive Powers (for the two power icons)

Install on **clients only**. Dedicated servers do not need it.

## What it changes

- Hotbar, action slots, and food quick slots → bottom left
- Health / food / guardian cluster raised above that hotbar
- Compass stays at the top (hotbar never drags it down)
- Boss bars nudged down a bit; slain/biome banners sit under the compass or boss bar
- Minimap cluster nudged left
- SeneaL's unused guardian-power circle and F hint can be hidden; Passive Powers sit beside the food column
- `[Powers] Scale` shrinks or grows those Passive Powers icons (0.5 to 1.5, default 1)
- Item Drawer hover preview collapses to one slot (total count) instead of every stack
- Creature and player world health bars show `current/max` just below the bar (`[WorldHud] ShowHealthNumbers`, on by default)
- Optional always-visible stamina, eitr, and adrenaline bars (`[Vitals]`, all off by default)
- Selected and equipped items get a soft gold glow (`[Slots] Highlight`, on by default)
- A green check marks the selected hotbar item and equipped gear (`[Slots] CornerPip`, on by default)
- Magic items fill the slot interior with their EpicLoot color, and SeneaL's rarity border is hidden (`[Slots] RarityFill`, on by default). `[Slots] RarityFillStrength` defaults to 0.10
- Equipping or unequipping shows a white seconds countdown in the inventory window (`[Slots] ShowEquipCue`). The hotbar only counts down worn gear (`[Slots] ShowGearBarCue`)
- The login server-rules window is hidden (`[Notices] HideServerRules`, on by default). Server limits still apply.
- SkillsReworked level appears in the top left of the character window (`[Character] ShowLevel`, on by default)
- Wind and day/time pills under the minimap are hidden (`[Minimap] HideStatusPills`, on by default). The biome name stays
- SkillsReworked level also sits under the food timers, beside the health number (`[Character] ShowLevel`)
- The crafting panel has an Items button that opens VNEI's item search. The same button says Close while it is open (`[Crafting] ShowItemSearch`, on by default)
- SeneaL's chat window is hidden (`[Chat] HideChat`, on by default) so another chat mod can draw the messages
- Depositing a coin purse larger than 999 splits off only the stacks the chest can hold (`[Wallet] SplitForChest`, on by default). The rest stays in the purse

Turn `[General] Enabled` off to restore SeneaL's own placement.

## Install

1. Install SeneaL UI.
2. Drop `SeneaLHudLayout.dll` into `BepInEx/plugins`.
3. Launch once. Config: `BepInEx/config/cjayride.SeneaLHudLayout.cfg`.

## Credits

SeneaL UI is by **seneaL**. This is a layout overlay by **cjayride**.
