using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Optionally treat the wishbone as a dungeon key so it lives in a SeneaL key slot
    /// instead of the charm cell. Clicking it there picks it up; it does not swap with the belt.
    /// </summary>
    static class WishboneKeySlot
    {
        const string Token = "$item_wishbone";
        const int CharmSlot = 7;
        const int ExtraCharmSlot = 14;

        static Type _slots;
        static Type _pouch;
        static MethodInfo _isKeyCell;
        static MethodInfo _freeKey;
        static MethodInfo _changed;
        static MethodInfo _setupDrag;
        static FieldInfo _manual;
        static FieldInfo _dragGo;
        static bool _looked;
        static bool _placing;

        public static void Apply(Harmony harmony)
        {
            _slots = AccessTools.TypeByName("SeneaLUI.Inv.Slots");
            _pouch = AccessTools.TypeByName("SeneaLUI.Inv.Pouch");
            if (_slots == null)
            {
                return;
            }

            MethodInfo isCharm = AccessTools.Method(_slots, "IsCharm", new[] { typeof(ItemDrop.ItemData) });
            MethodInfo isKey = AccessTools.Method(_slots, "IsKey", new[] { typeof(ItemDrop.ItemData) });
            if (isCharm != null)
            {
                harmony.Patch(isCharm, postfix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(AfterIsCharm)));
            }

            if (isKey != null)
            {
                harmony.Patch(isKey, postfix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(AfterIsKey)));
            }

            MethodInfo slotRaw = AccessTools.Method(_slots, "SlotRaw", new[] { typeof(ItemDrop.ItemData) });
            if (slotRaw != null)
            {
                harmony.Patch(slotRaw, postfix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(AfterSlotRaw)));
            }

            MethodInfo accepts = AccessTools.Method(_slots, "Accepts", new[] { typeof(int), typeof(ItemDrop.ItemData) });
            if (accepts != null)
            {
                harmony.Patch(accepts, postfix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(AfterAccepts)));
            }

            MethodInfo equip = AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem));
            if (equip != null)
            {
                harmony.Patch(equip, prefix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(BeforeEquip)));
                harmony.Patch(equip, postfix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(AfterEquip)));
            }

            MethodInfo selected = AccessTools.Method(typeof(InventoryGui), "OnSelectedItem");
            if (selected != null)
            {
                harmony.Patch(selected, prefix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(BeforeSelected)));
            }

            MethodInfo right = AccessTools.Method(typeof(InventoryGui), "OnRightClickItem");
            if (right != null)
            {
                harmony.Patch(right, prefix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(BeforeRightClick)));
            }

            MethodInfo use = AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UseItem));
            if (use != null)
            {
                harmony.Patch(use, prefix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(BeforeUse)));
            }

            MethodInfo toggle = AccessTools.DeclaredMethod(typeof(Player), "ToggleEquipped");
            if (toggle != null)
            {
                harmony.Patch(toggle, prefix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(BeforeToggle)));
            }

            MethodInfo queueOn = AccessTools.DeclaredMethod(typeof(Player), "QueueEquipAction");
            if (queueOn != null)
            {
                harmony.Patch(queueOn, prefix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(BeforeQueue)));
            }

            MethodInfo queueOff = AccessTools.DeclaredMethod(typeof(Player), "QueueUnequipAction");
            if (queueOff != null)
            {
                harmony.Patch(queueOff, prefix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(BeforeQueue)));
            }

            if (SeneaLHudLayoutPlugin.WishboneInKeySlot != null)
            {
                SeneaLHudLayoutPlugin.WishboneInKeySlot.SettingChanged += (_, __) => Place(Player.m_localPlayer);
            }
        }

        static bool BeforeSelected(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            if (!On() || __instance == null || item == null || !IsWishbone(item))
            {
                return true;
            }

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return true;
            }

            Inventory inv = player.GetInventory();
            if (inv == null || grid == null || grid.GetInventory() != inv)
            {
                return true;
            }

            if (!InKey(inv, item))
            {
                return true;
            }

            if (Dragging(__instance))
            {
                return true;
            }

            Pickup(__instance, item, inv);
            return false;
        }

        static bool BeforeRightClick(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
        {
            if (!On() || __instance == null || item == null || !IsWishbone(item))
            {
                return true;
            }

            Player player = Player.m_localPlayer;
            Inventory inv = player == null ? null : player.GetInventory();
            if (inv == null || grid == null || grid.GetInventory() != inv)
            {
                return true;
            }

            return false;
        }

        static bool BeforeUse(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (!On() || !IsWishbone(item) || !(__instance is Player player))
            {
                return true;
            }

            player.RemoveEquipAction(item);
            return false;
        }

        static bool BeforeToggle(Player __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!On() || !IsWishbone(item) || __instance == null)
            {
                return true;
            }

            __instance.RemoveEquipAction(item);
            __result = false;
            return false;
        }

        static bool BeforeQueue(Player __instance, ItemDrop.ItemData item)
        {
            return !On() || !IsWishbone(item);
        }

        static bool BeforeEquip(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!On() || !IsWishbone(item) || !(__instance is Player))
            {
                return true;
            }

            __result = false;
            return false;
        }

        static void AfterEquip(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (__instance is Player player && IsWishbone(item))
            {
                Place(player);
            }
        }

        static void AfterIsCharm(ItemDrop.ItemData item, ref bool __result)
        {
            if (__result && On() && IsWishbone(item))
            {
                __result = false;
            }
        }

        static void AfterIsKey(ItemDrop.ItemData item, ref bool __result)
        {
            if (!__result && On() && IsWishbone(item))
            {
                __result = true;
            }
        }

        static void AfterSlotRaw(ItemDrop.ItemData item, ref int __result)
        {
            if (On() && IsWishbone(item))
            {
                __result = -1;
            }
        }

        static void AfterAccepts(int slot, ItemDrop.ItemData item, ref bool __result)
        {
            if (__result || !On() || !IsWishbone(item))
            {
                return;
            }

            if (slot == CharmSlot || slot == ExtraCharmSlot)
            {
                __result = true;
            }
        }

        static bool On()
        {
            return SeneaLHudLayoutPlugin.WishboneInKeySlot != null
                && SeneaLHudLayoutPlugin.WishboneInKeySlot.Value;
        }

        static bool IsWishbone(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return false;
            }

            string name = item.m_shared.m_name;
            if (name == Token)
            {
                return true;
            }

            return !string.IsNullOrEmpty(name)
                && name.IndexOf("wishbone", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool InKey(Inventory inv, ItemDrop.ItemData item)
        {
            if (inv == null || item == null)
            {
                return false;
            }

            Lookup();
            if (_isKeyCell == null)
            {
                return false;
            }

            return (bool)_isKeyCell.Invoke(null, new object[] { inv, item.m_gridPos.x, item.m_gridPos.y });
        }

        static void Pickup(InventoryGui gui, ItemDrop.ItemData item, Inventory inv)
        {
            Lookup();
            if (gui == null || item == null || _setupDrag == null)
            {
                return;
            }

            _setupDrag.Invoke(gui, new object[] { item, inv, item.m_stack });
        }

        static bool Dragging(InventoryGui gui)
        {
            if (_dragGo == null || gui == null)
            {
                return false;
            }

            object go = _dragGo.GetValue(gui);
            return go is UnityEngine.Object obj && obj;
        }

        static bool ManualMove()
        {
            return _manual != null && _manual.GetValue(null) is bool flag && flag;
        }

        static void Place(Player player)
        {
            if (_placing || !On() || player == null || ManualMove())
            {
                return;
            }

            Inventory inv = player.GetInventory();
            if (inv == null)
            {
                return;
            }

            Lookup();
            if (_isKeyCell == null || _freeKey == null)
            {
                return;
            }

            _placing = true;
            try
            {
                List<ItemDrop.ItemData> items = inv.GetAllItems();
                if (items == null)
                {
                    return;
                }

                ItemDrop.ItemData wish = null;
                for (int i = 0; i < items.Count; i++)
                {
                    if (IsWishbone(items[i]))
                    {
                        wish = items[i];
                        break;
                    }
                }

                if (wish == null)
                {
                    return;
                }

                if ((bool)_isKeyCell.Invoke(null, new object[] { inv, wish.m_gridPos.x, wish.m_gridPos.y }))
                {
                    return;
                }

                object cell = _freeKey.Invoke(null, new object[] { inv });
                if (!(cell is Vector2i pos) || pos.x < 0)
                {
                    return;
                }

                if (wish.m_equipped)
                {
                    player.UnequipItem(wish, false);
                }

                wish.m_gridPos = pos;
                if (_changed != null)
                {
                    _changed.Invoke(null, new object[] { inv });
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                _placing = false;
            }
        }

        static void Lookup()
        {
            if (_looked)
            {
                return;
            }

            _looked = true;
            _isKeyCell = _slots == null
                ? null
                : AccessTools.Method(_slots, "IsKeyCell", new[] { typeof(Inventory), typeof(int), typeof(int) });
            _freeKey = _pouch == null
                ? null
                : AccessTools.Method(_pouch, "FreeKey", new[] { typeof(Inventory) });
            _changed = _pouch == null
                ? null
                : AccessTools.Method(_pouch, "Changed", new[] { typeof(Inventory) });
            _manual = _slots == null ? null : AccessTools.Field(_slots, "Manual");
            _setupDrag = AccessTools.Method(typeof(InventoryGui), "SetupDragItem");
            _dragGo = AccessTools.Field(typeof(InventoryGui), "m_dragGo");
        }
    }
}
