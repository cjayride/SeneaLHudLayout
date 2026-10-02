using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    // SkillsReworked allows floor(cap - level) points, so a fractional level left by SkillManager's
    // death loss (30 -> 29.1) can never be refilled.
    static class SkillRespendFix
    {
        const string TenacityGuid = "org.bepinex.plugins.tenacity";
        const string SkillsReworkedGuid = "M2Valheim.SkillsReworked";

        static readonly List<FieldInfo> _managedFields = new List<FieldInfo>();
        static AccessTools.FieldRef<Skills, Dictionary<Skills.SkillType, Skills.Skill>> _skillData;
        static MethodInfo _getCap;
        static bool _managedLookup;

        public static void Apply(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.ContainsKey(TenacityGuid) || !Chainloader.PluginInfos.ContainsKey(SkillsReworkedGuid))
            {
                return;
            }

            Type ui = ModTypes.Find("SkillsReworked", "SkillsReworked.UI.SkillPanel.SkillUiController");
            MethodInfo initialize = ui == null ? null : AccessTools.Method(ui, "Initialize");
            Type cap = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Progression.SkillCapPolicy");
            _getCap = cap == null ? null : AccessTools.Method(cap, "GetCurrentSkillCap", new[] { typeof(Player) });
            if (initialize == null || _getCap == null)
            {
                Debug.LogWarning("[SeneaL HUD Layout] SkillsReworked skill panel not found, so the Tenacity re-spend fix is off.");
                return;
            }

            _skillData = AccessTools.FieldRefAccess<Skills, Dictionary<Skills.SkillType, Skills.Skill>>("m_skillData");
            harmony.Patch(initialize, prefix: new HarmonyMethod(typeof(SkillRespendFix), nameof(BeforeInitialize)));
        }

        static void BeforeInitialize(Player player)
        {
            if (SeneaLHudLayoutPlugin.FixSkillRespendAfterDeath == null || !SeneaLHudLayoutPlugin.FixSkillRespendAfterDeath.Value)
            {
                return;
            }

            try
            {
                Normalize(player);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SeneaL HUD Layout] Tenacity re-spend fix failed: " + e.GetBaseException().Message);
            }
        }

        static void Normalize(Player player)
        {
            Skills skills = player != null ? player.GetSkills() : null;
            if (skills == null)
            {
                return;
            }

            Dictionary<Skills.SkillType, Skills.Skill> data = _skillData(skills);
            if (data == null)
            {
                return;
            }

            float cap = (float)_getCap.Invoke(null, new object[] { player });
            foreach (Skills.SkillType type in ManagedSkillTypes())
            {
                if (!data.TryGetValue(type, out Skills.Skill skill) || skill == null)
                {
                    continue;
                }

                float whole = Mathf.Floor(skill.m_level + 0.0001f);
                if (skill.m_level - whole > 0.0001f && skill.m_level < cap)
                {
                    skill.m_level = whole;
                }
            }
        }

        static IEnumerable<Skills.SkillType> ManagedSkillTypes()
        {
            if (!_managedLookup)
            {
                _managedLookup = true;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type;
                    try
                    {
                        type = assembly.GetType("SkillManager.Skill", false);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    FieldInfo field = type == null ? null : type.GetField("skills", BindingFlags.Static | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        _managedFields.Add(field);
                    }
                }
            }

            for (int i = 0; i < _managedFields.Count; i++)
            {
                if (!(_managedFields[i].GetValue(null) is IDictionary dict))
                {
                    continue;
                }

                foreach (object key in dict.Keys)
                {
                    if (key is Skills.SkillType type)
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
