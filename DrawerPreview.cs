using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class DrawerPreview
    {
        static Type _hoverView;
        static Type _drawer;
        static FieldInfo _slots;
        static FieldInfo _cols;
        static FieldInfo _rows;
        static FieldInfo _prev;
        static FieldInfo _slotRoot;
        static FieldInfo _slotCount;
        static FieldInfo _slotMax;
        static MethodInfo _slotSet;
        static MethodInfo _slotClear;

        public static void Apply(Harmony harmony)
        {
            if (SeneaLHudLayoutPlugin.CollapseDrawerPreview == null
                || !SeneaLHudLayoutPlugin.CollapseDrawerPreview.Value)
            {
                return;
            }

            _drawer = AccessTools.TypeByName("ItemDrawers.Game.DrawerComponent");
            _hoverView = AccessTools.TypeByName("SeneaLUI.Hud.HoverView");
            if (_hoverView == null)
            {
                return;
            }

            _slots = AccessTools.Field(_hoverView, "_slots");
            _cols = AccessTools.Field(_hoverView, "_cols");
            _rows = AccessTools.Field(_hoverView, "_rows");
            _prev = AccessTools.Field(_hoverView, "_prev");

            Type slot = AccessTools.TypeByName("SeneaLUI.Components.Slot");
            if (slot != null)
            {
                _slotRoot = AccessTools.Field(slot, "Root");
                _slotCount = AccessTools.Field(slot, "_count");
                _slotMax = AccessTools.Field(slot, "_max");
                _slotSet = AccessTools.Method(slot, "Set", new[] { typeof(ItemDrop.ItemData) });
                _slotClear = AccessTools.Method(slot, "Clear");
            }

            MethodInfo fill = AccessTools.Method(_hoverView, "FillPreview");
            MethodInfo layout = AccessTools.Method(_hoverView, "Layout");
            if (fill != null)
            {
                harmony.Patch(fill, postfix: new HarmonyMethod(typeof(DrawerPreview), nameof(AfterPreview)));
            }

            if (layout != null)
            {
                harmony.Patch(layout, postfix: new HarmonyMethod(typeof(DrawerPreview), nameof(AfterPreview)));
            }
        }

        static bool IsDrawer(Container container)
        {
            return _drawer != null && container && container.GetComponent(_drawer) != null;
        }

        static void AfterPreview(object __instance, Container c)
        {
            if (__instance == null || !IsDrawer(c))
            {
                return;
            }

            try
            {
                Collapse(__instance, c);
            }
            catch (Exception)
            {
            }
        }

        static void Collapse(object hover, Container container)
        {
            IList slots = _slots?.GetValue(hover) as IList;
            Inventory inv = container.GetInventory();
            if (slots == null || inv == null)
            {
                return;
            }

            _cols?.SetValue(hover, 1);
            _rows?.SetValue(hover, 1);

            List<ItemDrop.ItemData> items = inv.GetAllItems();
            int total = 0;
            ItemDrop.ItemData shown = null;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                if (item == null)
                {
                    continue;
                }

                if (shown == null)
                {
                    shown = item;
                }

                total += item.m_stack;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                object slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                RectTransform root = _slotRoot?.GetValue(slot) as RectTransform;
                if (i == 0 && shown != null)
                {
                    if (root)
                    {
                        root.gameObject.SetActive(true);
                    }

                    _slotSet?.Invoke(slot, new object[] { shown });
                    TMP_Text count = _slotCount?.GetValue(slot) as TMP_Text;
                    if (count)
                    {
                        int cap = Capacity(container);
                        count.text = cap > 0 ? total + "/" + cap : total.ToString();
                        count.gameObject.SetActive(true);
                    }

                    TMP_Text max = _slotMax?.GetValue(slot) as TMP_Text;
                    if (max)
                    {
                        max.gameObject.SetActive(false);
                    }
                }
                else
                {
                    _slotClear?.Invoke(slot, null);
                    if (root)
                    {
                        root.gameObject.SetActive(false);
                    }
                }
            }

            RectTransform prev = _prev?.GetValue(hover) as RectTransform;
            if (prev)
            {
                float size = 44f;
                prev.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size + 16f);
                prev.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size + 16f);
            }
        }

        static int Capacity(Container container)
        {
            Component drawer = container.GetComponent(_drawer);
            if (drawer == null)
            {
                return 0;
            }

            PropertyInfo cap = AccessTools.Property(_drawer, "Capacity");
            if (cap == null)
            {
                return 0;
            }

            object value = cap.GetValue(drawer, null);
            return value is int n ? n : 0;
        }
    }
}
