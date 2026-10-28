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
        static RectTransform _food;
        static Vector2 _foodBase;
        static bool _foodKnown;

        public static void Apply()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
                return;

            Transform seneal = hud.m_rootObject.transform.Find("SeneaLUI_Hud");
            if (seneal == null)
            {
                Debug.LogWarning("[SeneaL HUD Layout] SeneaLUI_Hud not found - waiting for SeneaL UI to initialize");
                return;
            }

            var root = (RectTransform)seneal;

            RectTransform hotbarShift = PlaceHotbar(root);
            PlaceHealthFood(root, hotbarShift);
            PlaceMinimap(root);
            PlaceBossBars();
            PinCompass(root);
            ClearBossFromCompass(root);
            LiftBossWithoutCompass(root);
            ClearRaidFromBoss(root);
            PlaceCenterMessage(root);
            PowersLayout.Apply(hud, root);
            EpicLootAbilitiesLayout.Apply(root);
            _applied = true;
        }

        public static void Restore()
        {
            if (!_applied)
                return;

            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
                return;

            Transform seneal = hud.m_rootObject.transform.Find("SeneaLUI_Hud");
            if (seneal == null)
                return;

            var root = (RectTransform)seneal;
            Transform menu = root.Find("menuFade");
            Transform shiftTransform = menu != null
                ? menu.Find("SeneaLHudLayout_HotbarShift")
                : null;

            if (shiftTransform is RectTransform shift)
            {
                shift.anchoredPosition = Vector2.zero;

                string[] names =
                {
                    "Hotbar",
                    "HotbarFood",
                    "HotbarActions",
                    "HotbarQuiver"
                };

                foreach (string name in names)
                {
                    Transform group = shift.Find(name);
                    if (group != null && menu != null)
                        group.SetParent(menu, true);
                }

                Object.Destroy(shift.gameObject);
            }

            if (_vitalsKnown && _vitals != null)
                _vitals.anchoredPosition = _vitalsBase;

            if (_foodKnown && _food != null)
                _food.anchoredPosition = _foodBase;

            if (root.Find("mapFade/Minimap") is RectTransform minimap)
                minimap.anchoredPosition = Vector2.zero;

            RestoreBossBars();
            RestoreCenterMessage();
            PowersLayout.Restore(root);
            EpicLootAbilitiesLayout.Restore(root);
            _applied = false;
        }

        static void LogHotbarTree(Transform parent, int depth = 0)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child is RectTransform rt)
                {
                    Debug.Log(
                        $"[Hotbar Tree] {new string(' ', depth * 2)}{child.name} " +
                        $"active={child.gameObject.activeInHierarchy} " +
                        $"pos={rt.anchoredPosition} rect={rt.rect} " +
                        $"bottom={EdgeY(rt, true):F1} left={EdgeX(rt, true):F1}");
                }

                LogHotbarTree(child, depth + 1);
            }
        }

        static void FindHotbarBounds(
            Transform parent,
            RectTransform root,
            ref float bottom,
            ref float left)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (!child.gameObject.activeInHierarchy)
                    continue;

                if (child is RectTransform rt)
                {
                    bool fullScreen =
                        rt.rect.width >= root.rect.width * 0.8f &&
                        rt.rect.height >= root.rect.height * 0.8f;

                    if (!fullScreen)
                    {
                        bottom = Mathf.Min(bottom, EdgeY(rt, true));
                        left = Mathf.Min(left, EdgeX(rt, true));
                    }
                }

                FindHotbarBounds(child, root, ref bottom, ref left);
            }
        }

        static RectTransform PlaceHotbar(RectTransform root)
        {
            Transform menu = root.Find("menuFade");
            if (!(menu is RectTransform menuRect))
                return null;

            string[] names =
            {
                "Hotbar",
                "HotbarFood",
                "HotbarActions",
                "HotbarQuiver"
            };

            Transform existing = menu.Find("SeneaLHudLayout_HotbarShift");
            RectTransform shift = existing as RectTransform;

            if (shift == null)
            {
                GameObject go = new GameObject(
                    "SeneaLHudLayout_HotbarShift",
                    typeof(RectTransform));

                shift = go.GetComponent<RectTransform>();
                shift.SetParent(menu, false);
                shift.anchorMin = Vector2.zero;
                shift.anchorMax = Vector2.one;
                shift.offsetMin = Vector2.zero;
                shift.offsetMax = Vector2.zero;
            }

            foreach (string name in names)
            {
                Transform group = menu.Find(name);
                if (group != null && group.parent != shift)
                    group.SetParent(shift, true);
            }

            shift.anchoredPosition = Vector2.zero;

            if (SeneaLHudLayoutPlugin.HotbarBottomLeft.Value)
            {
                float minY = float.PositiveInfinity;
                float unusedMinX = float.PositiveInfinity;

                FindHotbarBounds(shift, root, ref minY, ref unusedMinX);

                if (!float.IsPositiveInfinity(minY))
                {
                    Vector3 menuBottomLeft = menuRect.TransformPoint(
                        new Vector3(menuRect.rect.xMin, menuRect.rect.yMin, 0f));

                    float targetY = menuBottomLeft.y
                        + SeneaLHudLayoutPlugin.HotbarBottomMargin.Value
                        + SeneaLHudLayoutPlugin.HotbarOffsetY.Value;

                    Move(shift,
                        SeneaLHudLayoutPlugin.HotbarOffsetX.Value,
                        targetY - minY);
                }
            }
            else
            {
                Move(shift,
                    SeneaLHudLayoutPlugin.HotbarOffsetX.Value,
                    SeneaLHudLayoutPlugin.HotbarOffsetY.Value);
            }

            return shift;
        }

        static void PlaceHealthFood(RectTransform root, RectTransform hotbarShift)
        {
            Transform vitalsT = root.Find("Health/fx");
            Transform foodT = root.Find("Food/fx/vitals/food");

            if (!(vitalsT is RectTransform vitals))
                return;

            RectTransform food = foodT as RectTransform;

            if (_vitals != vitals)
            {
                _vitals = vitals;
                _vitalsBase = vitals.anchoredPosition;
                _vitalsKnown = true;
            }

            if (food != null && _food != food)
            {
                _food = food;
                _foodBase = food.anchoredPosition;
                _foodKnown = true;
            }

            vitals.anchoredPosition = _vitalsBase;

            if (food != null && _foodKnown)
                food.anchoredPosition = _foodBase;

            if (SeneaLHudLayoutPlugin.SyncVitalsSize.Value)
                ApplyVitalsSync(root);

            float extra = SeneaLHudLayoutPlugin.HotbarBottomLeft.Value
                ? SeneaLHudLayoutPlugin.HealthFoodOffsetY.Value
                : 0f;
            float moveY = extra;

            if (SeneaLHudLayoutPlugin.HotbarBottomLeft.Value
                && SeneaLHudLayoutPlugin.LiftHealthAboveHotbar.Value
                && hotbarShift != null
                && TryChildrenVertical(hotbarShift, out _, out float hotbarTop))
            {
                moveY = hotbarTop + extra - EdgeY(vitals, bottom: true);
            }

            if (!Mathf.Approximately(moveY, 0f))
                Move(vitals, 0f, moveY);

            if (food != null && _foodKnown
                && SeneaLHudLayoutPlugin.HotbarBottomLeft.Value
                && SeneaLHudLayoutPlugin.LiftHealthAboveHotbar.Value)
            {
                Move(food, SeneaLHudLayoutPlugin.HealthFoodOffsetX.Value, moveY);
            }
        }

        static void PlaceMinimap(RectTransform root)
        {
            Transform layer = root.Find("mapFade/Minimap");
            if (layer == null)
                return;

            var minimap = (RectTransform)layer;
            minimap.anchoredPosition = Vector2.zero;
            Move(minimap,
                SeneaLHudLayoutPlugin.MinimapOffsetX.Value,
                SeneaLHudLayoutPlugin.MinimapOffsetY.Value);

            if (SeneaLHudLayoutPlugin.HideMinimapStats.Value)
                HideMinimapStats(root);
            else
            {
                Transform info = root.Find("mapFade/WorldInfo/fx/info");
                if (info != null)
                    info.gameObject.SetActive(true);
            }
        }

        static void PlaceBossBars()
        {
            EnemyHud enemyHud = EnemyHud.instance;
            if (enemyHud == null || enemyHud.m_hudRoot == null)
                return;

            float offset = SeneaLHudLayoutPlugin.BossOffsetY.Value;
            Transform hudRoot = enemyHud.m_hudRoot.transform;
            var seen = new HashSet<int>();

            for (int i = 0; i < hudRoot.childCount; i++)
            {
                Transform child = hudRoot.GetChild(i);
                if (child.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var bar = child as RectTransform;
                if (bar == null)
                    continue;

                int id = bar.GetInstanceID();
                seen.Add(id);

                if (!_bossBase.ContainsKey(id))
                    _bossBase[id] = bar.anchoredPosition;

                bar.anchoredPosition = _bossBase[id];

                if (!Mathf.Approximately(offset, 0f))
                    Move(bar, 0f, offset);
            }

            if (++_prune < 120)
                return;

            _prune = 0;
            _staleBosses.Clear();

            foreach (int id in _bossBase.Keys)
            {
                if (!seen.Contains(id))
                    _staleBosses.Add(id);
            }

            for (int i = 0; i < _staleBosses.Count; i++)
                _bossBase.Remove(_staleBosses[i]);
        }

        static void ClearBossFromCompass(RectTransform root)
        {
            if (!(root.Find("menuFade/Compass/fx/compass") is RectTransform compass)
                || !compass.gameObject.activeInHierarchy)
                return;

            EnemyHud enemyHud = EnemyHud.instance;
            if (enemyHud == null || enemyHud.m_hudRoot == null)
                return;

            float limit = EdgeY(compass, bottom: true) -
                SeneaLHudLayoutPlugin.CenterMessageGap.Value;

            Transform hudRoot = enemyHud.m_hudRoot.transform;
            for (int i = 0; i < hudRoot.childCount; i++)
            {
                Transform child = hudRoot.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                    continue;

                if (child.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (!(child is RectTransform bar))
                    continue;

                float top = EdgeY(bar, bottom: false);
                if (top > limit)
                    Move(bar, 0f, limit - top);
            }
        }

        static float _compassTopY;
        static bool _compassTopKnown;

        static void LiftBossWithoutCompass(RectTransform root)
        {
            if (!(root.Find("menuFade/Compass/fx/compass") is RectTransform compass))
                return;

            if (compass.gameObject.activeInHierarchy)
            {
                _compassTopY = EdgeY(compass, false);
                _compassTopKnown = true;
                return;
            }

            if (!_compassTopKnown)
                return;

            float desiredTop = _compassTopY;
            EnemyHud enemyHud = EnemyHud.instance;

            if (enemyHud == null || enemyHud.m_hudRoot == null)
                return;

            Transform hudRoot = enemyHud.m_hudRoot.transform;

            for (int i = 0; i < hudRoot.childCount; i++)
            {
                Transform child = hudRoot.GetChild(i);

                if (!child.gameObject.activeInHierarchy ||
                    child.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) < 0 ||
                    !(child is RectTransform bar))
                    continue;

                float top = EdgeY(bar, false);

                if (top < desiredTop - 0.5f)
                    Move(bar, 0f, desiredTop - top);
            }
        }

        static void ClearRaidFromBoss(RectTransform root)
        {
            if (!(root.Find("menuFade/Compass/event") is RectTransform raid)
                || !raid.gameObject.activeInHierarchy
                || !TryBossBottom(out float bossBottom))
                return;

            float top = EdgeY(raid, bottom: false);
            float limit = bossBottom - 8f;

            if (top > limit)
                Move(raid, 0f, limit - top);
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
                    continue;

                if (_bossBase.TryGetValue(bar.GetInstanceID(), out Vector2 basePos))
                    bar.anchoredPosition = basePos;
            }

            _bossBase.Clear();
        }

        static RectTransform EnsureShift(RectTransform layer)
        {
            Transform existing = layer.Find(ShiftName);
            if (existing != null)
                return (RectTransform)existing;

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
                children.Add(layer.GetChild(i));

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] != shift)
                    children[i].SetParent(shift, false);
            }

            return shift;
        }

        static RectTransform _banner;
        static Vector2 _bannerBase;
        static bool _bannerKnown;

        static void PinCompass(RectTransform root)
        {
            if (!(root.Find("menuFade/Compass/fx/compass") is RectTransform compass))
                return;

            if (!(compass.parent is RectTransform parent))
                return;

            float desiredTop = EdgeY(parent, false) -
                SeneaLHudLayoutPlugin.CompassTop.Value;

            Move(compass, 0f, desiredTop - EdgeY(compass, false));
        }

        static float _lastBossBottom;
        static bool _lastBossBottomKnown;

        static void PlaceCenterMessage(RectTransform root)
        {
            if (!(root.Find("menuFade/Notifications2/banner") is RectTransform banner))
                return;

            if (_banner != banner)
            {
                _banner = banner;
                _bannerBase = banner.anchoredPosition;
                _bannerKnown = true;
            }

            banner.anchoredPosition = _bannerBase;

            float scale = SeneaLHudLayoutPlugin.CenterMessageScale != null
                ? SeneaLHudLayoutPlugin.CenterMessageScale.Value
                : 1f;

            banner.localScale = new Vector3(scale, scale, 1f);

            bool bossVisible = TryBossBottom(out float bossBottom);
            RectTransform reference = null;

            if (!bossVisible)
            {
                reference = root.Find("menuFade/Compass/fx/compass") as RectTransform;

                if (reference == null || !reference.gameObject.activeInHierarchy)
                    return;
            }

            if (!(banner.parent is RectTransform parent))
                return;

            float gap = SeneaLHudLayoutPlugin.CenterMessageGap.Value;

            float referenceBottom = bossVisible
                ? bossBottom
                : EdgeY(reference, true);

            float desiredTop = referenceBottom -
                parent.TransformVector(new Vector3(0f, gap, 0f)).y;

            float worldDelta = desiredTop - EdgeY(banner, false);

            Vector3 localDelta = parent.InverseTransformVector(
                new Vector3(0f, worldDelta, 0f));

            banner.anchoredPosition = _bannerBase +
                new Vector2(localDelta.x, localDelta.y);
        }

        static bool TryBossBottom(out float bottom)
        {
            bottom = float.PositiveInfinity;

            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
            {
                _lastBossBottomKnown = false;
                return false;
            }

            Transform root = hud.m_rootObject.transform.Find("SeneaLUI_Hud");
            Transform bosses = root != null
                ? root.Find("menuFade/BossBars/fx/bosses")
                : null;

            if (bosses == null)
            {
                _lastBossBottomKnown = false;
                return false;
            }

            bool found = false;
            bool allFullyVisible = true;

            for (int i = 0; i < bosses.childCount; i++)
            {
                Transform child = bosses.GetChild(i);

                if (!child.gameObject.activeInHierarchy ||
                    !(child is RectTransform bar) ||
                    bar.rect.height <= 0f)
                    continue;

                float alpha = 1f;
                Transform current = child;

                while (current != null)
                {
                    CanvasGroup group = current.GetComponent<CanvasGroup>();

                    if (group != null)
                        alpha *= group.alpha;

                    if (current == root)
                        break;

                    current = current.parent;
                }

                if (alpha <= 0.01f)
                    continue;

                found = true;

                if (alpha < 0.95f)
                    allFullyVisible = false;

                float barBottom = EdgeY(bar, true);
                if (barBottom < bottom)
                    bottom = barBottom;
            }

            if (!found)
            {
                _lastBossBottomKnown = false;
                return false;
            }

            if (allFullyVisible || !_lastBossBottomKnown)
            {
                _lastBossBottom = bottom;
                _lastBossBottomKnown = true;
            }

            bottom = _lastBossBottom;
            return true;
        }

        static void RestoreCenterMessage()
        {
            if (_bannerKnown && _banner != null)
            {
                _banner.localScale = Vector3.one;
                _banner.anchoredPosition = _bannerBase;
            }
        }

        internal static void Move(RectTransform rt, float pixelsX, float pixelsY)
        {
            if (rt.parent == null)
                return;

            Vector3 local = rt.parent.InverseTransformVector(
                new Vector3(pixelsX, pixelsY, 0f));

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
                    x = corners[i].x;
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
                    y = corners[i].y;
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
                    continue;

                float childMin = EdgeY(child, bottom: true);
                float childMax = EdgeY(child, bottom: false);

                if (childMin < minY)
                    minY = childMin;

                if (childMax > maxY)
                    maxY = childMax;

                any = true;
            }

            return any;
        }

        static void HideMinimapStats(RectTransform root)
        {
            if (root == null)
                return;

            Transform info = root.Find("mapFade/WorldInfo/fx/info");
            if (info == null)
                return;

            bool hide = SeneaLHudLayoutPlugin.HideMinimapStats.Value;
            info.gameObject.SetActive(!hide);
        }

        static void HideMinimapStatDescendants(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string name = child.name.Replace("_", "").Replace("-", "").ToLowerInvariant();

                bool isWindStat =
                    name.Contains("wind") &&
                    (name.Contains("pill") || name.Contains("card") ||
                     name.Contains("server") || name == "wind");

                bool isDayStat =
                    (name.Contains("day") || name.Contains("serverday") ||
                     name.Contains("timecard") || name.Contains("timepill")) &&
                    (name.Contains("pill") || name.Contains("card") ||
                     name.Contains("server") || name == "day" ||
                     name.Contains("serverday"));

                if (isWindStat || isDayStat)
                {
                    if (child.gameObject.activeSelf)
                        child.gameObject.SetActive(false);

                    continue;
                }

                HideMinimapStatDescendants(child);
            }
        }

        static void LogUIHierarchy(Transform parent, int depth)
        {
            if (depth > 5)
                return;

            string indent = new string(' ', depth * 2);
            Debug.Log($"[SeneaL HUD Layout] {indent}{parent.name}");

            for (int i = 0; i < parent.childCount; i++)
                LogUIHierarchy(parent.GetChild(i), depth + 1);
        }

        // --- Vitals size + sync ---

        static bool _vitalsSizeInitialized;
        static RectTransform _staminaBar;
        static RectTransform _eitrBar;
        static RectTransform _adrenalineBar;
        static Vector3 _staminaBarBaseScale;

        static RectTransform _staminaRoot;
        static RectTransform _eitrRoot;
        static RectTransform _adrenalineRoot;
        static Vector3 _staminaRootBaseScale;
        static Vector3 _eitrRootBaseScale;
        static Vector3 _adrenalineRootBaseScale;
        static Vector2 _staminaRootBasePos;
        static Vector2 _eitrRootBasePos;
        static Vector2 _adrenalineRootBasePos;

        static void ApplyVitalsSync(RectTransform hudRoot)
        {
            if (hudRoot == null || !SeneaLHudLayoutPlugin.SyncVitalsSize.Value)
                return;

            var staminaBar = hudRoot.Find("Stamina/fx/stamina/bar") as RectTransform;
            var eitrBar = hudRoot.Find("Eitr/fx/eitr/bar") as RectTransform;
            var adrenalineBar = hudRoot.Find("Adrenaline/fx/adrenaline/bar") as RectTransform;

            var staminaRoot = hudRoot.Find("Stamina/fx/stamina") as RectTransform;
            var eitrRoot = hudRoot.Find("Eitr/fx/eitr") as RectTransform;
            var adrenalineRoot = hudRoot.Find("Adrenaline/fx/adrenaline") as RectTransform;

            if (staminaBar == null || eitrBar == null || adrenalineBar == null ||
                staminaRoot == null || eitrRoot == null || adrenalineRoot == null)
            {
                Debug.LogWarning("[Vitals] Could not find all required vitals nodes.");
                return;
            }

            if (!_vitalsSizeInitialized
                || _staminaBar != staminaBar
                || _eitrBar != eitrBar
                || _adrenalineBar != adrenalineBar)
            {
                _staminaBar = staminaBar;
                _eitrBar = eitrBar;
                _adrenalineBar = adrenalineBar;
                _staminaBarBaseScale = staminaBar.localScale;

                _staminaRoot = staminaRoot;
                _eitrRoot = eitrRoot;
                _adrenalineRoot = adrenalineRoot;

                _staminaRootBaseScale = staminaRoot.localScale;
                _eitrRootBaseScale = eitrRoot.localScale;
                _adrenalineRootBaseScale = adrenalineRoot.localScale;

                _staminaRootBasePos = staminaRoot.anchoredPosition;
                _eitrRootBasePos = eitrRoot.anchoredPosition;
                _adrenalineRootBasePos = adrenalineRoot.anchoredPosition;

                _vitalsSizeInitialized = true;
            }

            float targetW = SeneaLHudLayoutPlugin.VitalsWidth.Value;
            float targetH = SeneaLHudLayoutPlugin.VitalsHeight.Value;
            Vector2 offset = SeneaLHudLayoutPlugin.VitalsOffset.Value;

            Vector3 staminaLossy = staminaRoot.lossyScale;

            float eitrParentX = eitrRoot.parent != null ? eitrRoot.parent.lossyScale.x : 1f;
            float eitrParentY = eitrRoot.parent != null ? eitrRoot.parent.lossyScale.y : 1f;
            float adrenalineParentX = adrenalineRoot.parent != null ? adrenalineRoot.parent.lossyScale.x : 1f;
            float adrenalineParentY = adrenalineRoot.parent != null ? adrenalineRoot.parent.lossyScale.y : 1f;

            if (eitrParentX > 0.001f && eitrParentY > 0.001f)
            {
                eitrRoot.localScale = new Vector3(
                    staminaLossy.x / eitrParentX,
                    staminaLossy.y / eitrParentY,
                    _eitrRootBaseScale.z);
            }

            if (adrenalineParentX > 0.001f && adrenalineParentY > 0.001f)
            {
                adrenalineRoot.localScale = new Vector3(
                    staminaLossy.x / adrenalineParentX,
                    staminaLossy.y / adrenalineParentY,
                    _adrenalineRootBaseScale.z);
            }

            float scaleX = Mathf.Clamp(targetW / 300f, 0.3f, 3f);
            float scaleY = Mathf.Clamp(targetH / 24f, 0.3f, 3f);

            Vector3 barScale = new Vector3(
                _staminaBarBaseScale.x * scaleX,
                _staminaBarBaseScale.y * scaleY,
                _staminaBarBaseScale.z);

            staminaBar.localScale = barScale;
            eitrBar.localScale = barScale;
            adrenalineBar.localScale = barScale;

            staminaRoot.anchoredPosition = _staminaRootBasePos + offset;
            eitrRoot.anchoredPosition = _eitrRootBasePos + offset;
            adrenalineRoot.anchoredPosition = _adrenalineRootBasePos + offset;

            float gap = 6f;

            RectTransform top, middle, bottom;
            switch (SeneaLHudLayoutPlugin.VitalsOrder.Value)
            {
                case VitalsBarOrder.Stamina_Eitr_Adrenaline:
                    top = staminaRoot; middle = eitrRoot; bottom = adrenalineRoot;
                    break;
                case VitalsBarOrder.Stamina_Adrenaline_Eitr:
                    top = staminaRoot; middle = adrenalineRoot; bottom = eitrRoot;
                    break;
                case VitalsBarOrder.Eitr_Stamina_Adrenaline:
                    top = eitrRoot; middle = staminaRoot; bottom = adrenalineRoot;
                    break;
                case VitalsBarOrder.Eitr_Adrenaline_Stamina:
                    top = eitrRoot; middle = adrenalineRoot; bottom = staminaRoot;
                    break;
                case VitalsBarOrder.Adrenaline_Stamina_Eitr:
                    top = adrenalineRoot; middle = staminaRoot; bottom = eitrRoot;
                    break;
                case VitalsBarOrder.Adrenaline_Eitr_Stamina:
                    top = adrenalineRoot; middle = eitrRoot; bottom = staminaRoot;
                    break;
                default:
                    top = staminaRoot; middle = eitrRoot; bottom = adrenalineRoot;
                    break;
            }

            float topBottom = EdgeY(top, bottom: true);

            float middleHeight = EdgeY(middle, bottom: false) - EdgeY(middle, bottom: true);
            float desiredMiddleTop = topBottom - gap;
            float middleDelta = desiredMiddleTop - EdgeY(middle, bottom: false);

            if (!Mathf.Approximately(middleDelta, 0f))
                Move(middle, 0f, middleDelta);

            float desiredBottomTop = (topBottom - gap) - middleHeight - gap;
            float bottomDelta = desiredBottomTop - EdgeY(bottom, bottom: false);

            if (!Mathf.Approximately(bottomDelta, 0f))
                Move(bottom, 0f, bottomDelta);

            SetBadgeVisible(staminaRoot, "badge", !SeneaLHudLayoutPlugin.HideStaminaNumber.Value);
            SetBadgeVisible(eitrRoot, "badge", !SeneaLHudLayoutPlugin.HideEitrNumber.Value);
            SetBadgeVisible(adrenalineRoot, "badge", !SeneaLHudLayoutPlugin.HideAdrenalineNumber.Value);
        }

        static void SetBadgeVisible(RectTransform root, string badgeName, bool visible)
        {
            if (root == null)
                return;

            Transform badge = root.Find(badgeName);
            if (badge != null && badge.gameObject.activeSelf != visible)
                badge.gameObject.SetActive(visible);
        }

        static void LogBarChildren(RectTransform bar, string label)
        {
            if (bar == null)
                return;

            Debug.Log($"[Vitals] --- {label} children ---");

            for (int i = 0; i < bar.childCount; i++)
            {
                var child = bar.GetChild(i) as RectTransform;
                if (child == null)
                    continue;

                Debug.Log($"[Vitals]   {child.name}: scale={child.localScale}  sizeDelta={child.sizeDelta}  anchorMin={child.anchorMin}  anchorMax={child.anchorMax}");
            }
        }

        static void DumpVitalsDetail(Transform root)
        {
            string[] paths = {
                "Health/fx/vitals",
                "Stamina/fx/stamina",
                "Eitr/fx/eitr",
                "Adrenaline/fx/adrenaline"
            };

            foreach (var path in paths)
            {
                Transform t = root.Find(path);
                if (t == null)
                {
                    Debug.Log($"[Vitals] Missing: {path}");
                    continue;
                }

                Debug.Log($"[Vitals] === {path} ===");
                LogUIHierarchy(t, 0);
            }
        }
    }
}