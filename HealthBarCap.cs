using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// After SeneaL HealthCapacity (or HUD Fixed length) computes bar length, clamp
    /// that length to HealthBarMaxHeight. Layout, Meter.Resize, and Meter.Set then
    /// all use the capped size; fill stays current / max of that height.
    /// </summary>
    static class HealthBarCap
    {
        static MethodInfo _clamp;

        public static void Apply(Harmony harmony)
        {
            Type vitals = AccessTools.TypeByName("SeneaLUI.Hud.VitalsView");
            MethodInfo tick = vitals == null
                ? null
                : AccessTools.Method(vitals, "Tick", new[] { typeof(Player) });
            if (tick == null)
            {
                Debug.LogWarning("[SeneaL HUD Layout] VitalsView.Tick not found; health bar height cap is off.");
                return;
            }

            _clamp = AccessTools.Method(typeof(HealthBarCap), nameof(ClampHeight));
            harmony.Patch(tick, transpiler: new HarmonyMethod(typeof(HealthBarCap), nameof(TranspileTick)));
        }

        public static float ClampHeight(float height)
        {
            if (SeneaLHudLayoutPlugin.HealthBarMaxHeight == null)
            {
                return height;
            }

            float cap = SeneaLHudLayoutPlugin.HealthBarMaxHeight.Value;
            if (cap < 40f || height <= cap)
            {
                return height;
            }

            return cap;
        }

        static IEnumerable<CodeInstruction> TranspileTick(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            int extra = 0;

            for (int i = 0; i < code.Count; i++)
            {
                if (IsLengthLocalStore(code, i))
                {
                    object local = code[i].operand;
                    yield return code[i];
                    yield return new CodeInstruction(OpCodes.Ldloc_S, local);
                    yield return new CodeInstruction(OpCodes.Call, _clamp);
                    yield return new CodeInstruction(OpCodes.Stloc_S, local);
                    extra++;
                    continue;
                }

                if (IsBarHeightStore(code[i]) || IsMeterResize(code[i]))
                {
                    yield return new CodeInstruction(OpCodes.Call, _clamp);
                    extra++;
                }

                yield return code[i];
            }

            if (extra < 2)
            {
                Debug.LogWarning("[SeneaL HUD Layout] Health bar height cap patched " + extra
                    + " site(s); expected at least 2. SeneaL UI layout may have changed.");
            }
        }

        static bool IsLengthLocalStore(List<CodeInstruction> code, int i)
        {
            if (i + 2 >= code.Count || code[i].opcode != OpCodes.Stloc_S)
            {
                return false;
            }

            CodeInstruction next = code[i + 1];
            CodeInstruction thick = code[i + 2];
            if (next.opcode != OpCodes.Ldsfld)
            {
                return false;
            }

            var field = thick.operand as FieldInfo;
            return thick.opcode == OpCodes.Ldfld && field != null && field.Name == "Thick";
        }

        static bool IsBarHeightStore(CodeInstruction ins)
        {
            if (ins == null || ins.opcode != OpCodes.Stfld)
            {
                return false;
            }

            var field = ins.operand as FieldInfo;
            return field != null && field.Name == "_barH";
        }

        static bool IsMeterResize(CodeInstruction ins)
        {
            if (ins == null || (ins.opcode != OpCodes.Callvirt && ins.opcode != OpCodes.Call))
            {
                return false;
            }

            var method = ins.operand as MethodInfo;
            return method != null
                && method.Name == "Resize"
                && method.DeclaringType != null
                && method.DeclaringType.Name == "Meter";
        }
    }
}
