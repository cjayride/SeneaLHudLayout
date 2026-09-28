using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class SlotCue
    {
        const string GlowName = "SeneaLHudLayout_Glow";
        const string RarityName = "SeneaLHudLayout_Rarity";
        const string MarkName = "SeneaLHudLayout_Mark";
        const string WashName = "SeneaLHudLayout_Equip";
        const string TimeName = "SeneaLHudLayout_EquipTime";
        const int EquippedState = 2;

        static readonly List<Tracked> Slots = new List<Tracked>();
        static Type _slotType;
        static FieldInfo _state;
        static FieldInfo _rar;
        static FieldInfo _bg;
        static FieldInfo _icon;
        static FieldInfo _actionType;
        static FieldInfo _actionItem;
        static FieldInfo _actionDuration;
        static FieldInfo _actionTime;
        static FieldInfo _queue;
        static FieldInfo _root;
        static FieldInfo _count;
        static MethodInfo _progress;
        static FieldInfo _playerGrid;
        static FieldInfo _containerGrid;
        static FieldInfo _elements;
        static Sprite _white;
        static Sprite _check;
        static TMP_FontAsset _font;
        static Material _fontMat;
        static readonly List<TextMeshProUGUI> Labels = new List<TextMeshProUGUI>();
        static ItemDrop.ItemData _trackedItem;
        static float _trackedEnd;
        static Canvas _overlay;
        static TextMeshProUGUI _overlayText;
        static bool _ready;
        static bool _logged;
        static bool _highlightWas;
        static bool _pipWas;
        static bool _rarityWas;
        static float _rarityStrengthWas = -1f;
        static Color _colorWas;
        static Color _checkWas;
        static float _sizeWas = -1f;
        static float _pipSizeWas = -1f;
        static float _scanUntil;

        class Tracked
        {
            public object Slot;
            public ItemDrop.ItemData Item;
            public int Seen = -1;
        }

        public static void Apply(Harmony harmony)
        {
            _slotType = AccessTools.TypeByName("SeneaLUI.Components.Slot");
            if (_slotType == null)
            {
                return;
            }

            _state = AccessTools.Field(_slotType, "_state");
            _rar = AccessTools.Field(_slotType, "_rar");
            _bg = AccessTools.Field(_slotType, "_bg");
            _icon = AccessTools.Field(_slotType, "_icon");
            _root = AccessTools.Field(_slotType, "Root");
            _count = AccessTools.Field(_slotType, "_count");
            MethodInfo refresh = AccessTools.Method(_slotType, "Refresh");
            MethodInfo set = AccessTools.Method(_slotType, "Set", new[] { typeof(ItemDrop.ItemData) });
            if (refresh != null)
            {
                harmony.Patch(refresh, postfix: new HarmonyMethod(typeof(SlotCue), nameof(AfterRefresh)));
            }

            if (set != null)
            {
                harmony.Patch(set, postfix: new HarmonyMethod(typeof(SlotCue), nameof(AfterSet)));
            }

            MethodInfo queueEquip = AccessTools.Method(typeof(Player), "QueueEquipAction");
            MethodInfo queueUnequip = AccessTools.Method(typeof(Player), "QueueUnequipAction");
            if (queueEquip != null)
            {
                harmony.Patch(queueEquip, postfix: new HarmonyMethod(typeof(SlotCue), nameof(AfterQueued)));
            }

            if (queueUnequip != null)
            {
                harmony.Patch(queueUnequip, postfix: new HarmonyMethod(typeof(SlotCue), nameof(AfterQueued)));
            }

            Type action = AccessTools.Inner(typeof(Player), "MinorActionData");
            _actionType = AccessTools.Field(action, "m_type");
            _actionItem = AccessTools.Field(action, "m_item");
            _actionDuration = AccessTools.Field(action, "m_duration");
            _actionTime = AccessTools.Field(action, "m_time");
            _queue = AccessTools.Field(typeof(Player), "m_actionQueue");
            _progress = AccessTools.Method(typeof(Player), "GetActionProgress", new[]
            {
                typeof(string).MakeByRefType(),
                typeof(float).MakeByRefType(),
                action.MakeByRefType()
            });
            _playerGrid = AccessTools.Field(typeof(InventoryGui), "m_playerGrid");
            _containerGrid = AccessTools.Field(typeof(InventoryGui), "m_containerGrid");
            _elements = AccessTools.Field(typeof(InventoryGrid), "m_elements");
        }

        public static void Tick()
        {
            if (!Ready())
            {
                return;
            }

            ItemDrop.ItemData busy = null;
            int kind = -1;
            float left = 0f;
            bool cue = SeneaLHudLayoutPlugin.ShowEquipCue != null && SeneaLHudLayoutPlugin.ShowEquipCue.Value;
            if (cue && Player.m_localPlayer != null)
            {
                TryAction(Player.m_localPlayer, out busy, out kind, out left);
            }

            if (cue && (busy == null || left <= 0.05f) && _trackedItem != null && Time.time < _trackedEnd)
            {
                busy = _trackedItem;
                left = _trackedEnd - Time.time;
            }

            if (busy == null || left <= 0.05f)
            {
                _trackedItem = null;
            }

            PlaceOverlay(busy, left);
            WatchHighlight();
            for (int i = 0; i < Slots.Count; i++)
            {
                object slot = Slots[i].Slot;
                if (slot == null || _state == null)
                {
                    continue;
                }

                int state = _state.GetValue(slot) is int n ? n : 0;
                if (Slots[i].Seen == state && !RarityPending(slot))
                {
                    continue;
                }

                Slots[i].Seen = state;
                Paint(slot);
            }
        }

        static void AfterQueued(ItemDrop.ItemData item)
        {
            if (SeneaLHudLayoutPlugin.ShowEquipCue == null
                || !SeneaLHudLayoutPlugin.ShowEquipCue.Value
                || item == null
                || item.m_shared == null)
            {
                return;
            }

            _trackedItem = item;
            _trackedEnd = Time.time + Mathf.Max(0.2f, item.m_shared.m_equipDuration);
        }

        static void AfterSet(object __instance, ItemDrop.ItemData item)
        {
            if (__instance == null)
            {
                return;
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                if (ReferenceEquals(Slots[i].Slot, __instance))
                {
                    Slots[i].Item = item;
                    return;
                }
            }

            Slots.Add(new Tracked { Slot = __instance, Item = item });
        }

        static void WatchHighlight()
        {
            bool highlight = SeneaLHudLayoutPlugin.Highlight != null && SeneaLHudLayoutPlugin.Highlight.Value;
            bool pip = SeneaLHudLayoutPlugin.CornerPip != null && SeneaLHudLayoutPlugin.CornerPip.Value;
            bool rarity = SeneaLHudLayoutPlugin.RarityFill != null && SeneaLHudLayoutPlugin.RarityFill.Value;
            float rarityStrength = SeneaLHudLayoutPlugin.RarityFillStrength != null ? SeneaLHudLayoutPlugin.RarityFillStrength.Value : 0.5f;
            Color color = SeneaLHudLayoutPlugin.HighlightColor != null ? SeneaLHudLayoutPlugin.HighlightColor.Value : Color.white;
            Color check = SeneaLHudLayoutPlugin.CheckColor != null ? SeneaLHudLayoutPlugin.CheckColor.Value : Color.green;
            float size = SeneaLHudLayoutPlugin.HighlightSize != null ? SeneaLHudLayoutPlugin.HighlightSize.Value : 0f;
            float pipSize = SeneaLHudLayoutPlugin.CornerPipSize != null ? SeneaLHudLayoutPlugin.CornerPipSize.Value : 0f;
            bool changed = highlight != _highlightWas
                || pip != _pipWas
                || rarity != _rarityWas
                || !Mathf.Approximately(rarityStrength, _rarityStrengthWas)
                || color != _colorWas
                || check != _checkWas
                || !Mathf.Approximately(size, _sizeWas)
                || !Mathf.Approximately(pipSize, _pipSizeWas);
            if (highlight && !_highlightWas)
            {
                _scanUntil = Time.time + 8f;
            }

            bool scan = changed || (highlight && Time.time <= _scanUntil && Time.frameCount % 20 == 0);
            _highlightWas = highlight;
            _pipWas = pip;
            _rarityWas = rarity;
            _rarityStrengthWas = rarityStrength;
            _colorWas = color;
            _checkWas = check;
            _sizeWas = size;
            _pipSizeWas = pipSize;
            if (!scan)
            {
                return;
            }

            CollectSlots();
            for (int i = 0; i < Slots.Count; i++)
            {
                Slots[i].Seen = -1;
            }
        }

        static void CollectSlots()
        {
            Type rootType = AccessTools.TypeByName("SeneaLUI.Hud.HudRoot");
            FieldInfo hotbarField = AccessTools.Field(rootType, "_hotbar");
            object hotbar = hotbarField != null ? hotbarField.GetValue(null) : null;
            if (hotbar == null)
            {
                return;
            }

            Type hotbarType = hotbar.GetType();
            RememberArray(hotbar, AccessTools.Field(hotbarType, "_slots"));
            RememberArray(hotbar, AccessTools.Field(hotbarType, "_act"));
            RememberArray(hotbar, AccessTools.Field(hotbarType, "_quick"));
        }

        static void RememberArray(object owner, FieldInfo field)
        {
            Array items = field != null ? field.GetValue(owner) as Array : null;
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                Remember(items.GetValue(i));
            }
        }

        static void Remember(object slot)
        {
            if (slot == null)
            {
                return;
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                if (ReferenceEquals(Slots[i].Slot, slot))
                {
                    return;
                }
            }

            Slots.Add(new Tracked { Slot = slot, Item = null });
        }

        static void AfterRefresh(object __instance)
        {
            if (__instance == null || _state == null || _bg == null)
            {
                return;
            }

            Image bg = _bg.GetValue(__instance) as Image;
            if (!bg)
            {
                return;
            }

            Remember(__instance);
            for (int i = 0; i < Slots.Count; i++)
            {
                if (ReferenceEquals(Slots[i].Slot, __instance))
                {
                    Slots[i].Seen = _state.GetValue(__instance) is int n ? n : 0;
                    break;
                }
            }

            Paint(__instance);
        }

        static void Paint(object slot)
        {
            if (slot == null || _state == null || _bg == null)
            {
                return;
            }

            Image bg = _bg.GetValue(slot) as Image;
            if (!bg)
            {
                return;
            }

            int state = _state.GetValue(slot) is int n ? n : 0;
            bool selected = state == EquippedState;
            float spread = SeneaLHudLayoutPlugin.HighlightSize == null ? 0f : SeneaLHudLayoutPlugin.HighlightSize.Value;
            bool glowOn = selected
                && spread > 0.01f
                && SeneaLHudLayoutPlugin.Highlight != null
                && SeneaLHudLayoutPlugin.Highlight.Value;
            bool pip = selected
                && SeneaLHudLayoutPlugin.CornerPip != null
                && SeneaLHudLayoutPlugin.CornerPip.Value;
            Color border = SeneaLHudLayoutPlugin.HighlightColor != null
                ? SeneaLHudLayoutPlugin.HighlightColor.Value
                : Color.white;
            PlaceGlow(bg, glowOn, border, spread);
            PlaceRarity(slot, bg);

            RectTransform host = EnsureHost(bg.transform.parent, bg.rectTransform);
            host.gameObject.SetActive(pip);
            if (!pip)
            {
                return;
            }

            Color check = SeneaLHudLayoutPlugin.CheckColor != null
                ? SeneaLHudLayoutPlugin.CheckColor.Value
                : new Color(0.15f, 0.92f, 0.28f, 1f);
            Rect rect = bg.rectTransform.rect;
            float w = Mathf.Max(8f, rect.width);
            float h = Mathf.Max(8f, rect.height);
            float pipSize = SeneaLHudLayoutPlugin.CornerPipSize == null ? 18f : SeneaLHudLayoutPlugin.CornerPipSize.Value;
            Image mark = PlaceBar(host, "Check", true, check, new Vector2(w * 0.5f - pipSize * 0.55f, h * 0.5f - pipSize * 0.55f), new Vector2(pipSize, pipSize));
            if (mark)
            {
                mark.sprite = CheckSprite();
                mark.rectTransform.localRotation = Quaternion.identity;
            }
        }

        static bool RarityPending(object slot)
        {
            if (SeneaLHudLayoutPlugin.RarityFill == null || !SeneaLHudLayoutPlugin.RarityFill.Value || _bg == null)
            {
                return false;
            }

            Image bg = _bg.GetValue(slot) as Image;
            if (!bg)
            {
                return false;
            }

            Transform plate = bg.transform.parent.Find(RarityName);
            bool plateOn = plate != null && plate.gameObject.activeSelf;
            bool shouldFill = RarityVisible(slot);
            return shouldFill != plateOn;
        }

        static bool RarityVisible(object slot)
        {
            if (SeneaLHudLayoutPlugin.RarityFill == null || !SeneaLHudLayoutPlugin.RarityFill.Value || _rar == null)
            {
                return false;
            }

            float strength = SeneaLHudLayoutPlugin.RarityFillStrength != null
                ? SeneaLHudLayoutPlugin.RarityFillStrength.Value
                : 0.10f;
            if (strength <= 0.001f)
            {
                return false;
            }

            Image icon = _icon != null ? _icon.GetValue(slot) as Image : null;
            if (icon != null && !icon.enabled)
            {
                return false;
            }

            Image rar = _rar.GetValue(slot) as Image;
            return rar != null && rar.enabled && !NearlyWhite(rar.color);
        }

        static void PlaceRarity(object slot, Image bg)
        {
            bool fill = SeneaLHudLayoutPlugin.RarityFill != null && SeneaLHudLayoutPlugin.RarityFill.Value;
            float strength = SeneaLHudLayoutPlugin.RarityFillStrength != null
                ? SeneaLHudLayoutPlugin.RarityFillStrength.Value
                : 0.10f;
            Image rar = _rar != null ? _rar.GetValue(slot) as Image : null;
            Transform existing = bg.transform.parent.Find(RarityName);
            Image plate = existing ? existing.GetComponent<Image>() : null;
            bool magic = RarityVisible(slot);
            Color source = magic ? rar.color : Color.white;

            if (!fill || !magic || strength <= 0.001f)
            {
                if (plate && plate.gameObject.activeSelf)
                {
                    plate.gameObject.SetActive(false);
                }

                if (rar != null && !rar.gameObject.activeSelf)
                {
                    rar.gameObject.SetActive(true);
                }

                return;
            }

            if (rar.gameObject.activeSelf)
            {
                rar.gameObject.SetActive(false);
            }

            if (!plate)
            {
                var go = new GameObject(RarityName, typeof(RectTransform));
                go.transform.SetParent(bg.transform.parent, false);
                plate = go.AddComponent<Image>();
                plate.raycastTarget = false;
                plate.sprite = White();
            }

            Color tint = new Color(source.r, source.g, source.b, strength);
            if (plate.color != tint)
            {
                plate.color = tint;
            }

            if (!plate.gameObject.activeSelf)
            {
                plate.gameObject.SetActive(true);
            }

            RectTransform rt = plate.rectTransform;
            RectTransform sourceRect = bg.rectTransform;
            rt.anchorMin = sourceRect.anchorMin;
            rt.anchorMax = sourceRect.anchorMax;
            rt.pivot = sourceRect.pivot;
            rt.anchoredPosition = sourceRect.anchoredPosition;
            float inset = Mathf.Clamp(Mathf.Min(sourceRect.rect.width, sourceRect.rect.height) * 0.14f, 4f, 10f);
            rt.offsetMin = sourceRect.offsetMin + new Vector2(inset, inset);
            rt.offsetMax = sourceRect.offsetMax - new Vector2(inset, inset);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            int after = sourceRect.GetSiblingIndex() + 1;
            if (rt.parent == sourceRect.parent && rt.GetSiblingIndex() != after)
            {
                rt.SetSiblingIndex(after);
            }
        }

        static bool NearlyWhite(Color color)
        {
            return color.r >= 0.98f && color.g >= 0.98f && color.b >= 0.98f;
        }

        static void PlaceGlow(Image bg, bool on, Color color, float spread)
        {
            Image glow = EnsureImage(bg.transform.parent, GlowName, bg.rectTransform);
            Transform extra = bg.transform.parent.Find(GlowName + "Soft");
            if (extra)
            {
                extra.gameObject.SetActive(false);
            }

            glow.gameObject.SetActive(on);
            if (!on)
            {
                return;
            }

            glow.sprite = bg.sprite;
            glow.type = Image.Type.Simple;
            glow.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * 0.8f));
            RectTransform rt = glow.rectTransform;
            RectTransform source = bg.rectTransform;
            rt.anchorMin = source.anchorMin;
            rt.anchorMax = source.anchorMax;
            rt.pivot = source.pivot;
            rt.anchoredPosition = source.anchoredPosition;
            rt.offsetMin = source.offsetMin - new Vector2(spread, spread);
            rt.offsetMax = source.offsetMax + new Vector2(spread, spread);
            int behind = source.GetSiblingIndex();
            if (rt.GetSiblingIndex() != behind - 1)
            {
                rt.SetSiblingIndex(behind);
            }
        }

        static void HideMark(RectTransform host, string name)
        {
            Transform mark = host.Find(name);
            if (mark && mark.gameObject.activeSelf)
            {
                mark.gameObject.SetActive(false);
            }
        }

        static RectTransform EnsureHost(Transform parent, RectTransform match)
        {
            Transform existing = parent.Find(MarkName);
            RectTransform host = existing as RectTransform;
            if (!host)
            {
                var go = new GameObject(MarkName, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                host = go.GetComponent<RectTransform>();
            }

            host.anchorMin = match.anchorMin;
            host.anchorMax = match.anchorMax;
            host.pivot = match.pivot;
            host.anchoredPosition = match.anchoredPosition;
            host.sizeDelta = match.sizeDelta;
            host.localScale = Vector3.one;
            host.localRotation = Quaternion.identity;
            if (host.GetSiblingIndex() != host.parent.childCount - 1)
            {
                host.SetAsLastSibling();
            }

            return host;
        }

        static Image PlaceBar(RectTransform host, string name, bool on, Color color, Vector2 position, Vector2 size, float angle = 0f)
        {
            Transform existing = host.Find(name);
            Image image = existing ? existing.GetComponent<Image>() : null;
            if (!image)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(host, false);
                image = go.AddComponent<Image>();
                image.raycastTarget = false;
                image.sprite = White();
            }

            image.gameObject.SetActive(on);
            if (!on)
            {
                return image;
            }

            image.color = color;
            RectTransform rt = image.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            return image;
        }

        static Sprite CheckSprite()
        {
            if (_check != null)
            {
                return _check;
            }

            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[n * n];
            Vector2 start = new Vector2(16f, 30f);
            Vector2 corner = new Vector2(27f, 18f);
            Vector2 end = new Vector2(50f, 46f);
            const float radius = 5.4f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = Mathf.Min(SegmentDistance(p, start, corner), SegmentDistance(p, corner, end));
                    float edge = Mathf.InverseLerp(radius + 1.6f, radius - 1.4f, dist);
                    float alpha = edge * edge * (3f - 2f * edge);
                    pixels[y * n + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _check = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
            return _check;
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            return Vector2.Distance(p, a + ab * t);
        }

        static void PlaceOverlay(ItemDrop.ItemData item, float left)
        {
            int seconds = Mathf.CeilToInt(left);
            if (item == null || seconds < 1)
            {
                if (_overlayText)
                {
                    _overlayText.gameObject.SetActive(false);
                }

                return;
            }

            RememberFont();
            EnsureOverlay();
            _overlayText.gameObject.SetActive(true);
            _overlayText.text = seconds.ToString();
            _overlayText.color = Color.white;
            if (_font != null)
            {
                _overlayText.font = _font;
                if (_fontMat != null)
                {
                    _overlayText.fontSharedMaterial = _fontMat;
                }
            }

            Image icon = FindInventoryIcon(item.GetIcon());
            if (!icon
                && SeneaLHudLayoutPlugin.ShowGearBarCue != null
                && SeneaLHudLayoutPlugin.ShowGearBarCue.Value
                && IsWornGear(item))
            {
                icon = FindBarIcon(item.GetIcon());
            }

            if (!icon)
            {
                _overlayText.gameObject.SetActive(false);
                return;
            }

            Vector2 screen = ScreenOf(icon.rectTransform);

            RectTransform rt = _overlayText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(64f, 40f);
            rt.anchoredPosition = screen - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        static bool IsWornGear(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
            {
                return false;
            }

            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Hands:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Utility:
                    return true;
                default:
                    return false;
            }
        }

        static Image FindBarIcon(Sprite sprite)
        {
            if (!sprite)
            {
                return null;
            }

            InventoryGui gui = InventoryGui.instance;
            Image best = null;
            float bestArea = 0f;
            Image[] images = UnityEngine.Object.FindObjectsOfType<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (!image || !image.isActiveAndEnabled || image.sprite != sprite)
                {
                    continue;
                }

                if (_overlay && image.transform.IsChildOf(_overlay.transform))
                {
                    continue;
                }

                if (gui && image.transform.IsChildOf(gui.transform))
                {
                    continue;
                }

                Rect rect = image.rectTransform.rect;
                float area = rect.width * rect.height;
                if (area < 24f * 24f || area <= bestArea)
                {
                    continue;
                }

                best = image;
                bestArea = area;
            }

            return best;
        }

        static Image FindInventoryIcon(Sprite sprite)
        {
            InventoryGui gui = InventoryGui.instance;
            if (!sprite || !gui || !InventoryGui.IsVisible())
            {
                return null;
            }

            Image best = null;
            float bestArea = 0f;
            Image[] images = gui.GetComponentsInChildren<Image>(false);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (!image || !image.isActiveAndEnabled || image.sprite != sprite)
                {
                    continue;
                }

                if (_overlay && image.transform.IsChildOf(_overlay.transform))
                {
                    continue;
                }

                Rect rect = image.rectTransform.rect;
                float area = rect.width * rect.height;
                if (area < 24f * 24f || area <= bestArea)
                {
                    continue;
                }

                best = image;
                bestArea = area;
            }

            return best;
        }

        static Vector2 ScreenOf(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 mid = (corners[0] + corners[2]) * 0.5f;
            Camera cam = null;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            return RectTransformUtility.WorldToScreenPoint(cam, mid);
        }

        static void EnsureOverlay()
        {
            if (_overlayText)
            {
                return;
            }

            var root = new GameObject("SeneaLHudLayout_EquipCountdown");
            UnityEngine.Object.DontDestroyOnLoad(root);
            _overlay = root.AddComponent<Canvas>();
            _overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlay.sortingOrder = 5000;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var textGo = new GameObject("Time", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            _overlayText = textGo.AddComponent<TextMeshProUGUI>();
            _overlayText.alignment = TextAlignmentOptions.Center;
            _overlayText.fontSize = 28f;
            _overlayText.fontStyle = FontStyles.Bold;
            _overlayText.color = Color.white;
            _overlayText.raycastTarget = false;
            _overlayText.textWrappingMode = TextWrappingModes.NoWrap;
            _overlayText.overflowMode = TextOverflowModes.Overflow;
        }

        static void ShowOnIcons(ItemDrop.ItemData item, float left)
        {
            int seconds = Mathf.CeilToInt(left);
            Sprite sprite = item != null ? item.GetIcon() : null;
            var keep = new HashSet<Transform>();
            if (sprite != null && seconds >= 1)
            {
                RememberFont();
                Image[] images = UnityEngine.Object.FindObjectsOfType<Image>();
                for (int i = 0; i < images.Length; i++)
                {
                    Image image = images[i];
                    if (!image || !image.isActiveAndEnabled || image.sprite != sprite)
                    {
                        continue;
                    }

                    if (image.name == GlowName || image.name == WashName || image.name == RarityName)
                    {
                        continue;
                    }

                    Rect rect = image.rectTransform.rect;
                    if (rect.width < 24f || rect.height < 24f)
                    {
                        continue;
                    }

                    TextMeshProUGUI label = EnsureTime(image.transform);
                    ShowSeconds(label, seconds);
                    keep.Add(image.transform);
                }
            }

            for (int i = Labels.Count - 1; i >= 0; i--)
            {
                TextMeshProUGUI label = Labels[i];
                if (!label)
                {
                    Labels.RemoveAt(i);
                    continue;
                }

                if (!keep.Contains(label.transform.parent))
                {
                    label.gameObject.SetActive(false);
                }
            }
        }

        static void RememberFont()
        {
            if (_font != null)
            {
                return;
            }

            TextMeshProUGUI any = UnityEngine.Object.FindObjectOfType<TextMeshProUGUI>();
            if (any != null && any.font != null)
            {
                _font = any.font;
                _fontMat = any.fontSharedMaterial;
            }
        }

        static void PaintAction(object slot, bool on, int kind, float left)
        {
            RectTransform root = _root != null ? _root.GetValue(slot) as RectTransform : null;
            if (!root)
            {
                return;
            }

            HideWash(root);
            TextMeshProUGUI time = EnsureTime(root);
            if (!on)
            {
                time.gameObject.SetActive(false);
                return;
            }

            TMP_Text fontFrom = _count != null ? _count.GetValue(slot) as TMP_Text : null;
            ShowSeconds(time, left, fontFrom);
        }

        static void TryAction(Player player, out ItemDrop.ItemData item, out int kind, out float left)
        {
            item = null;
            kind = -1;
            left = 0f;
            if (_progress == null)
            {
                return;
            }

            System.Collections.IList queue = _queue != null ? _queue.GetValue(player) as System.Collections.IList : null;
            if (queue == null || queue.Count == 0)
            {
                return;
            }

            object data = queue[0];
            if (data == null || _actionItem == null)
            {
                return;
            }

            item = _actionItem.GetValue(data) as ItemDrop.ItemData;
            kind = _actionType != null && _actionType.GetValue(data) != null ? Convert.ToInt32(_actionType.GetValue(data)) : 0;
            float duration = _actionDuration != null && _actionDuration.GetValue(data) is float d ? d : 0f;
            float time = _actionTime != null && _actionTime.GetValue(data) is float t ? t : 0f;
            left = Mathf.Max(0f, duration - time);
        }

        static void PaintGrids(ItemDrop.ItemData busy, int kind, float left)
        {
            InventoryGui gui = InventoryGui.instance;
            if (!gui)
            {
                return;
            }

            PaintGrid(_playerGrid != null ? _playerGrid.GetValue(gui) as InventoryGrid : null, busy, kind, left);
            PaintGrid(_containerGrid != null ? _containerGrid.GetValue(gui) as InventoryGrid : null, busy, kind, left);
        }

        static void PaintGrid(InventoryGrid grid, ItemDrop.ItemData busy, int kind, float left)
        {
            System.Collections.IList elements = _elements != null ? _elements.GetValue(grid) as System.Collections.IList : null;
            if (!grid || elements == null)
            {
                return;
            }

            Inventory inventory = grid.GetInventory();
            for (int i = 0; i < elements.Count; i++)
            {
                InventoryElement element = elements[i] as InventoryElement;
                if (!element || !element.m_icon)
                {
                    continue;
                }

                ItemDrop.ItemData item = inventory != null ? inventory.GetItemAt(element.Position.x, element.Position.y) : null;
                bool match = busy != null && item != null && ReferenceEquals(item, busy);
                if (!match && item != null && Player.m_localPlayer != null && Player.m_localPlayer.IsEquipActionQueued(item))
                {
                    match = true;
                }

                RectTransform host = element.transform as RectTransform;
                HideWash(host);
                Transform timeT = host != null ? host.Find(TimeName) : null;
                if (!match)
                {
                    if (timeT && timeT.gameObject.activeSelf)
                    {
                        timeT.gameObject.SetActive(false);
                    }

                    continue;
                }

                if (!host)
                {
                    continue;
                }

                ShowSeconds(EnsureTime(host), left, element.m_amount);
            }
        }

        static void HideWash(Transform parent)
        {
            Transform wash = parent != null ? parent.Find(WashName) : null;
            if (wash && wash.gameObject.activeSelf)
            {
                wash.gameObject.SetActive(false);
            }
        }

        static void ShowSeconds(TextMeshProUGUI time, int seconds)
        {
            ShowSeconds(time, seconds, null);
        }

        static void ShowSeconds(TextMeshProUGUI time, float left, TMP_Text fontFrom)
        {
            int seconds = Mathf.CeilToInt(left);
            bool show = seconds >= 1;
            time.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            if (fontFrom != null && fontFrom.font != null)
            {
                time.font = fontFrom.font;
                if (fontFrom.fontSharedMaterial != null)
                {
                    time.fontSharedMaterial = fontFrom.fontSharedMaterial;
                }
            }
            else if (_font != null)
            {
                time.font = _font;
                if (_fontMat != null)
                {
                    time.fontSharedMaterial = _fontMat;
                }
            }

            time.text = seconds.ToString();
            time.color = Color.white;
            time.fontSize = 22f;
            time.fontStyle = FontStyles.Bold;
            time.alignment = TextAlignmentOptions.Center;
            RectTransform rt = time.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }

        static void CopyRect(RectTransform rt, RectTransform match)
        {
            rt.anchorMin = match.anchorMin;
            rt.anchorMax = match.anchorMax;
            rt.pivot = match.pivot;
            rt.offsetMin = match.offsetMin;
            rt.offsetMax = match.offsetMax;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        static TextMeshProUGUI EnsureTime(Transform parent)
        {
            Transform existing = parent.Find(TimeName);
            TextMeshProUGUI label = existing ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (!label)
            {
                var go = new GameObject(TimeName, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                label = go.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 22f;
                label.fontStyle = FontStyles.Bold;
                label.raycastTarget = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.color = Color.white;
            }

            if (!Labels.Contains(label))
            {
                Labels.Add(label);
            }

            return label;
        }

        static Image EnsureImage(Transform parent, string name, RectTransform match)
        {
            Transform existing = parent.Find(name);
            Image image = existing ? existing.GetComponent<Image>() : null;
            if (!image)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                image = go.AddComponent<Image>();
                image.raycastTarget = false;
                image.sprite = White();
            }

            RectTransform rt = image.rectTransform;
            rt.anchorMin = match.anchorMin;
            rt.anchorMax = match.anchorMax;
            rt.pivot = match.pivot;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            return image;
        }

        static Sprite White()
        {
            if (_white != null)
            {
                return _white;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color[] pixels = { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(pixels);
            tex.Apply();
            _white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            return _white;
        }

        static bool Ready()
        {
            if (_ready)
            {
                return _slotType != null;
            }

            _ready = true;
            if (_slotType == null && !_logged)
            {
                _logged = true;
                Debug.Log("[SeneaL HUD Layout] Slot cues skipped. SeneaL UI slots were not found.");
            }

            return _slotType != null;
        }
    }
}
