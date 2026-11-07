using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Experimental. Off by default. Throttles optional SeneaL UI ticks when enabled.
    /// Does not touch vitals, hotbar, hover, combat HUD, or messages.
    /// </summary>
    static class SeneaLFpsTweaks
    {
        static readonly Dictionary<string, int> Interval = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "SeneaLUI.Map.OreScout.Tick", 20 },
            { "SeneaLUI.Map.LiveMarks.Tick", 12 },
            { "SeneaLUI.Map.MapPins.Tick", 10 },
            { "SeneaLUI.Hud.TextUpright.Tick", 8 },
            { "SeneaLUI.Hud.TextSizer.Tick", 8 },
            { "SeneaLUI.Hud.TextEdge.Tick", 8 },
            { "SeneaLUI.Hud.ElemFx.Tick", 8 },
            { "SeneaLUI.Hud.ElemCaption.Tick", 8 },
            { "SeneaLUI.Hud.DebugView.Tick", 30 },
            { "SeneaLUI.Hud.HudInspector.Tick", 30 },
            { "SeneaLUI.Hud.WorldPing.Tick", 12 },
            { "SeneaLUI.Hud.JoinToast.Tick", 6 },
            { "SeneaLUI.Hud.ExtElements.Tick", 10 },
            { "SeneaLUI.Inv.StatHistory.Tick", 20 },
            { "SeneaLUI.Components.Gray.Tick", 15 },
            { "SeneaLUI.Core.DevCommands.Tick", 30 },
            { "SeneaLUI.Hud.SaveMark.Tick", 10 },
            { "SeneaLUI.Hud.TabCover.Tick", 10 }
        };

        public static bool On
        {
            get
            {
                return SeneaLHudLayoutPlugin.SeneaLFpsTweaksOn != null
                    && SeneaLHudLayoutPlugin.SeneaLFpsTweaksOn.Value;
            }
        }

        public static void Apply(Harmony harmony)
        {
            foreach (string key in new List<string>(Interval.Keys))
            {
                int dot = key.LastIndexOf('.');
                if (dot <= 0)
                {
                    continue;
                }

                PatchNamed(harmony, key.Substring(0, dot), key.Substring(dot + 1));
            }

            Type inv = AccessTools.TypeByName("SeneaLUI.Patches.InventoryGuiPatches");
            MethodInfo invUpdate = inv != null ? AccessTools.Method(inv, "Update") : null;
            if (invUpdate != null)
            {
                try
                {
                    harmony.Patch(invUpdate, prefix: new HarmonyMethod(typeof(SeneaLFpsTweaks), nameof(SkipClosedInventory)));
                }
                catch (Exception)
                {
                }
            }
        }

        static void PatchNamed(Harmony harmony, string typeName, string methodName)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                return;
            }

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
            {
                if (method == null || method.Name != methodName || method.IsConstructor)
                {
                    continue;
                }

                try
                {
                    harmony.Patch(method, prefix: new HarmonyMethod(typeof(SeneaLFpsTweaks), nameof(Gate)));
                }
                catch (Exception)
                {
                }
            }
        }

        static bool Gate(MethodBase __originalMethod)
        {
            if (!On || __originalMethod == null || __originalMethod.DeclaringType == null)
            {
                return true;
            }

            string key = __originalMethod.DeclaringType.FullName + "." + __originalMethod.Name;
            int n;
            if (!Interval.TryGetValue(key, out n) || n <= 1)
            {
                return true;
            }

            return Time.frameCount % n == 0;
        }

        static bool SkipClosedInventory()
        {
            if (!On)
            {
                return true;
            }

            try
            {
                return InventoryGui.IsVisible();
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
