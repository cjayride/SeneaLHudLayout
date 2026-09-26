using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    static class CompassPin
    {
        public static void Apply(Harmony harmony)
        {
            System.Type type = AccessTools.TypeByName("SeneaLUI.Hud.CompassView");
            MethodInfo place = AccessTools.Method(type, "Place");
            if (place == null)
            {
                return;
            }

            harmony.Patch(place, prefix: new HarmonyMethod(typeof(CompassPin), nameof(IgnoreHotbar)));
        }

        public static void IgnoreHotbar(
            [HarmonyArgument(1)] float sc,
            [HarmonyArgument(2)] ref float hotTop,
            [HarmonyArgument(3)] ref float hotRight,
            [HarmonyArgument(4)] ref float hotBottom)
        {
            hotRight = 0f;
            hotBottom = 0f;
            if (sc > 0.01f)
            {
                hotTop = SeneaLHudLayoutPlugin.CompassTop.Value * sc;
            }
        }
    }
}
