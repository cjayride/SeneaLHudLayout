using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class WalletStacks
    {
        struct Piece
        {
            public ItemDrop.ItemData Source;
            public ItemDrop.ItemData Clone;
        }

        static Func<ItemDrop.ItemData, bool> _isCoin;
        static bool _isCoinReady;
        static bool _depositPurse;
        static readonly List<ItemDrop.ItemData> Held = new List<ItemDrop.ItemData>();
        static readonly List<Piece> Created = new List<Piece>();

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
                harmony.Patch(move,
                    prefix: new HarmonyMethod(typeof(WalletStacks), nameof(BeforeMove)),
                    postfix: new HarmonyMethod(typeof(WalletStacks), nameof(AfterMove)));
            }

            MethodInfo put = AccessTools.Method(transfer, "Put");
            if (put != null)
            {
                harmony.Patch(put,
                    prefix: new HarmonyMethod(typeof(WalletStacks), nameof(BeforePut)),
                    postfix: new HarmonyMethod(typeof(WalletStacks), nameof(AfterPut)));
            }

            MethodInfo putAll = AccessTools.Method(transfer, "PutAll");
            if (putAll != null)
            {
                harmony.Patch(putAll,
                    prefix: new HarmonyMethod(typeof(WalletStacks), nameof(MarkDepositAll)),
                    postfix: new HarmonyMethod(typeof(WalletStacks), nameof(ClearDeposit)));
            }

            MethodInfo putSimilar = AccessTools.Method(transfer, "PutSimilar");
            if (putSimilar != null)
            {
                harmony.Patch(putSimilar,
                    prefix: new HarmonyMethod(typeof(WalletStacks), nameof(MarkDepositSimilar)),
                    postfix: new HarmonyMethod(typeof(WalletStacks), nameof(ClearDeposit)));
            }
        }

        static bool On
        {
            get { return SeneaLHudLayoutPlugin.SplitWalletForChest != null && SeneaLHudLayoutPlugin.SplitWalletForChest.Value; }
        }

        static void MarkDepositAll()
        {
            _depositPurse = On;
        }

        static void MarkDepositSimilar(Inventory __1, Container __2)
        {
            Inventory dest = __2 != null ? __2.GetInventory() : __1;
            _depositPurse = On && ContainsCoin(dest);
        }

        static void ClearDeposit()
        {
            _depositPurse = false;
        }

        static void BeforeStackAll(Inventory __0, Inventory __1)
        {
            Held.Clear();
            if (!On || !IsLocalPlayer(__1))
            {
                return;
            }

            Prepare(__1, __0, null, ContainsCoin(__0));
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
            ReturnStuck(__1);
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

            Held.Clear();
            Changed(__1);
        }

        static void BeforeMove(Inventory __0, Inventory __1, List<ItemDrop.ItemData> __2)
        {
            bool depositPurse = _depositPurse;
            _depositPurse = false;
            if (!On)
            {
                return;
            }

            Prepare(__0, __1, __2, depositPurse);
        }

        static void BeforePut(Container __0, Inventory __1, List<ItemDrop.ItemData> __2)
        {
            bool depositPurse = _depositPurse;
            _depositPurse = false;
            if (!On || __0 == null)
            {
                return;
            }

            Prepare(__1, __0.GetInventory(), __2, depositPurse);
        }

        static void AfterMove(Inventory __0)
        {
            ReturnStuck(__0);
        }

        static void AfterPut(Inventory __1)
        {
            ReturnStuck(__1);
        }

        static void Prepare(Inventory from, Inventory dest, List<ItemDrop.ItemData> moving, bool depositPurse)
        {
            if (!IsLocalPlayer(from) || dest == null || dest == from)
            {
                return;
            }

            var sources = new List<ItemDrop.ItemData>();
            if (moving != null)
            {
                for (int i = 0; i < moving.Count; i++)
                {
                    if (OversizedCoin(moving[i]) && !sources.Contains(moving[i]))
                    {
                        sources.Add(moving[i]);
                    }
                }
            }

            if (depositPurse)
            {
                List<ItemDrop.ItemData> owned = from.GetAllItems();
                for (int i = 0; i < owned.Count; i++)
                {
                    ItemDrop.ItemData item = owned[i];
                    if (OversizedCoin(item) && !sources.Contains(item))
                    {
                        sources.Add(item);
                    }
                }
            }

            if (sources.Count == 0)
            {
                return;
            }

            int budget = Room(dest, sources[0]);
            for (int i = 0; i < sources.Count; i++)
            {
                ItemDrop.ItemData coin = sources[i];
                int send = budget > 0 ? Math.Min(budget, coin.m_stack) : 0;
                int left = send;
                while (left > 0)
                {
                    int take = Math.Min(Max(coin), left);
                    if (!PlaceClone(from, coin, take, moving))
                    {
                        Debug.Log("[SeneaLHudLayout] Left the rest of the coin purse alone. Not enough free inventory slots to split a stack of " + Max(coin) + ".");
                        left = 0;
                        break;
                    }

                    left -= take;
                    budget -= take;
                }

                if (coin.m_stack <= 0)
                {
                    from.GetAllItems().Remove(coin);
                    if (moving != null)
                    {
                        moving.Remove(coin);
                    }
                }
                else if (moving != null && OversizedCoin(coin))
                {
                    moving.Remove(coin);
                }
            }
        }

        static bool PlaceClone(Inventory inventory, ItemDrop.ItemData coin, int take, List<ItemDrop.ItemData> moving)
        {
            Vector2i slot = (Vector2i)AccessTools.Method(typeof(Inventory), "FindEmptySlot").Invoke(inventory, new object[] { false });
            if (slot.x < 0)
            {
                return false;
            }

            ItemDrop.ItemData clone = coin.Clone();
            clone.m_stack = take;
            clone.m_gridPos = slot;
            inventory.GetAllItems().Add(clone);
            coin.m_stack -= take;
            Created.Add(new Piece { Source = coin, Clone = clone });
            if (moving != null)
            {
                moving.Add(clone);
            }

            return true;
        }

        static void ReturnStuck(Inventory inventory)
        {
            if (Created.Count == 0 || inventory == null)
            {
                Created.Clear();
                return;
            }

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            bool changed = false;
            for (int i = 0; i < Created.Count; i++)
            {
                Piece piece = Created[i];
                if (piece.Clone == null || !items.Contains(piece.Clone))
                {
                    continue;
                }

                if (piece.Source != null)
                {
                    piece.Source.m_stack += piece.Clone.m_stack;
                    if (!items.Contains(piece.Source))
                    {
                        items.Add(piece.Source);
                    }
                }

                items.Remove(piece.Clone);
                changed = true;
            }

            Created.Clear();
            if (changed)
            {
                Changed(inventory);
            }
        }

        static int Room(Inventory dest, ItemDrop.ItemData coin)
        {
            if (dest == null || coin == null || coin.m_shared == null)
            {
                return 0;
            }

            MethodInfo method = AccessTools.Method(typeof(Inventory), "FindFreeStackSpace");
            if (method == null)
            {
                return 0;
            }

            ParameterInfo[] parameters = method.GetParameters();
            object[] args = new object[parameters.Length];
            if (parameters.Length > 0)
            {
                args[0] = coin.m_shared.m_name;
            }

            if (parameters.Length > 1)
            {
                args[1] = coin.m_worldLevel;
            }

            try
            {
                object result = method.Invoke(dest, args);
                return result is int room && room > 0 ? room : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        static bool ContainsCoin(Inventory inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                if (IsCoin(items[i]))
                {
                    return true;
                }
            }

            return false;
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
