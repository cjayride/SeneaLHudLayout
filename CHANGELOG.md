# Changelog

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
