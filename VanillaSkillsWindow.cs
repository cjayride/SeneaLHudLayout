using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace SeneaLHudLayout
{
    static class VanillaSkillsWindow
    {
        const string SkillsReworkedGuid = "M2Valheim.SkillsReworked";
        static readonly System.Version SeneaLPanelFrom = new System.Version(1, 8, 2);

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

        public static bool ReworkedHasSeneaLPanel()
        {
            if (!Chainloader.PluginInfos.TryGetValue(SkillsReworkedGuid, out var info) || info?.Metadata == null)
            {
                return false;
            }

            return info.Metadata.Version.CompareTo(SeneaLPanelFrom) > 0;
        }

        static void ForceVanilla(ref bool on)
        {
            if (ReworkedHasSeneaLPanel())
            {
                return;
            }

            on = false;
        }
    }
}
