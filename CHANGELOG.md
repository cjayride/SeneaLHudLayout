# Changelog

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
