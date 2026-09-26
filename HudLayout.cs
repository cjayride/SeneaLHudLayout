using System.Collections.Generic;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class HudLayout
    {
        const string ShiftName = "SeneaLHudLayout_Shift";

        static RectTransform _vitals;
        static Vector2 _vitalsBase;
        static bool _vitalsKnown;
        static bool _loggedReady;
        static bool _applied;
        static int _prune;

        static readonly Dictionary<int, Vector2> _bossBase = new Dictionary<int, Vector2>();
        static readonly List<int> _staleBosses = new List<int>();

        public static void Apply()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
            {
                return;
            }

            Transform seneal = hud.m_rootObject.transform.Find("SeneaLUI_Hud");
            if (seneal == null)
            {
                return;
            }

            var root = (RectTransform)seneal;
            RectTransform hotbarShift = PlaceHotbar(root);
            PlaceHealthFood(root, hotbarShift);
            PlaceMinimap(root);
            PlaceBossBars();
            PinCompass(root);
            PlaceCenterMessage(root);
            PowersLayout.Apply(hud, root);
            _applied = true;

            if (!_loggedReady)
            {
                _loggedReady = true;
                Debug.Log("[SeneaL HUD Layout] Positioning hotbar, compass, messages, and boss powers.");
            }
        }

        public static void Restore()
        {
            if (!_applied)
            {
                return;
            }

            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
            {
                return;
            }

            Transform seneal = hud.m_rootObject.transform.Find("SeneaLUI_Hud");
            if (seneal == null)
            {
                return;
            }

            var root = (RectTransform)seneal;
            Transform shift = root.Find("menuFade/Hotbar/" + ShiftName);
            if (shift is RectTransform hotbar)
            {
                hotbar.anchoredPosition = Vector2.zero;
            }

            if (_vitalsKnown && _vitals != null)
            {
                _vitals.anchoredPosition = _vitalsBase;
            }

            if (root.Find("mapFade/Minimap") is RectTransform minimap)
            {
                minimap.anchoredPosition = Vector2.zero;
            }

            RestoreBossBars();
            RestoreCenterMessage();
            PowersLayout.Restore(root);
            _applied = false;
        }

        static RectTransform PlaceHotbar(RectTransform root)
        {
            Transform layer = root.Find("menuFade/Hotbar");
            if (layer == null)
            {
                return null;
            }

            RectTransform shift = EnsureShift((RectTransform)layer);
            shift.anchoredPosition = Vector2.zero;

            if (!SeneaLHudLayoutPlugin.HotbarBottomLeft.Value
                && Mathf.Approximately(SeneaLHudLayoutPlugin.HotbarOffsetX.Value, 0f)
                && Mathf.Approximately(SeneaLHudLayoutPlugin.HotbarOffsetY.Value, 0f))
            {
                return shift;
            }

            if (SeneaLHudLayoutPlugin.HotbarBottomLeft.Value
                && TryChildrenVertical(shift, out float contentBottom, out _))
            {
                float screenBottom = EdgeY(root, bottom: true);
                float target = screenBottom + SeneaLHudLayoutPlugin.HotbarBottomMargin.Value;
                Move(shift, 0f, target - contentBottom);
            }

            Move(shift, SeneaLHudLayoutPlugin.HotbarOffsetX.Value, SeneaLHudLayoutPlugin.HotbarOffsetY.Value);
            return shift;
        }

        static void PlaceHealthFood(RectTransform root, RectTransform hotbarShift)
        {
            Transform vitalsT = root.Find("Health/vitals");
            if (vitalsT == null)
            {
                return;
            }

            var vitals = (RectTransform)vitalsT;
            if (_vitals != vitals)
            {
                _vitals = vitals;
                _vitalsBase = vitals.anchoredPosition;
                _vitalsKnown = true;
            }

            vitals.anchoredPosition = _vitalsBase;

            float extra = SeneaLHudLayoutPlugin.HealthFoodOffsetY.Value;
            float hotbarTop = 0f;
            bool lift = SeneaLHudLayoutPlugin.LiftHealthAboveHotbar.Value
                && SeneaLHudLayoutPlugin.HotbarBottomLeft.Value
                && hotbarShift != null
                && TryChildrenVertical(hotbarShift, out _, out hotbarTop);

            if (lift)
            {
                float vitalsBottom = EdgeY(vitals, bottom: true);
                Move(vitals, 0f, (hotbarTop + extra) - vitalsBottom);
            }
            else if (!Mathf.Approximately(extra, 0f))
            {
                Move(vitals, 0f, extra);
            }
        }

        static void PlaceMinimap(RectTransform root)
        {
            Transform layer = root.Find("mapFade/Minimap");
            if (layer == null)
            {
                return;
            }

            var minimap = (RectTransform)layer;
            minimap.anchoredPosition = Vector2.zero;
            Move(minimap,
                SeneaLHudLayoutPlugin.MinimapOffsetX.Value,
                SeneaLHudLayoutPlugin.MinimapOffsetY.Value);
        }

        static void PlaceBossBars()
        {
            EnemyHud enemyHud = EnemyHud.instance;
            if (enemyHud == null || enemyHud.m_hudRoot == null)
            {
                return;
            }

            float offset = SeneaLHudLayoutPlugin.BossOffsetY.Value;
            Transform hudRoot = enemyHud.m_hudRoot.transform;
            var seen = new HashSet<int>();

            for (int i = 0; i < hudRoot.childCount; i++)
            {
                Transform child = hudRoot.GetChild(i);
                if (child.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var bar = child as RectTransform;
                if (bar == null)
                {
                    continue;
                }

                int id = bar.GetInstanceID();
                seen.Add(id);
                if (!_bossBase.ContainsKey(id))
                {
                    _bossBase[id] = bar.anchoredPosition;
                }

                bar.anchoredPosition = _bossBase[id];
                if (!Mathf.Approximately(offset, 0f))
                {
                    Move(bar, 0f, offset);
                }
            }

            if (++_prune < 120)
            {
                return;
            }

            _prune = 0;
            _staleBosses.Clear();
            foreach (int id in _bossBase.Keys)
            {
                if (!seen.Contains(id))
                {
                    _staleBosses.Add(id);
                }
            }

            for (int i = 0; i < _staleBosses.Count; i++)
            {
                _bossBase.Remove(_staleBosses[i]);
            }
        }

        static void RestoreBossBars()
        {
            EnemyHud enemyHud = EnemyHud.instance;
            if (enemyHud == null || enemyHud.m_hudRoot == null)
            {
                _bossBase.Clear();
                return;
            }

            Transform hudRoot = enemyHud.m_hudRoot.transform;
            for (int i = 0; i < hudRoot.childCount; i++)
            {
                if (!(hudRoot.GetChild(i) is RectTransform bar))
                {
                    continue;
                }

                if (_bossBase.TryGetValue(bar.GetInstanceID(), out Vector2 basePos))
                {
                    bar.anchoredPosition = basePos;
                }
            }

            _bossBase.Clear();
        }

        static RectTransform EnsureShift(RectTransform layer)
        {
            Transform existing = layer.Find(ShiftName);
            if (existing != null)
            {
                return (RectTransform)existing;
            }

            var go = new GameObject(ShiftName, typeof(RectTransform));
            var shift = go.GetComponent<RectTransform>();
            shift.SetParent(layer, false);
            shift.anchorMin = Vector2.zero;
            shift.anchorMax = Vector2.one;
            shift.offsetMin = Vector2.zero;
            shift.offsetMax = Vector2.zero;
            shift.pivot = new Vector2(0.5f, 0.5f);

            var children = new List<Transform>(layer.childCount);
            for (int i = 0; i < layer.childCount; i++)
            {
                children.Add(layer.GetChild(i));
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] != shift)
                {
                    children[i].SetParent(shift, false);
                }
            }

            return shift;
        }

        static RectTransform _banner;
        static Vector2 _bannerBase;
        static bool _bannerKnown;

        static void PinCompass(RectTransform root)
        {
            if (!(root.Find("menuFade/Compass/compass") is RectTransform compass))
            {
                return;
            }

            if (!(compass.parent is RectTransform parent))
            {
                return;
            }

            float desiredTop = EdgeY(parent, bottom: false) - SeneaLHudLayoutPlugin.CompassTop.Value;
            Move(compass, 0f, desiredTop - EdgeY(compass, bottom: false));
        }

        static void PlaceCenterMessage(RectTransform root)
        {
            if (!(root.Find("menuFade/Notifications2/banner") is RectTransform banner))
            {
                return;
            }

            if (_banner != banner)
            {
                _banner = banner;
                _bannerBase = banner.anchoredPosition;
                _bannerKnown = true;
            }

            float ceiling = float.PositiveInfinity;
            if (root.Find("menuFade/Compass/compass") is RectTransform compass
                && compass.gameObject.activeInHierarchy)
            {
                ceiling = EdgeY(compass, bottom: true);
            }

            if (TryBossBottom(out float bossBottom))
            {
                ceiling = Mathf.Min(ceiling, bossBottom);
            }

            if (float.IsPositiveInfinity(ceiling))
            {
                return;
            }

            float targetTop = ceiling - SeneaLHudLayoutPlugin.CenterMessageGap.Value;
            Move(banner, 0f, targetTop - EdgeY(banner, bottom: false));
        }

        static void RestoreCenterMessage()
        {
            if (_bannerKnown && _banner != null)
            {
                _banner.anchoredPosition = _bannerBase;
            }
        }

        static bool TryBossBottom(out float bottom)
        {
            bottom = float.PositiveInfinity;
            EnemyHud enemyHud = EnemyHud.instance;
            if (enemyHud == null || enemyHud.m_hudRoot == null)
            {
                return false;
            }

            Transform hudRoot = enemyHud.m_hudRoot.transform;
            bool any = false;
            for (int i = 0; i < hudRoot.childCount; i++)
            {
                Transform child = hudRoot.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (child.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (!(child is RectTransform bar))
                {
                    continue;
                }

                float y = EdgeY(bar, bottom: true);
                if (y < bottom)
                {
                    bottom = y;
                }

                any = true;
            }

            return any;
        }

        internal static void Move(RectTransform rt, float pixelsX, float pixelsY)
        {
            if (rt.parent == null)
            {
                return;
            }

            Vector3 local = rt.parent.InverseTransformVector(new Vector3(pixelsX, pixelsY, 0f));
            Vector2 pos = rt.anchoredPosition;
            pos.x += local.x;
            pos.y += local.y;
            rt.anchoredPosition = pos;
        }

        internal static float EdgeX(RectTransform rt, bool left)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float x = corners[0].x;
            for (int i = 1; i < 4; i++)
            {
                if (left ? corners[i].x < x : corners[i].x > x)
                {
                    x = corners[i].x;
                }
            }

            return x;
        }

        internal static float EdgeY(RectTransform rt, bool bottom)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float y = corners[0].y;
            for (int i = 1; i < 4; i++)
            {
                if (bottom ? corners[i].y < y : corners[i].y > y)
                {
                    y = corners[i].y;
                }
            }

            return y;
        }

        static bool TryChildrenVertical(RectTransform parent, out float minY, out float maxY)
        {
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;
            bool any = false;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float childMin = EdgeY(child, bottom: true);
                float childMax = EdgeY(child, bottom: false);
                if (childMin < minY)
                {
                    minY = childMin;
                }

                if (childMax > maxY)
                {
                    maxY = childMax;
                }

                any = true;
            }

            return any;
        }
    }
}
