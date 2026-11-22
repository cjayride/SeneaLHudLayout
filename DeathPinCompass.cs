using System;
using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Valheim death pins are added with isChecked false. SeneaL UI's CompassPins = Marked
    /// only shows pins marked for the compass, so the grave marker stays off the compass until
    /// you toggle it in the pin dialog. This marks new death pins for the compass automatically.
    /// </summary>
    static class DeathPinCompass
    {
        static MethodInfo _addPin;
        static MethodInfo _shown;

        public static void Apply(Harmony harmony)
        {
            // Current Valheim AddPin also takes Splatform.PlatformUserID. Match by name so a
            // parameter list from an older game build cannot fail the lookup.
            _addPin = AccessTools.Method(typeof(Minimap), nameof(Minimap.AddPin));
            if (_addPin != null)
            {
                harmony.Patch(_addPin, postfix: new HarmonyMethod(typeof(DeathPinCompass), nameof(AfterAddPin)));
            }

            Type view = AccessTools.TypeByName("SeneaLUI.Hud.CompassView");
            _shown = view != null ? AccessTools.Method(view, "Shown") : null;
            if (_shown != null)
            {
                harmony.Patch(_shown, postfix: new HarmonyMethod(typeof(DeathPinCompass), nameof(AllowDeathOnCompass)));
            }
        }

        static bool Enabled()
        {
            return SeneaLHudLayoutPlugin.DeathPinOnCompass != null && SeneaLHudLayoutPlugin.DeathPinOnCompass.Value;
        }

        static void AfterAddPin(Minimap.PinData __result, Minimap.PinType type)
        {
            if (!Enabled() || __result == null || type != Minimap.PinType.Death)
            {
                return;
            }

            __result.m_checked = true;
        }

        static void AllowDeathOnCompass(Minimap.PinData pin, ref bool __result)
        {
            if (!Enabled() || pin == null || pin.m_type != Minimap.PinType.Death)
            {
                return;
            }

            __result = true;
        }
    }
}
