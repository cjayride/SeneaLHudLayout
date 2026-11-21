using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SeneaL UI draws up to 4 stacked boss cards (BossBarView.MaxCards). Vanilla EnemyHud
    /// also clones a bar per IsBoss(). Cap both to [BossBar] MaxVisible.
    /// </summary>
    static class BossBarCap
    {
        static readonly List<HudEntry> HudPool = new List<HudEntry>(8);
        static FieldInfo _huds;
        static FieldInfo _hudCharacter;
        static FieldInfo _hudGui;
        static FieldInfo _hudName;
        static FieldInfo _want;
        static FieldInfo _dist;
        static FieldInfo _extra;
        static FieldInfo _primary;
        static bool _lookedHud;
        static bool _lookedSenea;

        struct HudEntry
        {
            public Character Character;
            public GameObject Gui;
            public TMP_Text Name;
            public float Rank;
        }

        public static void Apply(Harmony harmony)
        {
            MethodInfo update = AccessTools.Method(typeof(EnemyHud), "UpdateHuds");
            if (update != null)
            {
                harmony.Patch(update, postfix: new HarmonyMethod(typeof(BossBarCap), nameof(AfterVanilla)));
            }

            Type view = AccessTools.TypeByName("SeneaLUI.Hud.BossBarView");
            MethodInfo scan = view == null ? null : AccessTools.Method(view, "Scan");
            if (scan != null)
            {
                harmony.Patch(scan, postfix: new HarmonyMethod(typeof(BossBarCap), nameof(AfterSeneaL)));
            }
        }

        static int Max()
        {
            ConfigEntry<int> entry = SeneaLHudLayoutPlugin.BossBarMaxVisible;
            if (entry == null)
            {
                return 1;
            }

            return Mathf.Clamp(entry.Value, 1, 8);
        }

        static bool CountOn()
        {
            return SeneaLHudLayoutPlugin.BossBarShowHiddenCount != null
                && SeneaLHudLayoutPlugin.BossBarShowHiddenCount.Value;
        }

        // SeneaL's own panel: Scan fills _want (already sorted by distance) then trims to 4.
        static void AfterSeneaL(object __instance, Player p)
        {
            LookupSenea();
            if (__instance == null || _want == null || _dist == null || _extra == null)
            {
                return;
            }

            IList want = _want.GetValue(__instance) as IList;
            IList dist = _dist.GetValue(__instance) as IList;
            if (want == null || dist == null || want.Count == 0)
            {
                return;
            }

            int extra = 0;
            try
            {
                extra = (int)_extra.GetValue(__instance);
            }
            catch (Exception)
            {
            }

            int total = want.Count + extra;
            int max = Mathf.Min(Max(), 4);
            Character hover = p != null ? p.GetHoverCreature() : null;
            if (hover != null && hover.IsBoss())
            {
                int at = IndexOf(want, hover);
                if (at > 0)
                {
                    object who = want[at];
                    object d = dist.Count > at ? dist[at] : null;
                    want.RemoveAt(at);
                    want.Insert(0, who);
                    if (d != null && dist.Count > at)
                    {
                        dist.RemoveAt(at);
                        dist.Insert(0, d);
                    }
                }
            }

            if (want.Count <= max && extra == 0)
            {
                return;
            }

            while (want.Count > max)
            {
                want.RemoveAt(want.Count - 1);
            }

            while (dist.Count > want.Count)
            {
                dist.RemoveAt(dist.Count - 1);
            }

            int hidden = total - want.Count;
            if (!CountOn())
            {
                hidden = 0;
            }

            _extra.SetValue(__instance, hidden);
            if (_primary != null && want.Count > 0)
            {
                _primary.SetValue(__instance, want[0]);
            }
        }

        static int IndexOf(IList list, Character who)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], who))
                {
                    return i;
                }
            }

            return -1;
        }

        static void AfterVanilla(EnemyHud __instance, Player player)
        {
            if (__instance == null)
            {
                return;
            }

            int max = Max();
            LookupHud();
            if (_huds == null || _hudCharacter == null || _hudGui == null)
            {
                return;
            }

            IDictionary map = _huds.GetValue(__instance) as IDictionary;
            if (map == null || map.Count == 0)
            {
                return;
            }

            HudPool.Clear();
            Character hover = player != null ? player.GetHoverCreature() : null;
            Vector3 here = player != null ? player.transform.position : Vector3.zero;
            foreach (DictionaryEntry pair in map)
            {
                object data = pair.Value;
                if (data == null)
                {
                    continue;
                }

                Character character = _hudCharacter.GetValue(data) as Character;
                if (character == null || !character.IsBoss())
                {
                    continue;
                }

                GameObject gui = _hudGui.GetValue(data) as GameObject;
                if (gui == null)
                {
                    continue;
                }

                HudPool.Add(new HudEntry
                {
                    Character = character,
                    Gui = gui,
                    Name = _hudName != null ? _hudName.GetValue(data) as TMP_Text : null,
                    Rank = RankOf(character, hover, here)
                });
            }

            int n = HudPool.Count;
            if (n <= max)
            {
                return;
            }

            HudPool.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            int extra = n - max;
            bool showCount = CountOn();
            for (int i = 0; i < n; i++)
            {
                HudEntry e = HudPool[i];
                bool keep = i < max;
                if (e.Gui.activeSelf != keep)
                {
                    e.Gui.SetActive(keep);
                }

                if (keep && showCount && extra > 0 && e.Name != null && i == 0)
                {
                    e.Name.text = e.Name.text + "  (+" + extra + ")";
                }
            }
        }

        static float RankOf(Character character, Character hover, Vector3 here)
        {
            if (character == hover)
            {
                return 0f;
            }

            float dist = Vector3.Distance(character.transform.position, here);
            BaseAI ai = character.GetBaseAI();
            if (ai != null && ai.IsAlerted())
            {
                return 1000f + dist;
            }

            return 2000f + dist;
        }

        static void LookupHud()
        {
            if (_lookedHud)
            {
                return;
            }

            _lookedHud = true;
            _huds = AccessTools.Field(typeof(EnemyHud), "m_huds");
            Type hudData = AccessTools.Inner(typeof(EnemyHud), "HudData");
            if (hudData == null)
            {
                return;
            }

            _hudCharacter = AccessTools.Field(hudData, "m_character");
            _hudGui = AccessTools.Field(hudData, "m_gui");
            _hudName = AccessTools.Field(hudData, "m_name");
        }

        static void LookupSenea()
        {
            if (_lookedSenea)
            {
                return;
            }

            _lookedSenea = true;
            Type view = AccessTools.TypeByName("SeneaLUI.Hud.BossBarView");
            if (view == null)
            {
                return;
            }

            _want = AccessTools.Field(view, "_want");
            _dist = AccessTools.Field(view, "_dist");
            _extra = AccessTools.Field(view, "_extra");
            _primary = AccessTools.Field(view, "_primary");
        }
    }
}
