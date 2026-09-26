using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class ResourceBars
    {
        static Type _hudRoot;
        static Type _staminaView;
        static Type _row;
        static FieldInfo _viewField;
        static FieldInfo _staminaRow;
        static FieldInfo _eitrRow;
        static FieldInfo _adrenalineRow;
        static FieldInfo _groupField;
        static bool _ready;

        public static void Apply()
        {
            if (!Ready())
            {
                return;
            }

            object view = _viewField.GetValue(null);
            if (view == null)
            {
                return;
            }

            if (On(SeneaLHudLayoutPlugin.AlwaysShowStamina))
            {
                Show(_staminaRow.GetValue(view));
            }

            if (On(SeneaLHudLayoutPlugin.AlwaysShowEitr))
            {
                Show(_eitrRow.GetValue(view));
            }

            if (On(SeneaLHudLayoutPlugin.AlwaysShowAdrenaline))
            {
                Show(_adrenalineRow.GetValue(view));
            }
        }

        static bool On(object entry)
        {
            var config = entry as BepInEx.Configuration.ConfigEntry<bool>;
            return config != null && config.Value;
        }

        static void Show(object row)
        {
            if (row == null)
            {
                return;
            }

            CanvasGroup group = _groupField.GetValue(row) as CanvasGroup;
            if (!group)
            {
                return;
            }

            if (!group.gameObject.activeSelf)
            {
                group.gameObject.SetActive(true);
            }

            if (group.alpha < 1f)
            {
                group.alpha = 1f;
            }
        }

        static bool Ready()
        {
            if (_ready)
            {
                return _viewField != null;
            }

            _ready = true;
            _hudRoot = AccessTools.TypeByName("SeneaLUI.Hud.HudRoot");
            _staminaView = AccessTools.TypeByName("SeneaLUI.Hud.StaminaView");
            _row = AccessTools.Inner(_staminaView, "Row");
            _viewField = AccessTools.Field(_hudRoot, "_stamina");
            _staminaRow = AccessTools.Field(_staminaView, "_stamina");
            _eitrRow = AccessTools.Field(_staminaView, "_eitr");
            _adrenalineRow = AccessTools.Field(_staminaView, "_adrenaline");
            _groupField = AccessTools.Field(_row, "Group");
            return _viewField != null && _groupField != null;
        }
    }
}
