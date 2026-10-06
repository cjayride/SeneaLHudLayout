using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class WorldHealthText
    {
        const string LabelName = "SeneaLHudLayout_Health";
        const string StarRowName = "SeneaLHudLayout_Stars";
        const string GlowName = "SeneaLHudLayout_StarGlow";
        const string GlowInnerName = "SeneaLHudLayout_StarGlowInner";
        const string LevelName = "SeneaLHudLayout_EnemyLevel";
        const string SeneaLStarRow = "sstars";
        static readonly Regex LevelPattern = new Regex(@"\[Lvl:\s*\d+\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly FieldInfo HudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
        static readonly FieldInfo CharacterField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_character");
        static readonly FieldInfo GuiField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_gui");
        static readonly FieldInfo HealthTextField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_healthText");
        static readonly FieldInfo NameField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_name");
        static readonly Color StarGold = new Color(1f, 0.843f, 0.2f, 1f);
        static readonly Color NoGlow = new Color(0f, 0f, 0f, 0f);
        static readonly Dictionary<int, float> _nameSizes = new Dictionary<int, float>();
        static readonly Dictionary<int, float> _barWidths = new Dictionary<int, float>();
        static readonly Dictionary<int, float> _fillWidths = new Dictionary<int, float>();
        static Type _guiBarType;
        static FieldInfo _guiBarWidth;
        static MethodInfo _setWidth;
        static MethodInfo _extraEffect;
        static bool _extraEffectLookup;

        public static void Apply(Harmony harmony)
        {
            MethodInfo update = AccessTools.Method(typeof(EnemyHud), "UpdateHuds");
            if (update != null)
            {
                harmony.Patch(update, postfix: new HarmonyMethod(typeof(WorldHealthText), nameof(AfterUpdateHuds)));
            }
        }

        [HarmonyPriority(Priority.Last)]
        static void AfterUpdateHuds(EnemyHud __instance)
        {
            if (__instance == null || HudsField == null)
            {
                return;
            }

            bool creatureHud = SeneaLHudLayoutPlugin.CreatureHud == null
                || SeneaLHudLayoutPlugin.CreatureHud.Value;
            bool showHealth = SeneaLHudLayoutPlugin.ShowHealthNumbers != null
                && SeneaLHudLayoutPlugin.ShowHealthNumbers.Value;
            bool showLevel = SeneaLHudLayoutPlugin.ShowEnemyLevel != null
                && SeneaLHudLayoutPlugin.ShowEnemyLevel.Value;
            bool hideExtras = SeneaLHudLayoutPlugin.HideNameplateExtras != null
                && SeneaLHudLayoutPlugin.HideNameplateExtras.Value;
            bool showThreat = SeneaLHudLayoutPlugin.ShowThreatIcons == null
                || SeneaLHudLayoutPlugin.ShowThreatIcons.Value;
            bool showDiamonds = SeneaLHudLayoutPlugin.ShowNameplateDiamonds != null
                && SeneaLHudLayoutPlugin.ShowNameplateDiamonds.Value;

            IDictionary huds = HudsField.GetValue(__instance) as IDictionary;
            if (huds == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in huds)
            {
                object data = entry.Value;
                if (data == null)
                {
                    continue;
                }

                Character character = CharacterField?.GetValue(data) as Character;
                GameObject gui = GuiField?.GetValue(data) as GameObject;
                if (!character || !gui)
                {
                    continue;
                }

                if (!creatureHud)
                {
                    ReleasePlate(gui.transform);
                    continue;
                }

                TMP_Text plateName = NameField?.GetValue(data) as TMP_Text;
                SizeName(plateName);

                if (!showDiamonds && plateName)
                {
                    HideChild(plateName.transform, "sdl");
                    HideChild(plateName.transform, "sdr");
                    HideChild(plateName.transform, "srl");
                    HideChild(plateName.transform, "srr");
                }

                if (hideExtras)
                {
                    HideExtras(gui.transform, plateName);
                }

                if (!showHealth)
                {
                    Transform ours = gui.transform.Find(LabelName);
                    if (!ours)
                    {
                        Transform health = gui.transform.Find("Health");
                        ours = health ? health.Find(LabelName) : null;
                    }

                    if (ours && ours.gameObject.activeSelf)
                    {
                        ours.gameObject.SetActive(false);
                    }

                    Transform vanillaText = gui.transform.Find("Health/HealthText");
                    if (vanillaText && !vanillaText.gameObject.activeSelf)
                    {
                        vanillaText.gameObject.SetActive(true);
                    }
                }
                else
                {
                    TMP_Text label = HealthTextField?.GetValue(data) as TMP_Text;
                    if (!label)
                    {
                        label = EnsureLabel(gui.transform, plateName);
                        HealthTextField?.SetValue(data, label);
                    }

                    Transform vanilla = gui.transform.Find("Health/HealthText");
                    if (vanilla && vanilla.gameObject != label.gameObject)
                    {
                        vanilla.gameObject.SetActive(false);
                    }

                    int hp = Mathf.CeilToInt(character.GetHealth());
                    int max = Mathf.Max(1, Mathf.CeilToInt(character.GetMaxHealth()));
                    label.text = hp + "/" + max;
                    SizeHealth(label, plateName);
                    PlaceHealthStack(character, gui.transform, label, plateName);
                    label.gameObject.SetActive(true);
                }

                // Stars, bar width, and level stay independent of health numbers so
                // toggling ShowHealthNumbers does not recolor or recenter the stars.
                LayoutBarAndStars(character, gui.transform, plateName, showLevel, showThreat);
            }
        }

        static void ReleasePlate(Transform gui)
        {
            HideChild(gui, LabelName);
            HideChild(gui, StarRowName);
            HideChild(gui, LevelName);
            Transform health = gui.Find("Health");
            if (health != null)
            {
                HideChild(health, LabelName);
                Transform vanilla = health.Find("HealthText");
                if (vanilla != null && !vanilla.gameObject.activeSelf)
                {
                    vanilla.gameObject.SetActive(true);
                }
            }

            for (int i = 0; i < gui.childCount; i++)
            {
                Transform child = gui.GetChild(i);
                if (child.name.StartsWith("level_", StringComparison.Ordinal) && !child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);
                }
            }
        }

        public static void Tick()
        {
            EnemyHud hud = EnemyHud.instance;
            if (hud != null)
            {
                AfterUpdateHuds(hud);
            }
        }

        static void HideChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = parent.Find(name);
            if (child && child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
            }
        }

        static TMP_Text EnsureLabel(Transform gui, TMP_Text name)
        {
            Transform health = gui.Find("Health");
            if (!health)
            {
                return null;
            }

            Transform existing = gui.Find(LabelName);
            if (!existing)
            {
                existing = health.Find(LabelName);
            }

            TextMeshProUGUI tmp = existing ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (tmp == null)
            {
                GameObject go = new GameObject(LabelName);
                go.transform.SetParent(gui, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.raycastTarget = false;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
            }

            if (name != null && name.font != null)
            {
                tmp.font = name.font;
                if (name.fontSharedMaterial != null)
                {
                    tmp.fontSharedMaterial = name.fontSharedMaterial;
                }
            }
            else if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            tmp.color = new Color(0.96f, 0.93f, 0.86f);
            tmp.fontSize = name != null ? Mathf.Max(12f, BaseSize(name) * 0.8f) : 14f;
            return tmp;
        }

        static void SizeName(TMP_Text name)
        {
            if (name == null)
            {
                return;
            }

            float scale = SeneaLHudLayoutPlugin.NameTextScale != null ? SeneaLHudLayoutPlugin.NameTextScale.Value : 0.75f;
            name.enableAutoSizing = false;
            name.fontSize = BaseSize(name) * scale;
        }

        static void SizeHealth(TMP_Text label, TMP_Text name)
        {
            if (label == null)
            {
                return;
            }

            float scale = SeneaLHudLayoutPlugin.HealthTextScale != null ? SeneaLHudLayoutPlugin.HealthTextScale.Value : 0.94f;
            float basis = name != null ? Mathf.Max(12f, BaseSize(name) * 0.8f) : 14f;
            label.enableAutoSizing = false;
            label.fontSize = basis * scale;
        }

        static float BaseSize(TMP_Text text)
        {
            int id = text.GetInstanceID();
            if (_nameSizes.TryGetValue(id, out float size) && size > 1f)
            {
                return size;
            }

            size = text.fontSize > 1f ? text.fontSize : 15f;
            _nameSizes[id] = size;
            return size;
        }

        static void PlaceHealthStack(Character character, Transform gui, TMP_Text label, TMP_Text name)
        {
            RectTransform bar = gui.Find("Health") as RectTransform;
            if (bar == null || label == null)
            {
                return;
            }

            bool boss = character.IsBoss() || gui.name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!boss)
            {
                SteadyScale(gui);
                bar.localScale = Vector3.one;
                label.rectTransform.localScale = Vector3.one;
                if (name != null)
                {
                    name.rectTransform.localScale = Vector3.one;
                }
            }

            Vector3 up = bar.TransformVector(Vector3.up);
            float unit = up.magnitude;
            if (unit < 0.0001f)
            {
                return;
            }

            up /= unit;
            Vector3 barTop = Edge(bar, top: true);
            Vector3 mid = Center(bar);
            MoveGlyph(label, barTop, bottom: true, mid);
            if (name != null)
            {
                Vector3 hpTop = GlyphEdge(label, top: true);
                MoveGlyph(name, hpTop, bottom: true, mid);
            }
        }

        static void LayoutBarAndStars(Character character, Transform gui, TMP_Text name, bool showLevel, bool showThreat)
        {
            RectTransform bar = gui.Find("Health") as RectTransform;
            if (bar == null)
            {
                return;
            }

            bool boss = character.IsBoss() || gui.name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!boss)
            {
                SteadyScale(gui);
                bar.localScale = Vector3.one;
                Widen(bar);
            }

            Vector3 up = bar.TransformVector(Vector3.up);
            float unit = up.magnitude;
            if (unit < 0.0001f)
            {
                return;
            }

            up /= unit;
            Vector3 barBottom = Edge(bar, top: false);

            if (!showThreat)
            {
                HideMarker(gui, "Alerted");
                HideMarker(gui, "Aware");
            }
            else
            {
                PlaceThreatIcon(gui, "Alerted", name, up, unit);
                PlaceThreatIcon(gui, "Aware", name, up, unit);
            }

            bool showStars = SeneaLHudLayoutPlugin.ShowCreatureStars == null
                || SeneaLHudLayoutPlugin.ShowCreatureStars.Value;
            int stars = Mathf.Max(0, character.GetLevel() - 1);
            Color glow = EffectGlow(character);
            RectTransform row = showStars ? EnsureStars(gui, character.GetLevel(), stars) : null;
            if (row != null)
            {
                HideRivalStars(gui, row);
                FitStars(row, bar, glow);
                Vector3 starTop = ImageEdge(row, top: true);
                Vector3 delta = barBottom - up * unit - starTop;
                delta.x = AlignOffset(row, bar);
                row.position += delta;
                row.SetAsLastSibling();
            }
            else
            {
                HideRivalStars(gui, null);
            }

            ApplyLevel(gui, bar, name, character, showLevel, up, unit, barBottom);
        }

        /// <summary>
        /// Keep a single star row. Vanilla/SeneaL level_N plus CLLC/custom extras
        /// otherwise stack as gold + colored duplicates.
        /// </summary>
        static void HideRivalStars(Transform gui, Transform keep)
        {
            for (int i = 0; i < gui.childCount; i++)
            {
                Transform child = gui.GetChild(i);
                if (child == keep)
                {
                    continue;
                }

                string name = child.name;
                bool starRow = name.StartsWith("level_", StringComparison.Ordinal)
                    || name == StarRowName
                    || name.IndexOf("Star", StringComparison.OrdinalIgnoreCase) >= 0;
                if (starRow && child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                }
            }

            // SeneaL UI 1.1.9 parents its own "sstars" row to the Health bar, not the plate root.
            HideChild(gui.Find("Health"), SeneaLStarRow);
        }

        static Color EffectGlow(Character character)
        {
            if (!_extraEffectLookup)
            {
                _extraEffectLookup = true;
                Type api = ModTypes.Find("CreatureLevelControl", "CreatureLevelControl.API");
                _extraEffect = api != null ? AccessTools.Method(api, "GetExtraEffectCreature") : null;
            }

            if (_extraEffect == null || !character)
            {
                return NoGlow;
            }

            object effect;
            try
            {
                effect = _extraEffect.Invoke(null, new object[] { character });
            }
            catch (Exception)
            {
                return NoGlow;
            }

            switch (effect != null ? effect.ToString() : "")
            {
                case "Regenerating":
                    return Color.green;
                case "Aggressive":
                    return Color.red;
                case "Armored":
                    return Color.blue;
                case "Curious":
                    return Color.cyan;
                case "Quick":
                    return Color.magenta;
                case "Splitting":
                    return Color.white;
                default:
                    return NoGlow;
            }
        }

        static void HideMarker(Transform gui, string name)
        {
            Transform marker = gui.Find(name);
            if (marker != null && marker.gameObject.activeSelf)
            {
                marker.gameObject.SetActive(false);
            }
        }

        static void PlaceThreatIcon(Transform gui, string markerName, TMP_Text creatureName, Vector3 up, float unit)
        {
            if (!(gui.Find(markerName) is RectTransform rt) || !rt.gameObject.activeInHierarchy)
            {
                return;
            }

            float size = SeneaLHudLayoutPlugin.ThreatIconSize != null
                ? SeneaLHudLayoutPlugin.ThreatIconSize.Value
                : 5f;
            size = Mathf.Clamp(size, 2f, 32f);

            // Stretch anchors ignore sizeDelta — lock to a centered box so size actually applies.
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(size, size);

            float opacity = SeneaLHudLayoutPlugin.ThreatIconOpacity != null
                ? SeneaLHudLayoutPlugin.ThreatIconOpacity.Value
                : 0.71f;
            opacity = Mathf.Clamp01(opacity);
            Graphic[] graphics = rt.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Color color = graphics[i].color;
                color.a = opacity;
                graphics[i].color = color;
            }

            float liftPx = SeneaLHudLayoutPlugin.ThreatIconLift != null
                ? SeneaLHudLayoutPlugin.ThreatIconLift.Value
                : 10f;
            liftPx = Mathf.Clamp(liftPx, 0f, 24f);

            // Pin the icon center above the name so changing size does not slide it vertically.
            if (creatureName != null)
            {
                Vector3 nameTop = GlyphEdge(creatureName, top: true);
                Vector3 target = nameTop + up * ((liftPx + size * 0.5f) * unit);
                Vector3 delta = target - Center(rt);
                delta.x = 0f;
                rt.position += delta;
            }
            else
            {
                rt.position += up * (liftPx * unit);
            }
        }

        static void ApplyLevel(Transform gui, RectTransform bar, TMP_Text name, Character character, bool show, Vector3 up, float unit, Vector3 barBottom)
        {
            // Prefer an existing CLLC-style [Lvl:N] label when present.
            TMP_Text foreign = FindForeignLevel(gui);
            if (foreign != null)
            {
                // Do not hide CLLC's label when our option is off — only force it on when asked.
                if (show && !foreign.gameObject.activeSelf)
                {
                    foreign.gameObject.SetActive(true);
                }

                HideLevel(gui);
                return;
            }

            if (!show)
            {
                HideLevel(gui);
                return;
            }

            TMP_Text label = EnsureLevel(gui, name);
            if (label == null)
            {
                return;
            }

            int level = character.GetLevel();
            label.text = "[Lvl:" + level + "]";
            float size = SeneaLHudLayoutPlugin.LevelSize != null ? SeneaLHudLayoutPlugin.LevelSize.Value : 14f;
            label.fontSize = size;
            label.gameObject.SetActive(true);

            RectTransform rt = label.rectTransform;
            rt.localScale = Vector3.one;
            Vector3 right = RightEdge(bar);
            Vector3 top = GlyphEdge(label, top: true);
            Vector3 delta = (barBottom - up * unit) - top;
            delta.x = right.x - GlyphRight(label).x;
            rt.position += delta;
            rt.SetAsLastSibling();
        }

        static TMP_Text FindForeignLevel(Transform gui)
        {
            TMP_Text[] texts = gui.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text.gameObject.name == LevelName || text.gameObject.name == LabelName)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(text.text) && LevelPattern.IsMatch(text.text))
                {
                    return text;
                }
            }

            return null;
        }

        static TMP_Text EnsureLevel(Transform gui, TMP_Text name)
        {
            Transform existing = gui.Find(LevelName);
            TextMeshProUGUI tmp = existing ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (tmp == null)
            {
                GameObject go = new GameObject(LevelName);
                go.transform.SetParent(gui, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.raycastTarget = false;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
            }

            if (name != null && name.font != null)
            {
                tmp.font = name.font;
                if (name.fontSharedMaterial != null)
                {
                    tmp.fontSharedMaterial = name.fontSharedMaterial;
                }
            }
            else if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            tmp.color = new Color(0.96f, 0.93f, 0.86f);
            return tmp;
        }

        static void HideExtras(Transform gui, TMP_Text plateName)
        {
            TMP_Text[] texts = gui.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text == plateName)
                {
                    continue;
                }

                string name = text.gameObject.name;
                if (name == LabelName || name == LevelName || name == "HealthText" || name == "StaminaText" || name == "Name")
                {
                    continue;
                }

                Transform health = gui.Find("Health");
                Transform stamina = gui.Find("Stamina");
                if ((health != null && text.transform.IsChildOf(health)) ||
                    (stamina != null && text.transform.IsChildOf(stamina)))
                {
                    continue;
                }

                if (LevelPattern.IsMatch(text.text ?? ""))
                {
                    continue;
                }

                // Extra affix / mutation / empty caption lines from CLLC, EliteCreatures, etc.
                if (text.gameObject.activeSelf)
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        static void SteadyScale(Transform gui)
        {
            if (gui.parent == null)
            {
                return;
            }

            float want = gui.parent.lossyScale.y;
            float have = gui.lossyScale.y;
            if (want < 0.0001f || have < 0.0001f)
            {
                return;
            }

            float fix = want / have;
            if (Mathf.Abs(fix - 1f) < 0.02f)
            {
                return;
            }

            Vector3 scale = gui.localScale;
            gui.localScale = new Vector3(scale.x * fix, scale.y * fix, scale.z == 0f ? 1f : scale.z);
        }

        static void Widen(RectTransform bar)
        {
            float scale = SeneaLHudLayoutPlugin.BarWidth != null ? SeneaLHudLayoutPlugin.BarWidth.Value : 1f;
            int id = bar.GetInstanceID();
            if (!_barWidths.TryGetValue(id, out float width) || width < 30f || width > 200f)
            {
                float raw = bar.sizeDelta.x;
                width = raw >= 30f && raw <= 200f ? raw : 100f;
                _barWidths[id] = width;
            }

            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(30f, width * scale));
            FitFills(bar, scale);
        }

        static void FitFills(RectTransform host, float scale)
        {
            if (_guiBarType == null)
            {
                _guiBarType = AccessTools.TypeByName("GuiBar");
                if (_guiBarType == null)
                {
                    return;
                }

                _guiBarWidth = AccessTools.Field(_guiBarType, "m_width");
                _setWidth = AccessTools.Method(_guiBarType, "SetWidth");
            }

            if (_guiBarWidth == null || _setWidth == null)
            {
                return;
            }

            Component[] fills = host.GetComponentsInChildren(_guiBarType, true);
            for (int i = 0; i < fills.Length; i++)
            {
                Component fill = fills[i];
                int id = fill.GetInstanceID();
                if (!_fillWidths.TryGetValue(id, out float width) || width < 20f || width > 200f)
                {
                    object raw = _guiBarWidth.GetValue(fill);
                    width = raw is float value ? value : 0f;
                    if (width < 20f || width > 200f)
                    {
                        width = 100f;
                    }

                    _fillWidths[id] = width;
                }

                _setWidth.Invoke(fill, new object[] { width * scale });
            }
        }

        static void MoveGlyph(TMP_Text text, Vector3 worldTarget, bool bottom, Vector3 mid)
        {
            Vector3 edge = GlyphEdge(text, top: !bottom);
            Vector3 center = Center(text.rectTransform);
            Vector3 delta = worldTarget - edge;
            delta.x = mid.x - center.x;
            text.rectTransform.position += delta;
        }

        static Vector3 GlyphEdge(TMP_Text text, bool top)
        {
            text.ForceMeshUpdate();
            float min = float.MaxValue;
            float max = float.MinValue;
            TMP_TextInfo info = text.textInfo;
            int count = info != null ? info.characterCount : 0;
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                Vector3[] verts = info.meshInfo[character.materialReferenceIndex].vertices;
                int index = character.vertexIndex;
                if (verts == null || index + 3 >= verts.Length)
                {
                    continue;
                }

                for (int corner = 0; corner < 4; corner++)
                {
                    float y = verts[index + corner].y;
                    if (y < min)
                    {
                        min = y;
                    }

                    if (y > max)
                    {
                        max = y;
                    }
                }
            }

            if (max <= min)
            {
                return Edge(text.rectTransform, top);
            }

            return text.rectTransform.TransformPoint(new Vector3(0f, top ? max : min, 0f));
        }

        static Vector3 GlyphRight(TMP_Text text)
        {
            text.ForceMeshUpdate();
            float max = float.MinValue;
            bool found = false;
            TMP_TextInfo info = text.textInfo;
            int count = info != null ? info.characterCount : 0;
            Vector3 point = RightEdge(text.rectTransform);
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                Vector3[] verts = info.meshInfo[character.materialReferenceIndex].vertices;
                int index = character.vertexIndex;
                if (verts == null || index + 3 >= verts.Length)
                {
                    continue;
                }

                for (int corner = 0; corner < 4; corner++)
                {
                    float x = verts[index + corner].x;
                    if (!found || x > max)
                    {
                        max = x;
                        found = true;
                    }
                }
            }

            return found ? text.rectTransform.TransformPoint(new Vector3(max, 0f, 0f)) : point;
        }

        static void HideLevel(Transform gui)
        {
            Transform existing = gui.Find(LevelName);
            if (existing != null && existing.gameObject.activeSelf)
            {
                existing.gameObject.SetActive(false);
            }
        }

        static Vector3 Edge(RectTransform rt, bool top)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return top ? (corners[1] + corners[2]) * 0.5f : (corners[0] + corners[3]) * 0.5f;
        }

        static Vector3 LeftEdge(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return (corners[0] + corners[1]) * 0.5f;
        }

        static Vector3 RightEdge(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return (corners[2] + corners[3]) * 0.5f;
        }

        static Vector3 Center(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        static RectTransform EnsureStars(Transform gui, int level, int stars)
        {
            Transform custom = gui.Find(StarRowName);
            if (stars <= 0)
            {
                if (custom != null)
                {
                    custom.gameObject.SetActive(false);
                }

                return null;
            }

            // SeneaL UI 1.1.9 zeroes the CanvasGroup alpha on level_2 and level_3, so reusing
            // those rows for 1-2 star creatures renders nothing. Always build our own row.
            for (int i = 0; i < gui.childCount; i++)
            {
                Transform child = gui.GetChild(i);
                if (child.name.StartsWith("level_"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            RectTransform row = custom as RectTransform;
            if (row == null)
            {
                GameObject go = new GameObject(StarRowName);
                go.transform.SetParent(gui, false);
                row = go.AddComponent<RectTransform>();
            }

            if (!row.gameObject.activeSelf)
            {
                row.gameObject.SetActive(true);
            }

            Sprite sprite = StarSprite(gui);
            while (row.childCount < stars)
            {
                GameObject star = new GameObject("star");
                star.transform.SetParent(row, false);
                Image image = star.AddComponent<Image>();
                image.raycastTarget = false;
                image.sprite = sprite;
                image.color = StarGold;
                image.preserveAspect = true;
                RectTransform starRect = star.GetComponent<RectTransform>();
                starRect.sizeDelta = new Vector2(12f, 12f);
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
            }

            for (int i = 0; i < row.childCount; i++)
            {
                bool on = i < stars;
                if (row.GetChild(i).gameObject.activeSelf != on)
                {
                    row.GetChild(i).gameObject.SetActive(on);
                }
            }

            return row;
        }

        static Sprite StarSprite(Transform gui)
        {
            Transform sample = gui.Find("level_3");
            if (sample == null)
            {
                sample = gui.Find("level_2");
            }

            if (sample == null)
            {
                return null;
            }

            Image image = sample.GetComponentInChildren<Image>(true);
            return image != null ? image.sprite : null;
        }

        static float StarPixels()
        {
            float size = SeneaLHudLayoutPlugin.StarSize != null ? SeneaLHudLayoutPlugin.StarSize.Value : 10f;
            return Mathf.Clamp(size, 4f, 24f);
        }

        static void FitStars(RectTransform row, RectTransform bar, Color tint)
        {
            float size = StarPixels();
            float barScale = bar.lossyScale.y;
            if (barScale < 0.0001f)
            {
                barScale = 0.0001f;
            }

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null && layout.enabled)
            {
                layout.enabled = false;
            }

            row.localScale = Vector3.one;
            int count = 0;
            for (int i = 0; i < row.childCount; i++)
            {
                if (row.GetChild(i).gameObject.activeSelf)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return;
            }

            float step = size + 1f;
            float x = 0f;
            Color color = FadeStar(tint.a > 0.01f ? tint : StarGold);
            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (!child.gameObject.activeSelf || !(child is RectTransform slot))
                {
                    continue;
                }

                Image face = PickFace(child);
                if (face == null)
                {
                    continue;
                }

                face.color = color;
                face.preserveAspect = true;
                Transform walk = face.transform;
                while (walk != null && walk != slot)
                {
                    walk.localScale = Vector3.one;
                    walk = walk.parent;
                }

                float parentScale = slot.parent != null ? slot.parent.lossyScale.y : barScale;
                if (parentScale < 0.0001f)
                {
                    parentScale = barScale;
                }

                float match = barScale / parentScale;
                slot.localScale = new Vector3(match, match, 1f);
                slot.anchorMin = new Vector2(0f, 0.5f);
                slot.anchorMax = new Vector2(0f, 0.5f);
                slot.pivot = new Vector2(0f, 0.5f);
                slot.sizeDelta = new Vector2(size, size);
                slot.anchoredPosition = new Vector2(x, 0f);
                if (face.rectTransform != slot)
                {
                    RectTransform graphic = face.rectTransform;
                    graphic.anchorMin = Vector2.zero;
                    graphic.anchorMax = Vector2.one;
                    graphic.offsetMin = Vector2.zero;
                    graphic.offsetMax = Vector2.zero;
                    graphic.localScale = Vector3.one;
                }

                x += step;
            }
        }

        static Image PickFace(Transform slot)
        {
            Image[] images = slot.GetComponentsInChildren<Image>(true);
            Image face = null;
            int depth = -1;
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                string name = image.gameObject.name;
                if (name == GlowName || name == GlowInnerName || name == "face")
                {
                    image.gameObject.SetActive(false);
                    continue;
                }

                int here = 0;
                Transform walk = image.transform;
                while (walk != null && walk != slot)
                {
                    here++;
                    walk = walk.parent;
                }

                if (face == null || here > depth)
                {
                    if (face != null)
                    {
                        face.enabled = false;
                    }

                    face = image;
                    depth = here;
                    face.enabled = true;
                }
                else
                {
                    image.enabled = false;
                }
            }

            return face;
        }

        static Vector3 ImageEdge(RectTransform row, bool top)
        {
            Vector3 point = Edge(row, top);
            bool found = false;
            float best = top ? float.MinValue : float.MaxValue;
            Image[] images = row.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (!StarFace(image))
                {
                    continue;
                }

                Vector3 edge = Edge(image.rectTransform, top);
                if (!found || (top ? edge.y > best : edge.y < best))
                {
                    best = edge.y;
                    point = edge;
                    found = true;
                }
            }

            return point;
        }

        static float AlignOffset(RectTransform row, RectTransform bar)
        {
            StarAlign align = SeneaLHudLayoutPlugin.StarAlignment != null
                ? SeneaLHudLayoutPlugin.StarAlignment.Value
                : StarAlign.Left;
            float left = ImageLeft(row).x;
            float right = ImageRight(row).x;

            switch (align)
            {
                case StarAlign.Center:
                    return Center(bar).x - (left + right) * 0.5f;
                case StarAlign.Right:
                    return RightEdge(bar).x - right;
                default:
                    return LeftEdge(bar).x - left;
            }
        }

        static Vector3 ImageRight(RectTransform row)
        {
            Vector3 point = RightEdge(row);
            bool found = false;
            float best = float.MinValue;
            Image[] images = row.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (!StarFace(images[i]))
                {
                    continue;
                }

                Vector3 right = RightEdge(images[i].rectTransform);
                if (!found || right.x > best)
                {
                    best = right.x;
                    point = right;
                    found = true;
                }
            }

            return point;
        }

        static Vector3 ImageLeft(RectTransform row)
        {
            Vector3 point = LeftEdge(row);
            bool found = false;
            float best = float.MaxValue;
            Image[] images = row.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (!StarFace(images[i]))
                {
                    continue;
                }

                Vector3 left = LeftEdge(images[i].rectTransform);
                if (!found || left.x < best)
                {
                    best = left.x;
                    point = left;
                    found = true;
                }
            }

            return point;
        }

        static bool StarFace(Image image)
        {
            if (image == null || !image.enabled || !image.gameObject.activeInHierarchy)
            {
                return false;
            }

            string name = image.gameObject.name;
            return name != GlowName && name != GlowInnerName && name != "face";
        }

        static Color FadeStar(Color color)
        {
            color.r *= 0.7f;
            color.g *= 0.7f;
            color.b *= 0.7f;
            color.a = 0.75f;
            return color;
        }
    }
}
