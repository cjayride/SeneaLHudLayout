using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    static class RulesNotice
    {
        public static void Apply(Harmony harmony)
        {
            System.Type modal = AccessTools.TypeByName("SeneaLUI.Hud.NoticeModal");
            MethodInfo show = AccessTools.Method(modal, "Show");
            if (show == null)
            {
                return;
            }

            harmony.Patch(show, prefix: new HarmonyMethod(typeof(RulesNotice), nameof(SkipRulesWindow)));
        }

        static bool SkipRulesWindow(string title)
        {
            if (SeneaLHudLayoutPlugin.HideServerRules == null || !SeneaLHudLayoutPlugin.HideServerRules.Value)
            {
                return true;
            }

            System.Type loc = AccessTools.TypeByName("SeneaLUI.Core.Loc");
            string rules = AccessTools.Property(loc, "SrvRulesTitle")?.GetValue(null, null) as string;
            return string.IsNullOrEmpty(rules) || title != rules;
        }
    }
}
