using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SeneaL ModsView.CmPresent only looks up com.bepis.bepinex.configurationmanager.
    /// shudnal ConfigurationManager uses _shudnal.ConfigurationManager, so UseCm stays false
    /// and F1 keeps opening SeneaL's own mods window even when
    /// ModSettingsWindow = ConfigurationManager.
    /// </summary>
    static class ShudnalConfigBridge
    {
        const string ShudnalCmGuid = "_shudnal.ConfigurationManager";

        public static void Apply(Harmony harmony)
        {
            Type modsView = AccessTools.TypeByName("SeneaLUI.Hud.ModsView");
            MethodInfo getter = modsView == null
                ? null
                : AccessTools.PropertyGetter(modsView, "CmPresent");
            if (getter == null)
            {
                return;
            }

            harmony.Patch(getter, postfix: new HarmonyMethod(typeof(ShudnalConfigBridge), nameof(CmPresentPostfix)));
        }

        static void CmPresentPostfix(ref bool __result)
        {
            if (__result)
            {
                return;
            }

            Type modsView = AccessTools.TypeByName("SeneaLUI.Hud.ModsView");
            if (modsView == null)
            {
                return;
            }

            if (!Chainloader.PluginInfos.TryGetValue(ShudnalCmGuid, out PluginInfo info) ||
                info?.Instance == null)
            {
                return;
            }

            PropertyInfo display = AccessTools.Property(info.Instance.GetType(), "DisplayingWindow");
            if (display == null)
            {
                return;
            }

            AccessTools.Field(modsView, "_cm")?.SetValue(null, info.Instance);
            AccessTools.Field(modsView, "_cmProp")?.SetValue(null, display);
            AccessTools.Field(modsView, "_cmChecked")?.SetValue(null, true);
            __result = true;
        }
    }
}
