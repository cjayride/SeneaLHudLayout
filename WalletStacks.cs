using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class WalletStacks
    {
        static Func<ItemDrop.ItemData, bool> _isCoin;
        static bool _isCoinReady;
        static readonly List<ItemDrop.ItemData> Held = new List<ItemDrop.ItemData>();

        public static void Apply(Harmony harmony)
        {
            Type stackAll = AccessTools.TypeByName("StackAllPatch");
            MethodInfo stackPrefix = AccessTools.Method(stackAll, "Prefix");
            if (stackPrefix != null)
            {
                harmony.Patch(stackPrefix,
                    prefix: new HarmonyMethod(typeof(WalletStacks), nameof(BeforeStackAll)),
                    postfix: new HarmonyMethod(typeof(WalletStacks), nameof(AfterStackAll)));
            }

            Type transfer = AccessTools.TypeByName("SeneaLUI.Inv.Transfer");
            MethodInfo move = AccessTools.Method(transfer, "Move");
            if (move != null)
            {
                harmony.Patch(move, prefix: new HarmonyMethod(typeof(WalletStacks), nameof(BeforeMove)));
            }
        }

        static void BeforeStackAll(Inventory __1)
        {
            Held.Clear();
            Player player = Player.m_localPlayer;
            if (__1 == null || player == null || player.GetInventory() != __1)
            {
                return;
            }

            Split(__1, null);
            List<ItemDrop.ItemData> items = __1.GetAllItems();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                ItemDrop.ItemData item = items[i];
                if (!OversizedCoin(item))
                {
                    continue;
                }

                Held.Add(item);
                items.RemoveAt(i);
            }
        }

        static void AfterStackAll(Inventory __1)
        {
            if (Held.Count == 0 || __1 == null)
            {
                Held.Clear();
                return;
            }

            List<ItemDrop.ItemData> items = __1.GetAllItems();
            for (int i = 0; i < Held.Count; i++)
            {
                if (!items.Contains(Held[i]))
                {
                    items.Add(Held[i]);
                }
            }

            Debug.Log("[SeneaLHudLayout] Left " + Held.Count + " coin stack(s) in the wallet. Not enough free slots to split them into stacks of 999.");
            Held.Clear();
            AccessTools.Method(typeof(Inventory), "Changed").Invoke(__1, new object[] { true, false });
        }

        static void BeforeMove(Inventory __0, List<ItemDrop.ItemData> __2)
        {
            if (__0 == null || __2 == null)
            {
                return;
            }

            Split(__0, __2);
            for (int i = __2.Count - 1; i >= 0; i--)
            {
                if (OversizedCoin(__2[i]))
                {
                    __2.RemoveAt(i);
                }
            }
        }

        static void Split(Inventory inventory, List<ItemDrop.ItemData> also)
        {
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            var coins = new List<ItemDrop.ItemData>();
            for (int i = 0; i < items.Count; i++)
            {
                if (OversizedCoin(items[i]))
                {
                    coins.Add(items[i]);
                }
            }

            for (int i = 0; i < coins.Count; i++)
            {
                ItemDrop.ItemData item = coins[i];
                int max = Max(item);
                while (item.m_stack > max)
                {
                    Vector2i slot = (Vector2i)AccessTools.Method(typeof(Inventory), "FindEmptySlot").Invoke(inventory, new object[] { false });
                    if (slot.x < 0)
                    {
                        break;
                    }

                    ItemDrop.ItemData clone = item.Clone();
                    clone.m_stack = max;
                    clone.m_gridPos = slot;
                    items.Add(clone);
                    item.m_stack -= max;
                    if (also != null && also.Contains(item))
                    {
                        also.Add(clone);
                    }
                }
            }
        }

        static bool OversizedCoin(ItemDrop.ItemData item)
        {
            return IsCoin(item) && item.m_stack > Max(item);
        }

        static int Max(ItemDrop.ItemData item)
        {
            int max = item.m_shared.m_maxStackSize;
            return max > 0 ? max : 999;
        }

        static bool IsCoin(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return false;
            }

            if (!_isCoinReady)
            {
                _isCoinReady = true;
                MethodInfo method = AccessTools.Method("SeneaLUI.Inv.Slots:IsCoin");
                if (method != null)
                {
                    _isCoin = (Func<ItemDrop.ItemData, bool>)Delegate.CreateDelegate(typeof(Func<ItemDrop.ItemData, bool>), method);
                }
            }

            if (_isCoin != null)
            {
                return _isCoin(item);
            }

            return item.m_shared.m_name == "$item_coins";
        }
    }
}
