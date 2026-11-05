using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace SeneaLHudLayout
{
    static class StatsTalentFocus
    {
        const string TalentTreeGuid = "M2Valheim.TalentTree";

        static bool _checked;
        static bool _available;
        static MethodInfo _getRuntime;
        static PropertyInfo _registryAll;
        static PropertyInfo _sectorAll;
        static MethodInfo _costFor;
        static PropertyInfo _stateRanks;
        static PropertyInfo _sectorDisplay;
        static PropertyInfo _defSectorId;
        static PropertyInfo _defTier;
        static PropertyInfo _defId;
        public static string Describe(Player player)
        {
            if (player == null || !Ensure())
            {
                return null;
            }

            try
            {
                object runtime = _getRuntime.Invoke(null, new object[] { player });
                if (runtime == null)
                {
                    return null;
                }

                object state = runtime.GetType().GetProperty("State")?.GetValue(runtime, null);
                if (state == null || _stateRanks == null)
                {
                    return null;
                }

                object ranksObj = _stateRanks.GetValue(state, null);
                if (!(ranksObj is IDictionary ranks) || ranks.Count == 0)
                {
                    return null;
                }

                var sectorPoints = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (object def in Enumerate(_registryAll.GetValue(null, null)))
                {
                    if (def == null)
                    {
                        continue;
                    }

                    string id = _defId?.GetValue(def, null) as string;
                    string sectorId = _defSectorId?.GetValue(def, null) as string;
                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(sectorId))
                    {
                        continue;
                    }

                    if (!ranks.Contains(id))
                    {
                        continue;
                    }

                    int rank = Convert.ToInt32(ranks[id]);
                    if (rank <= 0)
                    {
                        continue;
                    }

                    object tier = _defTier?.GetValue(def, null);
                    int cost = tier != null ? (int)_costFor.Invoke(null, new[] { tier }) : 1;
                    if (!sectorPoints.ContainsKey(sectorId))
                    {
                        sectorPoints[sectorId] = 0;
                    }

                    sectorPoints[sectorId] += rank * cost;
                }

                if (sectorPoints.Count == 0)
                {
                    return null;
                }

                string bestId = null;
                int best = -1;
                foreach (KeyValuePair<string, int> pair in sectorPoints)
                {
                    if (pair.Value > best)
                    {
                        best = pair.Value;
                        bestId = pair.Key;
                    }
                }

                if (bestId == null || best <= 0)
                {
                    return null;
                }

                string label = SectorName(bestId);
                int total = 0;
                foreach (int v in sectorPoints.Values)
                {
                    total += v;
                }

                if (sectorPoints.Count == 1 || best >= total * 0.45f)
                {
                    return label + " spec";
                }

                return label + " focus";
            }
            catch (Exception)
            {
                return null;
            }
        }

        static string SectorName(string sectorId)
        {
            if (_sectorAll == null)
            {
                return sectorId;
            }

            foreach (object sector in Enumerate(_sectorAll.GetValue(null, null)))
            {
                if (sector == null)
                {
                    continue;
                }

                PropertyInfo idProp = sector.GetType().GetProperty("Id");
                if (!string.Equals(idProp?.GetValue(sector, null) as string, sectorId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string display = _sectorDisplay?.GetValue(sector, null) as string;
                if (!string.IsNullOrEmpty(display))
                {
                    return display;
                }
            }

            return sectorId;
        }

        static IEnumerable Enumerate(object collection)
        {
            if (collection is IEnumerable e)
            {
                return e;
            }

            return Array.Empty<object>();
        }

        static bool Ensure()
        {
            if (_checked)
            {
                return _available;
            }

            _checked = true;
            if (!Chainloader.PluginInfos.ContainsKey(TalentTreeGuid))
            {
                return false;
            }

            Type runtimeService = ModTypes.Find("TalentTree", "Core.TalentRuntimeService");
            Type registry = ModTypes.Find("TalentTree", "Core.TalentRegistry");
            Type sectors = ModTypes.Find("TalentTree", "Core.SectorRegistry");
            Type budget = ModTypes.Find("TalentTree", "Core.TalentPointBudgetService");
            Type state = ModTypes.Find("TalentTree", "Core.TalentState");
            Type def = ModTypes.Find("TalentTree", "Core.TalentDefinition");
            if (runtimeService == null || registry == null || budget == null || state == null || def == null)
            {
                return false;
            }

            _getRuntime = AccessTools.Method(runtimeService, "Get", new[] { typeof(Player) });
            _registryAll = AccessTools.Property(registry, "All");
            _sectorAll = sectors != null ? AccessTools.Property(sectors, "All") : null;
            _costFor = AccessTools.Method(budget, "CostFor");
            _stateRanks = AccessTools.Property(state, "Ranks");
            _defId = AccessTools.Property(def, "Id");
            _defSectorId = AccessTools.Property(def, "SectorId");
            _defTier = AccessTools.Property(def, "Tier");
            Type sectorDef = ModTypes.Find("TalentTree", "Core.SectorDefinition");
            _sectorDisplay = sectorDef != null ? AccessTools.Property(sectorDef, "DisplayName") : null;

            _available = _getRuntime != null && _registryAll != null && _costFor != null && _stateRanks != null;
            return _available;
        }
    }
}
