using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class KillXpNotice
    {
        static readonly Dictionary<int, float> _dealt = new Dictionary<int, float>();
        static readonly Regex PercentLine = new Regex(@"\b([0-9]{1,3})\s*%", RegexOptions.CultureInvariant);
        static MethodInfo _applyScaling;
        static bool _scalingLookup;

        static string _killName;
        static float _killPct;
        static float _shareUntil;
        static float _killXp;
        static float _killUntil;
        static bool _awaitingMessage;
        static float _showOwnAt;

        public static void Apply(Harmony harmony)
        {
            MethodInfo damage = AccessTools.Method(typeof(Character), "RPC_Damage");
            if (damage != null)
            {
                harmony.Patch(damage, postfix: new HarmonyMethod(typeof(KillXpNotice), nameof(AfterDamage)));
            }

            MethodInfo death = AccessTools.Method(typeof(Character), "OnDeath");
            if (death != null)
            {
                harmony.Patch(death, prefix: new HarmonyMethod(typeof(KillXpNotice), nameof(BeforeDeath)));
            }

            MethodInfo award = AccessTools.Method("SkillsReworked.Systems.Rewards.KillRewardFlow:AwardXp");
            if (award != null)
            {
                harmony.Patch(award, postfix: new HarmonyMethod(typeof(KillXpNotice), nameof(AfterAwardXp)));
            }

            MethodInfo show = AccessTools.Method(typeof(MessageHud), "ShowMessage");
            if (show != null)
            {
                harmony.Patch(show, prefix: new HarmonyMethod(typeof(KillXpNotice), nameof(BeforeShowMessage))
                {
                    priority = Priority.First
                });
            }
        }

        public static void Tick()
        {
            if (!_awaitingMessage || Time.unscaledTime < _showOwnAt)
            {
                return;
            }

            _awaitingMessage = false;
            if (_killXp <= 0f || Time.unscaledTime > _killUntil)
            {
                return;
            }

            ShowOwn();
        }

        static void AfterDamage(Character __instance, HitData hit)
        {
            if (__instance == null || hit == null || __instance.IsPlayer() || __instance.IsTamed())
            {
                return;
            }

            if (!IsLocalAttacker(hit))
            {
                return;
            }

            float amount = hit.GetTotalDamage();
            if (amount <= 0f)
            {
                return;
            }

            int id = __instance.GetInstanceID();
            float prev;
            _dealt.TryGetValue(id, out prev);
            _dealt[id] = prev + amount;
        }

        static void BeforeDeath(Character __instance)
        {
            if (__instance == null || __instance.IsPlayer() || __instance.IsTamed())
            {
                return;
            }

            int id = __instance.GetInstanceID();
            float dealt;
            _dealt.TryGetValue(id, out dealt);
            _dealt.Remove(id);

            float max = __instance.GetMaxHealth();
            _killName = CreatureName(__instance);
            _killPct = max > 0.01f ? Mathf.Clamp(dealt / max * 100f, 0f, 100f) : 0f;
            _shareUntil = Time.unscaledTime + 2f;
        }

        static void AfterAwardXp(Player player, Heightmap.Biome biome, float xp)
        {
            if (SeneaLHudLayoutPlugin.ShowKillXp == null || !SeneaLHudLayoutPlugin.ShowKillXp.Value)
            {
                return;
            }

            if (player == null || player != Player.m_localPlayer || xp <= 0f)
            {
                return;
            }

            _killXp = ScaleXp(player, biome, xp);
            if (_killXp <= 0f)
            {
                return;
            }

            if (Time.unscaledTime > _shareUntil)
            {
                _killName = null;
                _killPct = 0f;
            }

            _killUntil = Time.unscaledTime + 2f;
            _awaitingMessage = true;
            _showOwnAt = Time.unscaledTime + 0.08f;
        }

        static bool BeforeShowMessage(ref string text)
        {
            if (!_awaitingMessage || _killXp <= 0f || string.IsNullOrEmpty(text))
            {
                return true;
            }

            if (text.IndexOf(" XP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (!LooksLikeDamageShare(text))
            {
                return true;
            }

            text = XpLead() + text.Trim();
            _awaitingMessage = false;
            _killXp = 0f;
            return true;
        }

        static void ShowOwn()
        {
            if (MessageHud.instance == null)
            {
                return;
            }

            string name = string.IsNullOrEmpty(_killName) ? "Kill" : _killName;
            string text;
            if (_killPct >= 0.5f)
            {
                text = XpLead() + name + "  " + _killPct.ToString("0") + "%";
            }
            else
            {
                text = XpLead() + name;
            }

            _killXp = 0f;
            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
        }

        static string XpLead()
        {
            float xp = _killXp;
            string amount = xp >= 9.95f ? xp.ToString("0") : xp.ToString("0.#");
            return "+" + amount + " XP  ";
        }

        static string XpSuffix()
        {
            float xp = _killXp;
            string amount = xp >= 9.95f ? xp.ToString("0") : xp.ToString("0.#");
            return "  +" + amount + " XP";
        }

        static bool LooksLikeDamageShare(string text)
        {
            if (text.Length > 96)
            {
                return false;
            }

            Match match = PercentLine.Match(text);
            if (!match.Success)
            {
                return false;
            }

            int pct;
            if (!int.TryParse(match.Groups[1].Value, out pct) || pct > 100)
            {
                return false;
            }

            return true;
        }

        static float ScaleXp(Player player, Heightmap.Biome biome, float xp)
        {
            if (!_scalingLookup)
            {
                _scalingLookup = true;
                Type type = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Progression.LevelXpCalculator");
                _applyScaling = type == null
                    ? null
                    : AccessTools.Method(type, "ApplyLevelScaling", new[] { typeof(Player), typeof(Heightmap.Biome), typeof(float) });
            }

            if (_applyScaling == null)
            {
                return xp;
            }

            try
            {
                return (float)_applyScaling.Invoke(null, new object[] { player, biome, xp });
            }
            catch (Exception)
            {
                return xp;
            }
        }

        static bool IsLocalAttacker(HitData hit)
        {
            Player local = Player.m_localPlayer;
            if (local == null)
            {
                return false;
            }

            Character attacker = hit.GetAttacker();
            if (attacker == null)
            {
                return false;
            }

            if (attacker == local)
            {
                return true;
            }

            MonsterAI ai = attacker.GetComponent<MonsterAI>();
            if (ai == null)
            {
                return false;
            }

            GameObject follow = ai.GetFollowTarget();
            return follow != null && follow.GetComponent<Player>() == local;
        }

        static string CreatureName(Character character)
        {
            string name = character.m_name;
            if (string.IsNullOrEmpty(name))
            {
                return character.name;
            }

            if (Localization.instance != null)
            {
                return Localization.instance.Localize(name);
            }

            return name;
        }
    }
}
