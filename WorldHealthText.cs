using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class WorldHealthText
    {
        const string LabelName = "SeneaLHudLayout_Health";
        static readonly FieldInfo HudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
        static readonly FieldInfo CharacterField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_character");
        static readonly FieldInfo GuiField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_gui");
        static readonly FieldInfo HealthTextField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_healthText");
        static readonly FieldInfo NameField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_name");

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

            bool show = SeneaLHudLayoutPlugin.ShowHealthNumbers != null
                && SeneaLHudLayoutPlugin.ShowHealthNumbers.Value;

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

                TMP_Text plateName = NameField?.GetValue(data) as TMP_Text;
                if (!show)
                {
                    SizeName(plateName);
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

                    continue;
                }

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
                SizeName(plateName);
                SizeHealth(label, plateName);
                Layout(character, gui.transform, label, plateName);
                label.gameObject.SetActive(true);
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

        static readonly Dictionary<int, float> _nameSizes = new Dictionary<int, float>();

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

        static readonly Dictionary<int, float> _barWidths = new Dictionary<int, float>();
        static readonly Dictionary<int, float> _fillWidths = new Dictionary<int, float>();
        static Type _guiBarType;
        static FieldInfo _guiBarWidth;
        static MethodInfo _setWidth;
        const string StarRowName = "SeneaLHudLayout_Stars";
        const string GlowName = "SeneaLHudLayout_StarGlow";
        const string GlowInnerName = "SeneaLHudLayout_StarGlowInner";
        const string LevelName = "SeneaLHudLayout_EnemyLevel";
        static readonly Color StarGold = new Color(1f, 0.843f, 0.2f, 1f);
        static readonly Color NoGlow = new Color(0f, 0f, 0f, 0f);
        static MethodInfo _extraEffect;
        static bool _extraEffectLookup;

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

        static void Layout(Character character, Transform gui, TMP_Text label, TMP_Text name)
        {
            RectTransform bar = gui.Find("Health") as RectTransform;
            if (bar == null || label == null)
            {
                return;
            }

            bool boss = character.IsBoss() || gui.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!boss)
            {
                Widen(bar);
            }

            float gapPx = 0f;
            Vector3 up = bar.TransformVector(Vector3.up);
            float unit = up.magnitude;
            if (unit < 0.0001f)
            {
                return;
            }

            up /= unit;
            float gap = gapPx * unit;
            Vector3 barTop = Edge(bar, top: true);
            Vector3 barBottom = Edge(bar, top: false);
            Vector3 mid = Center(bar);

            MoveGlyph(label, barTop + up * gap, bottom: true, mid);
            if (name != null)
            {
                Vector3 hpTop = GlyphEdge(label, top: true);
                MoveGlyph(name, hpTop + up * gap, bottom: true, mid);
            }

            HideLevel(gui);
            HideMarker(gui, "Alerted");
            HideMarker(gui, "Aware");

            int stars = Mathf.Max(0, character.GetLevel() - 1);
            RectTransform row = EnsureStars(gui, character.GetLevel(), stars, EffectGlow(character));
            if (row != null)
            {
                MoveRect(row, barBottom - up * gap, bottom: false, mid);
                row.SetAsLastSibling();
            }
        }

        static void Widen(RectTransform bar)
        {
            float scale = SeneaLHudLayoutPlugin.BarWidth != null ? SeneaLHudLayoutPlugin.BarWidth.Value : 1f;
            int id = bar.GetInstanceID();
            if (!_barWidths.TryGetValue(id, out float width) || width < 8f)
            {
                width = bar.sizeDelta.x > 8f ? bar.sizeDelta.x : bar.rect.width;
                _barWidths[id] = width;
            }

            bar.sizeDelta = new Vector2(Mathf.Max(8f, width * scale), bar.sizeDelta.y);
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
                if (!_fillWidths.TryGetValue(id, out float width) || width < 1f)
                {
                    object raw = _guiBarWidth.GetValue(fill);
                    width = raw is float value ? value : 0f;
                    if (width < 1f)
                    {
                        continue;
                    }

                    _fillWidths[id] = width;
                }

                _setWidth.Invoke(fill, new object[] { width * scale });
            }
        }

        static void MoveRect(RectTransform rt, Vector3 worldTarget, bool bottom, Vector3 mid)
        {
            Vector3 edge = Edge(rt, top: !bottom);
            Vector3 center = Center(rt);
            Vector3 delta = worldTarget - edge;
            delta.x = mid.x - center.x;
            rt.position += delta;
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

        static void HideMarker(Transform gui, string name)
        {
            Transform marker = gui.Find(name);
            if (marker != null && marker.gameObject.activeSelf)
            {
                marker.gameObject.SetActive(false);
            }
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

        static Vector3 Center(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        static RectTransform EnsureStars(Transform gui, int level, int stars, Color glow)
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

            Transform vanilla = gui.Find("level_" + level);
            if (level <= 3 && vanilla is RectTransform vanillaRow)
            {
                if (custom != null)
                {
                    custom.gameObject.SetActive(false);
                }

                if (!vanilla.gameObject.activeSelf)
                {
                    vanilla.gameObject.SetActive(true);
                }

                PackStars(vanillaRow, StarSprite(gui), glow);
                return vanillaRow;
            }

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
            }

            for (int i = 0; i < row.childCount; i++)
            {
                bool on = i < stars;
                if (row.GetChild(i).gameObject.activeSelf != on)
                {
                    row.GetChild(i).gameObject.SetActive(on);
                }
            }

            PackStars(row, sprite, glow);
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

        static void PackStars(RectTransform row, Sprite sprite, Color glow)
        {
            int count = 0;
            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                if (child.name.StartsWith("star") || child.GetComponentInChildren<Image>(true) != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return;
            }

            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 1f);
            row.localScale = Vector3.one;
            int shown = 0;
            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                bool star = child.name.StartsWith("star") || child.GetComponentInChildren<Image>(true) != null;
                if (!star)
                {
                    continue;
                }

                float x = -(count - 1) * 7f + shown * 14f;
                if (child is RectTransform childRect)
                {
                    childRect.anchorMin = new Vector2(0.5f, 1f);
                    childRect.anchorMax = new Vector2(0.5f, 1f);
                    childRect.pivot = new Vector2(0.5f, 1f);
                    childRect.sizeDelta = new Vector2(12f, 12f);
                    childRect.anchoredPosition = new Vector2(x, 0f);
                    childRect.localScale = Vector3.one;
                }

                LiftStarFace(child);
                PaintGlow(child, sprite, glow);
                Image[] images = child.GetComponentsInChildren<Image>(true);
                for (int n = 0; n < images.Length; n++)
                {
                    if (IsGlow(images[n]))
                    {
                        continue;
                    }

                    images[n].enabled = true;
                    if (images[n].sprite == null && sprite != null)
                    {
                        images[n].sprite = sprite;
                    }

                    if (images[n].color.a < 0.2f || images[n].gameObject.name == "face")
                    {
                        images[n].color = StarGold;
                    }

                    images[n].preserveAspect = true;
                    images[n].transform.SetAsLastSibling();
                }

                shown++;
            }

            row.sizeDelta = new Vector2(Mathf.Max(16f, count * 14f + 4f), 13f);
        }

        static void LiftStarFace(Transform star)
        {
            Image root = star.GetComponent<Image>();
            if (root == null)
            {
                return;
            }

            GameObject face = new GameObject("face");
            face.transform.SetParent(star, false);
            Image image = face.AddComponent<Image>();
            image.sprite = root.sprite;
            image.color = StarGold;
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform rect = face.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            UnityEngine.Object.DestroyImmediate(root);
        }

        static void PaintGlow(Transform star, Sprite sprite, Color tint)
        {
            bool on = tint.a > 0f;
            PaintGlowLayer(star, GlowInnerName, sprite, tint, 20f, 0.78f, on);
            PaintGlowLayer(star, GlowName, sprite, tint, 34f, 0.32f, on);
        }

        static void PaintGlowLayer(Transform star, string name, Sprite sprite, Color tint, float size, float alpha, bool on)
        {
            Transform existing = NamedChild(star, name);
            if (!on)
            {
                if (existing != null && existing.gameObject.activeSelf)
                {
                    existing.gameObject.SetActive(false);
                }

                return;
            }

            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(star, false);
                image = go.AddComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                RectTransform created = go.GetComponent<RectTransform>();
                created.anchorMin = new Vector2(0.5f, 0.5f);
                created.anchorMax = new Vector2(0.5f, 0.5f);
                created.pivot = new Vector2(0.5f, 0.5f);
                created.anchoredPosition = Vector2.zero;
                existing = go.transform;
            }

            if (!existing.gameObject.activeSelf)
            {
                existing.gameObject.SetActive(true);
            }

            existing.SetAsFirstSibling();
            RectTransform rect = existing as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(size, size);
            }

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            image.color = new Color(tint.r, tint.g, tint.b, alpha);
        }

        static Transform NamedChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        static bool IsGlow(Image image)
        {
            string name = image.gameObject.name;
            return name == GlowName || name == GlowInnerName;
        }

    }
}
