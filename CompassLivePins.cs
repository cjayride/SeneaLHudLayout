using System;
using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SeneaL UI's LiveShips, LiveCarts and LivePortals switches cover the map, the minimap
    /// and the compass together. This keeps them on the maps while leaving the compass clear.
    /// </summary>
    static class CompassLivePins
    {
        static int _ship = int.MinValue;
        static int _cart = int.MinValue;
        static int _portal = int.MinValue;
        static bool _looked;

        public static void Apply(Harmony harmony)
        {
            Type view = AccessTools.TypeByName("SeneaLUI.Hud.CompassView");
            MethodInfo shown = view != null ? AccessTools.Method(view, "Shown") : null;
            if (shown == null)
            {
                return;
            }

            harmony.Patch(shown, postfix: new HarmonyMethod(typeof(CompassLivePins), nameof(DropLivePin)));
        }

        static void DropLivePin([HarmonyArgument(0)] Minimap.PinData pin, ref bool __result)
        {
            if (!__result || pin == null)
            {
                return;
            }

            Lookup();
            int type = (int)pin.m_type;
            if (type == _ship)
            {
                __result = Allowed(SeneaLHudLayoutPlugin.ShipsOnCompass);
            }
            else if (type == _cart)
            {
                __result = Allowed(SeneaLHudLayoutPlugin.CartsOnCompass);
            }
            else if (type == _portal)
            {
                __result = Allowed(SeneaLHudLayoutPlugin.PortalsOnCompass);
            }
        }

        static bool Allowed(BepInEx.Configuration.ConfigEntry<bool> entry)
        {
            return entry == null || entry.Value;
        }

        static void Lookup()
        {
            if (_looked)
            {
                return;
            }

            _looked = true;
            Type marks = AccessTools.TypeByName("SeneaLUI.Map.LiveMarks");
            if (marks == null)
            {
                return;
            }

            _ship = PinValue(marks, "PinShip");
            _cart = PinValue(marks, "PinCart");
            _portal = PinValue(marks, "PinPortal");
        }

        static int PinValue(Type marks, string field)
        {
            FieldInfo info = AccessTools.Field(marks, field);
            object value = info != null ? info.GetValue(null) : null;
            return value != null ? Convert.ToInt32(value) : int.MinValue;
        }
    }
}
