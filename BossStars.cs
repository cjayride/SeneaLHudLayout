using UnityEngine;

namespace SeneaLHudLayout
{
    static class BossStars
    {
        public static void Tick()
        {
            if (SeneaLHudLayoutPlugin.HideBossStars == null || !SeneaLHudLayoutPlugin.HideBossStars.Value)
            {
                return;
            }

            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
            {
                return;
            }

            Transform root = hud.m_rootObject.transform.Find("SeneaLUI_Hud");
            Transform bosses = FindNamed(root, "bosses");
            if (bosses == null)
            {
                return;
            }

            for (int i = 0; i < bosses.childCount; i++)
            {
                HideOn(bosses.GetChild(i));
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
