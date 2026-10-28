
using System.Collections.Generic;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class EpicLootAbilitiesLayout
    {
        const string AbilitiesPath = "Abilities/fx/vitals";

        static readonly Dictionary<int, Vector2> BasePositions =
            new Dictionary<int, Vector2>();

        static readonly Dictionary<int, RectTransform> Tracked =
            new Dictionary<int, RectTransform>();

        static readonly List<RectTransform> Abilities =
            new List<RectTransform>();

        public static void Apply(RectTransform root)
        {
            if (root == null)
                return;

            Transform vitals = root.Find(AbilitiesPath);
            if (vitals == null)
                return;

            Abilities.Clear();

            for (int i = 0; i < 3; i++)
            {
                Transform ability = vitals.Find("ability" + i);

                if (!(ability is RectTransform rt))
                    continue;

                if (!rt.gameObject.activeInHierarchy)
                    continue;

                Remember(rt);
                Abilities.Add(rt);
            }

            if (Abilities.Count == 0)
                return;

            RectTransform reference = FindGuardianPowerReference(root);

            if (reference == null)
                reference = FindFoodReference(root);

            if (reference == null)
                return;

            float gap = SeneaLHudLayoutPlugin.EpicLootAbilitiesOffsetX.Value;
            float offsetY = SeneaLHudLayoutPlugin.EpicLootAbilitiesOffsetY.Value;

            float nextX = HudLayout.EdgeX(reference, false) + gap;
            float centerY =
                (HudLayout.EdgeY(reference, true) +
                 HudLayout.EdgeY(reference, false)) * 0.5f + offsetY;

            foreach (RectTransform ability in Abilities)
            {
                Transform powerTransform = ability.Find("power");
                RectTransform icon = powerTransform as RectTransform;

                if (icon == null)
                    continue;

                float iconCenterX =
                    (HudLayout.EdgeX(icon, true) +
                     HudLayout.EdgeX(icon, false)) * 0.5f;

                float iconCenterY =
                    (HudLayout.EdgeY(icon, true) +
                     HudLayout.EdgeY(icon, false)) * 0.5f;

                float moveX = nextX - HudLayout.EdgeX(icon, true);
                float moveY = centerY - iconCenterY;

                ability.position += new Vector3(moveX, moveY, 0f);

                nextX = HudLayout.EdgeX(icon, false) + gap;
            }
        }

        static RectTransform FindGuardianPowerReference(RectTransform root)
        {
            Transform vitals = root.Find("GuardianPower/fx/vitals");

            if (vitals == null)
                return null;

            RectTransform rightmost = null;
            float rightEdge = float.NegativeInfinity;

            for (int i = 0; i < vitals.childCount; i++)
            {
                Transform child = vitals.GetChild(i);

                if (!child.gameObject.activeInHierarchy)
                    continue;

                if (child.name != "gp" &&
                    !(child.name.StartsWith("gp") && child.name.Length <= 3))
                    continue;

                RectTransform icon = child.Find("power") as RectTransform;

                if (icon == null)
                    icon = child as RectTransform;

                if (icon == null)
                    continue;

                float edge = HudLayout.EdgeX(icon, false);

                if (edge > rightEdge)
                {
                    rightEdge = edge;
                    rightmost = icon;
                }
            }

            return rightmost;
        }

        static RectTransform FindFoodReference(RectTransform root)
        {
            Transform food = root.Find("Food/fx/vitals/food");

            if (food == null)
                food = root.Find("Food/fx/vitals");

            return food as RectTransform;
        }

        static void Remember(RectTransform rt)
        {
            int id = rt.GetInstanceID();

            if (Tracked.TryGetValue(id, out RectTransform tracked)
                && tracked == rt)
                return;

            Tracked[id] = rt;
            BasePositions[id] = rt.anchoredPosition;
        }

        public static void Restore(RectTransform root)
        {
            foreach (KeyValuePair<int, RectTransform> entry in Tracked)
            {
                RectTransform rt = entry.Value;

                if (rt != null && BasePositions.TryGetValue(entry.Key, out Vector2 pos))
                    rt.anchoredPosition = pos;
            }

            Tracked.Clear();
            BasePositions.Clear();
            Abilities.Clear();
        }
    }
}
