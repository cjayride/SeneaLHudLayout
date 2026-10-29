using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SeneaL parks the wishbone in the charm / utility cell. Optionally treat it as a key
    /// so pickup and drag go to a dungeon-key slot instead.
    /// </summary>
    static class WishboneKeySlot
    {
        const string Token = "$item_wishbone";

        static Type _slots;
        static Type _pouch;
        static MethodInfo _isKeyCell;
        static MethodInfo _freeKey;
        static MethodInfo _changed;
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

            MethodInfo changed = AccessTools.Method(typeof(Inventory), "Changed");
            if (changed != null)
            {
                harmony.Patch(changed, postfix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(AfterInventory)));
            }

            MethodInfo equip = AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem));
            if (equip != null)
            {
                harmony.Patch(equip, postfix: new HarmonyMethod(typeof(WishboneKeySlot), nameof(AfterEquip)));
            }

            if (SeneaLHudLayoutPlugin.WishboneInKeySlot != null)
            {
                SeneaLHudLayoutPlugin.WishboneInKeySlot.SettingChanged += (_, __) => Place(Player.m_localPlayer);
            }
        }

        static void AfterInventory(Inventory __instance)
        {
            Player player = Player.m_localPlayer;
            if (player != null && player.GetInventory() == __instance)
            {
                Place(player);
            }
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

        static void Place(Player player)
        {
            if (_placing || !On() || player == null)
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
        }
    }
}
