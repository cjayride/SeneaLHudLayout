using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    public enum StatusCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    static class StatusEffectsPlace
    {
        static FieldInfo _box;
        static FieldInfo _squaresEnabled;
        static bool _squaresLookup;

        public static void Apply(Harmony harmony)
        {
            Type view = AccessTools.TypeByName("SeneaLUI.Hud.StatusEffectsView");
            MethodInfo tick = view != null ? AccessTools.Method(view, "Tick") : null;
            if (tick == null)
            {
                return;
            }

            _box = AccessTools.Field(view, "_box");
            harmony.Patch(tick, postfix: new HarmonyMethod(typeof(StatusEffectsPlace), nameof(AfterTick))
            {
                priority = Priority.Last
            });
        }

        static void AfterTick(object __instance)
        {
            if (SeneaLHudLayoutPlugin.StatusMove == null || !SeneaLHudLayoutPlugin.StatusMove.Value || SquaresOwnIt())
            {
                return;
            }

            RectTransform box = _box != null ? _box.GetValue(__instance) as RectTransform : null;
            if (box == null)
            {
                return;
            }

            StatusCorner corner = SeneaLHudLayoutPlugin.StatusCornerSetting != null
                ? SeneaLHudLayoutPlugin.StatusCornerSetting.Value
                : StatusCorner.TopLeft;
            float x = SeneaLHudLayoutPlugin.StatusOffsetX != null ? SeneaLHudLayoutPlugin.StatusOffsetX.Value : 32f;
            float y = SeneaLHudLayoutPlugin.StatusOffsetY != null ? SeneaLHudLayoutPlugin.StatusOffsetY.Value : -96f;
            bool top = corner == StatusCorner.TopLeft || corner == StatusCorner.TopRight;
            bool left = corner == StatusCorner.TopLeft || corner == StatusCorner.BottomLeft;
            if (!top)
            {
                y += box.sizeDelta.y;
            }

            if (box.name == "status")
            {
                float stack = SiblingHeight(box, "states");
                y += top ? -stack : stack;
            }

            Vector2 anchor = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            box.anchorMin = anchor;
            box.anchorMax = anchor;
            box.pivot = new Vector2(left ? 0f : 1f, 1f);
            box.anchoredPosition = new Vector2(x, y);
        }

        static float SiblingHeight(RectTransform box, string name)
        {
            Transform sibling = box.parent != null ? box.parent.Find(name) : null;
            RectTransform rect = sibling as RectTransform;
            if (rect == null || !rect.gameObject.activeSelf)
            {
                return 0f;
            }

            return rect.sizeDelta.y + 8f;
        }

        static bool SquaresOwnIt()
        {
            if (!_squaresLookup)
            {
                _squaresLookup = true;
                Type plugin = ModTypes.Find("CompactStatusSquares", "CompactStatusSquares.Plugin");
                _squaresEnabled = plugin != null ? AccessTools.Field(plugin, "EnabledIcons") : null;
            }

            if (_squaresEnabled == null)
            {
                return false;
            }

            ConfigEntry<bool> enabled = _squaresEnabled.GetValue(null) as ConfigEntry<bool>;
            return enabled != null && enabled.Value;
        }
    }
}
