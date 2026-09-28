using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    static class WalletStacks
    {
        static Func<ItemDrop.ItemData, bool> _isCoin;
        static bool _isCoinReady;
        static MethodInfo _isWalletCell;
        static bool _walletChecked;
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

            MethodInfo put = AccessTools.Method(transfer, "Put");
            if (put != null)
            {
                harmony.Patch(put, prefix: new HarmonyMethod(typeof(WalletStacks), nameof(BeforePut)));
            }
        }

        static bool On
        {
            get { return SeneaLHudLayoutPlugin.SplitWalletForChest != null && SeneaLHudLayoutPlugin.SplitWalletForChest.Value; }
        }

        static void BeforeStackAll(Inventory __1)
        {
            Held.Clear();
            if (!On || !IsLocalPlayer(__1))
            {
                return;
            }

            List<ItemDrop.ItemData> items = __1.GetAllItems();
            if (items == null)
            {
                return;
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                ItemDrop.ItemData item = items[i];
                if (!InPurse(__1, item))
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
            if (items != null)
            {
                for (int i = 0; i < Held.Count; i++)
                {
                    if (!items.Contains(Held[i]))
                    {
                        items.Add(Held[i]);
                    }
                }
            }

            Held.Clear();
            Changed(__1);
        }

        static void BeforeMove(Inventory __0, Inventory __1, List<ItemDrop.ItemData> __2)
        {
            if (!On || __1 == null || __1 == __0)
            {
                return;
            }

            StripPurse(__0, __2);
        }

        static void BeforePut(Inventory __1, List<ItemDrop.ItemData> __2)
        {
            if (!On)
            {
                return;
            }

            StripPurse(__1, __2);
        }

        static void StripPurse(Inventory inventory, List<ItemDrop.ItemData> moving)
        {
            if (!IsLocalPlayer(inventory) || moving == null)
            {
                return;
            }

            for (int i = moving.Count - 1; i >= 0; i--)
            {
                if (InPurse(inventory, moving[i]))
                {
                    moving.RemoveAt(i);
                }
            }
        }

        static bool InPurse(Inventory inventory, ItemDrop.ItemData item)
        {
            if (!IsCoin(item) || inventory == null)
            {
                return false;
            }

            if (!_walletChecked)
            {
                _walletChecked = true;
                _isWalletCell = AccessTools.Method("SeneaLUI.Inv.Slots:IsWalletCell");
            }

            if (_isWalletCell == null)
            {
                return item.m_stack > Max(item);
            }

            try
            {
                object result = _isWalletCell.Invoke(null, new object[] { inventory, item.m_gridPos.x, item.m_gridPos.y });
                return result is bool purse && purse;
            }
            catch (Exception)
            {
                return item.m_stack > Max(item);
            }
        }

        static bool IsLocalPlayer(Inventory inventory)
        {
            Player player = Player.m_localPlayer;
            return inventory != null && player != null && player.GetInventory() == inventory;
        }

        static void Changed(Inventory inventory)
        {
            if (inventory == null)
            {
                return;
            }

            AccessTools.Method(typeof(Inventory), "Changed").Invoke(inventory, new object[] { true, false });
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
                try
                {
                    return _isCoin(item);
                }
                catch (Exception)
                {
                    return item.m_shared.m_name == "$item_coins";
                }
            }

            return item.m_shared.m_name == "$item_coins";
        }
    }
}
