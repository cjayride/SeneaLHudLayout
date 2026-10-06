using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SeneaL UI draws a queue position, 1 then 2, in the top left of an item icon
    /// while that item waits to equip. There is no switch for it in SeneaL UI.
    /// </summary>
    static class EquipQueueMark
    {
        public static void Apply(Harmony harmony)
        {
            System.Type slot = AccessTools.TypeByName("SeneaLUI.Components.Slot");
            MethodInfo show = slot == null ? null : AccessTools.Method(slot, "ShowMark");
            if (show == null)
            {
                return;
            }

            harmony.Patch(show, postfix: new HarmonyMethod(typeof(EquipQueueMark), nameof(AfterMark)));
        }

        static void AfterMark(object __instance)
        {
            if (SeneaLHudLayoutPlugin.ShowEquipQueue != null && SeneaLHudLayoutPlugin.ShowEquipQueue.Value)
            {
                return;
            }

            Component mark = AccessTools.Field(__instance.GetType(), "_markRoot")?.GetValue(__instance) as Component;
            if (mark != null && mark.gameObject.activeSelf)
            {
                mark.gameObject.SetActive(false);
            }
        }
    }
}
