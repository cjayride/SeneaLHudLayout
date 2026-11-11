using UnityEngine;

namespace SeneaLHudLayout
{
    static class BossStars
    {
        static Transform _bosses;

        // 1.5.52 FPS: FindNamed below walks the whole SeneaLUI_Hud subtree, and when there is no
        // "bosses" node (no boss bars earned yet - the common case) the cache stays null and the
        // walk re-ran EVERY FRAME while HideBossStars is on. Negative-cache: retry on a 2s gate.
        static float _nextScan;

        public static void Tick()
        {
            if (SeneaLHudLayoutPlugin.HideBossStars == null || !SeneaLHudLayoutPlugin.HideBossStars.Value)
            {
                return;
            }

            if (_bosses == null)
            {
                if (Time.unscaledTime < _nextScan)
                {
                    return;
                }

                _nextScan = Time.unscaledTime + 2f;
                Hud hud = Hud.instance;
                Transform root = hud != null && hud.m_rootObject != null
                    ? hud.m_rootObject.transform.Find("SeneaLUI_Hud")
                    : null;
                _bosses = FindNamed(root, "bosses");
                if (_bosses == null)
                {
                    return;
                }
            }

            for (int i = 0; i < _bosses.childCount; i++)
            {
                HideOn(_bosses.GetChild(i));
            }
        }

        static Transform FindNamed(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        static void HideOn(Transform card)
        {
            for (int i = 0; i < card.childCount; i++)
            {
                Transform child = card.GetChild(i);
                HideOn(child);
                string name = child.name;
                bool star = name == "starchip"
                    || name == "starnum"
                    || (name.Length > 4 && name.StartsWith("star") && char.IsDigit(name[4]));
                if (star && child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}
