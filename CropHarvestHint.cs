using System;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Plant Easily's hover hint asks ZInput which action is bound to LeftShift.
    /// That lookup comes back as Q. The harvest itself is still Shift plus the use key.
    /// </summary>
    static class CropHarvestHint
    {
        static readonly Regex AreaHint = new Regex(
            @"\[<color=yellow><b>[^<]*</b></color>\](?=[^\n]*\(area\))",
            RegexOptions.CultureInvariant);

        static PropertyInfo _config;
        static PropertyInfo _modifier;
        static bool _keyReady;
        static KeyCode _key;
        static string _shown;

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(Pickable), nameof(Pickable.GetHoverText)),
                postfix: new HarmonyMethod(typeof(CropHarvestHint), nameof(AfterHover)) { priority = Priority.Last });
            harmony.Patch(
                AccessTools.Method(typeof(Beehive), nameof(Beehive.GetHoverText)),
                postfix: new HarmonyMethod(typeof(CropHarvestHint), nameof(AfterHover)) { priority = Priority.Last });
        }

        static void AfterHover(ref string __result)
        {
            if (string.IsNullOrEmpty(__result) || __result.IndexOf("(area)", StringComparison.Ordinal) < 0)
            {
                return;
            }

            string shown = Shown();
            if (shown == null || __result.IndexOf(shown, StringComparison.Ordinal) >= 0)
            {
                return;
            }

            __result = AreaHint.Replace(__result, shown);
        }

        static string Shown()
        {
            if (_shown != null)
            {
                return _shown;
            }

            if (Localization.instance == null)
            {
                return null;
            }

            string modifier = ModifierLabel(ModifierKey());
            if (modifier == null)
            {
                _shown = null;
                return null;
            }

            _shown = "[<color=yellow><b>" + modifier + " + " + Localization.instance.Localize("$KEY_Use") + "</b></color>]";
            return _shown;
        }

        static KeyCode ModifierKey()
        {
            if (_keyReady)
            {
                return _key;
            }

            _keyReady = true;
            Type context = AccessTools.TypeByName("Advize_PlantEasily.ModContext");
            _config = context != null ? AccessTools.Property(context, "config") : null;
            object config = _config?.GetValue(null, null);
            _modifier = config != null ? AccessTools.Property(config.GetType(), "KeyboardHarvestModifierKey") : null;
            _key = config != null && _modifier?.GetValue(config, null) is KeyCode key ? key : KeyCode.None;
            return _key;
        }

        static string ModifierLabel(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return null;
            }

            switch (key)
            {
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return "Shift";
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return "Ctrl";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return "Alt";
                default:
                    string name = key.ToString();
                    return name.StartsWith("Alpha") ? name.Substring(5) : name;
            }
        }
    }
}
