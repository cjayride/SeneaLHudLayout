using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace SeneaLHudLayout
{
    static class VanillaSkillsWindow
    {
        const string SkillsReworkedGuid = "M2Valheim.SkillsReworked";

        public static void Apply(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.ContainsKey(SkillsReworkedGuid))
            {
                return;
            }

            Type view = AccessTools.TypeByName("SeneaLUI.Inv.SkillsView")
                ?? AccessTools.TypeByName("SkillsView");
            MethodInfo apply = view == null ? null : AccessTools.Method(view, "Apply");
            if (apply == null)
            {
                return;
            }

            harmony.Patch(apply, prefix: new HarmonyMethod(typeof(VanillaSkillsWindow), nameof(ForceVanilla)));
        }

        static void ForceVanilla(ref bool on)
        {
            if (SeneaLHudLayoutPlugin.UseVanillaSkillsWindow != null
                && SeneaLHudLayoutPlugin.UseVanillaSkillsWindow.Value)
            {
                on = false;
            }
        }
    }
}
