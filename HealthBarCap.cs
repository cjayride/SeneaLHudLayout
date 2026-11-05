using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SeneaL can grow the player health bar with max HP. Cap the visual height so
    /// 600+ HP does not stretch the HUD; fill still tracks current / max.
    /// </summary>
    static class HealthBarCap
    {
        public static void Apply(RectTransform senealRoot)
        {
            if (senealRoot == null || SeneaLHudLayoutPlugin.HealthBarMaxHeight == null)
            {
                return;
            }

            float cap = SeneaLHudLayoutPlugin.HealthBarMaxHeight.Value;
            if (cap < 40f)
            {
                return;
            }

            Transform health = senealRoot.Find("Health");
            if (health == null)
            {
                return;
            }

            CapNode(health as RectTransform, cap, 0);
        }

        static void CapNode(RectTransform rt, float cap, int depth)
        {
            if (rt == null || depth > 8)
            {
                return;
            }

            string name = rt.name;
            bool likelyBar = name == "Health" || name == "fx" || name == "vitals"
                || name.IndexOf("hp", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("health", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("gauge", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("bar", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name == "fill" || name == "back";

            if (likelyBar)
            {
                LayoutElement le = rt.GetComponent<LayoutElement>();
                if (le != null)
                {
                    if (le.preferredHeight > cap)
                    {
                        le.preferredHeight = cap;
                    }

                    if (le.minHeight > cap)
                    {
                        le.minHeight = cap;
                    }
                }

                float h = rt.rect.height;
                if (h > cap + 0.5f)
                {
                    Vector2 size = rt.sizeDelta;
                    size.y -= h - cap;
                    rt.sizeDelta = size;
                }
            }

            for (int i = 0; i < rt.childCount; i++)
            {
                CapNode(rt.GetChild(i) as RectTransform, cap, depth + 1);
            }
        }
    }
}
