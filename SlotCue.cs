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
        const string WashName = "SeneaLHudLayout_Equip";
        const string TimeName = "SeneaLHudLayout_EquipTime";
        const int EquippedState = 2;

        static readonly List<Tracked> Slots = new List<Tracked>();
        static Type _slotType;
        static FieldInfo _state;
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
        static TMP_FontAsset _font;
        static Material _fontMat;
        static readonly List<TextMeshProUGUI> Labels = new List<TextMeshProUGUI>();
        static ItemDrop.ItemData _trackedItem;
        static float _trackedEnd;
        static Canvas _overlay;
        static TextMeshProUGUI _overlayText;
        static bool _ready;
        static bool _logged;

        class Tracked
        {
            public object Slot;
            public ItemDrop.ItemData Item;
        }

        public static void Apply(Harmony harmony)
        {
            _slotType = AccessTools.TypeByName("SeneaLUI.Components.Slot");
            if (_slotType == null)
            {
                return;
            }

            _state = AccessTools.Field(_slotType, "_state");
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
            if (SeneaLHudLayoutPlugin.ShowEquipCue != null
                && SeneaLHudLayoutPlugin.ShowEquipCue.Value
                && Player.m_localPlayer != null)
            {
                TryAction(Player.m_localPlayer, out busy, out kind, out left);
            }

            if ((busy == null || left <= 0.05f) && _trackedItem != null && Time.time < _trackedEnd)
            {
                busy = _trackedItem;
                left = _trackedEnd - Time.time;
            }

            if (busy == null || left <= 0.05f)
            {
                _trackedItem = null;
            }

            PlaceOverlay(busy, left);
        }

        static void AfterQueued(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
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

            int state = _state.GetValue(__instance) is int n ? n : 0;
            float extra = SeneaLHudLayoutPlugin.SelectionGlow == null ? 0f : SeneaLHudLayoutPlugin.SelectionGlow.Value;
            bool on = state == EquippedState && extra > 0.01f;
            Image glow = EnsureImage(bg.transform.parent, GlowName, bg.rectTransform);
            glow.gameObject.SetActive(on);
            if (!on)
            {
                return;
            }

            glow.sprite = bg.sprite;
            glow.type = Image.Type.Simple;
            glow.color = new Color(0.28f, 0.62f, 1f, Mathf.Clamp01(0.4f + extra * 0.35f));
            RectTransform rt = glow.rectTransform;
            float grow = 1f + extra * 1.5f;
            rt.anchoredPosition = bg.rectTransform.anchoredPosition;
            rt.sizeDelta = bg.rectTransform.sizeDelta + new Vector2(grow, grow) * 2f;
            rt.SetSiblingIndex(bg.transform.GetSiblingIndex() + 1);
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

            Vector2 screen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Image icon = FindIcon(item.GetIcon());
            if (icon)
            {
                screen = ScreenOf(icon.rectTransform);
            }

            RectTransform rt = _overlayText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(64f, 40f);
            rt.anchoredPosition = screen - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        static Image FindIcon(Sprite sprite)
        {
            if (!sprite)
            {
                return null;
            }

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

                if (image.transform.IsChildOf(_overlay.transform))
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

                    if (image.name == GlowName || image.name == WashName)
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
