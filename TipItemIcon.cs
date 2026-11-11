using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Small item sprite on the tooltip type line (Sub): [hoe] Tool.
    /// Does not use SeneaL's 52px Tip.Icon box (that expands the header).
    /// </summary>
    static class TipItemIcon
    {
        const string Name = "SeneaLHudLayout_TipItemIcon";
        const float Gap = 3f;
        const float MaxPx = 13f;

        static FieldInfo _sub;
        static FieldInfo _title;
        static FieldInfo _panel;
        static Sprite _sprite;
        static Image _img;

        public static void Apply(Harmony harmony)
        {
            Type tooltip = AccessTools.TypeByName("SeneaLUI.Components.Tooltip");
            Type tips = AccessTools.TypeByName("SeneaLUI.Inv.Tips");
            if (tooltip == null)
            {
                return;
            }

            _sub = AccessTools.Field(tooltip, "_sub");
            _title = AccessTools.Field(tooltip, "_title");
            _panel = AccessTools.Field(tooltip, "_panel");

            MethodInfo forItem = tips != null ? AccessTools.Method(tips, "ForItem") : null;
            if (forItem != null)
            {
                harmony.Patch(forItem, postfix: new HarmonyMethod(typeof(TipItemIcon), nameof(AfterForItem)));
            }

            MethodInfo layout = AccessTools.Method(tooltip, "Layout");
            if (layout != null)
            {
                harmony.Patch(layout, postfix: new HarmonyMethod(typeof(TipItemIcon), nameof(AfterLayout)));
            }
        }

        static void AfterForItem(ItemDrop.ItemData item)
        {
            try
            {
                _sprite = item != null ? item.GetIcon() : null;
            }
            catch (Exception)
            {
                _sprite = null;
            }
        }

        static bool On
        {
            get
            {
                return SeneaLHudLayoutPlugin.ShowTooltipItemIcon == null
                    || SeneaLHudLayoutPlugin.ShowTooltipItemIcon.Value;
            }
        }

        static void AfterLayout(object tip)
        {
            TMP_Text sub = GetText(_sub);
            TMP_Text title = GetText(_title);
            RectTransform panel = _panel != null ? _panel.GetValue(null) as RectTransform : null;
            TMP_Text line = Shown(sub) ? sub : title;
            if (!On || panel == null || line == null || _sprite == null || ModdedTip(tip))
            {
                Hide();
                return;
            }

            Image icon = Ensure(panel);
            if (icon == null)
            {
                return;
            }

            float lineH = line.fontSize > 1f ? line.fontSize : 12f;
            float size = Mathf.Min(MaxPx, lineH);
            icon.sprite = _sprite;
            icon.enabled = true;
            if (!icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(true);
            }

            RectTransform ir = icon.rectTransform;
            ir.SetParent(panel, false);
            ir.anchorMin = new Vector2(0f, 1f);
            ir.anchorMax = new Vector2(0f, 1f);
            ir.pivot = new Vector2(0f, 1f);
            ir.sizeDelta = new Vector2(size, size);

            RectTransform tr = line.rectTransform;
            Vector2 pos = tr.anchoredPosition;
            ir.anchoredPosition = new Vector2(pos.x, pos.y - Mathf.Max(0f, (lineH - size) * 0.15f));
            tr.anchoredPosition = new Vector2(pos.x + size + Gap, pos.y);
        }

        static bool ModdedTip(object tip)
        {
            // SeneaL's tooltip panel is POOLED: one panel serves every tooltip. A third-party
            // tooltip (RepairRequiresCoins and friends) sets tip.Source to brand itself and draws
            // its own header icon. Never add ours on top of theirs, and clear our sprite once so
            // the pooled header icon cannot leak from an item tooltip into theirs.
            try
            {
                if (tip == null)
                {
                    return false;
                }

                object source = AccessTools.Field(tip.GetType(), "Source")?.GetValue(tip);
                if (source is string s && s.Length > 0)
                {
                    _sprite = null;
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        static Image Ensure(RectTransform panel)
        {
            if (_img != null)
            {
                return _img;
            }

            Transform existing = panel.Find(Name);
            if (existing != null)
            {
                _img = existing.GetComponent<Image>();
                if (_img != null)
                {
                    return _img;
                }
            }

            var go = new GameObject(Name, typeof(RectTransform), typeof(Image));
            go.layer = panel.gameObject.layer;
            go.transform.SetParent(panel, false);
            _img = go.GetComponent<Image>();
            _img.raycastTarget = false;
            _img.preserveAspect = true;
            return _img;
        }

        static void Hide()
        {
            if (_img != null && _img.gameObject.activeSelf)
            {
                _img.gameObject.SetActive(false);
            }
        }

        static TMP_Text GetText(FieldInfo field)
        {
            try
            {
                return field != null ? field.GetValue(null) as TMP_Text : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static bool Shown(TMP_Text text)
        {
            return text != null
                && text.gameObject.activeSelf
                && !string.IsNullOrEmpty(text.text);
        }
    }
}
