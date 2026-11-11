using System;
using System.Reflection;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class VneiSearch
    {
        const string ButtonName = "SeneaLHudLayout_ItemSearch";

        static RectTransform _craft;
        static Button _button;
        static TextMeshProUGUI _label;
        static Sprite _pixel;
        static FieldInfo _isOpen;
        static Type _uiType;
        static MethodInfo _setVisibility;
        static Component _ui;
        static bool _bound;
        static bool _missing;
        static bool _inventoryWasOpen;
        static bool _open;
        static bool _aligned;

        // 1.5.52 FPS: three sites below (Bind's assembly loop, Window's FindObjectsOfTypeAll,
        // EnsureButton's whole-GUI Transform scan) cache STAYS NULL whenever the VNEI window or
        // the craft node is absent - which is every frame outside the inventory - and re-ran the
        // full scan per frame. Same negative-cache shape as PieceHealthLabel: heavy probes retry
        // on a 2s cadence, and a found window/craft keeps its cache for the session (destroyed
        // objects read null through Unity's ==, so the cache self-heals when the GUI rebuilds).
        static float _nextHeavyScan;

        public static void Tick()
        {
            if (!Bind())
            {
                return;
            }

            bool inventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            if (_inventoryWasOpen && !inventoryOpen)
            {
                _open = false;
            }

            _inventoryWasOpen = inventoryOpen;
            if (!_open)
            {
                _aligned = false;
            }

            ApplyWindow();

            bool show = SeneaLHudLayoutPlugin.ShowItemSearch != null && SeneaLHudLayoutPlugin.ShowItemSearch.Value;
            if (!show || !EnsureButton())
            {
                if (_button != null && _button.gameObject.activeSelf)
                {
                    _button.gameObject.SetActive(false);
                }

                return;
            }

            string text = _open ? "Close" : "VNEI";
            EnsureFont();
            ApplySize(text);

            if (!_button.gameObject.activeSelf)
            {
                _button.gameObject.SetActive(true);
            }

            if (_open && !_aligned && AlignToButton(Window()))
            {
                _aligned = true;
            }
        }

        // 1.5.55: the button width is measured from the LABEL's text, so it is only correct when
        // the measurement ran with the FINAL font. The font is sampled from the craft panel title
        // ("orn/t"), and on a fast Tab-open the title's font is not resolved yet when the button
        // is created - the label keeps TMP's default face, which lays out WIDER, and the button
        // landed "bigger than it should be" (the user's report) and stayed that size, because the
        // old code re-measured ONLY when the text changed. So: (EnsureFont) the craft-title font is
        // re-checked live and applied the moment it becomes available; (ApplySize) the button is
        // (re)measured when the text changes, when the font changed since the last measurement, or
        // when the measured size drifted. Text/font are stable 99.9% of frames, so the per-frame
        // cost is two comparisons.
        static string _sizedText;
        static int _sizedFontId;
        static float _sizedWidth = float.NaN;

        static void EnsureFont()
        {
            if (_label == null || _craft == null)
            {
                return;
            }

            Transform title = _craft.Find("orn/t");
            TextMeshProUGUI sample = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            if (sample == null || sample.font == null)
            {
                return;
            }

            if (_label.font != sample.font)
            {
                _label.font = sample.font;
                if (sample.fontSharedMaterial != null)
                {
                    _label.fontSharedMaterial = sample.fontSharedMaterial;
                }
            }
        }

        static void ApplySize(string text)
        {
            if (_label == null || _button == null)
            {
                return;
            }

            RectTransform rt = _button.GetComponent<RectTransform>();
            int fontId = _label.font != null ? _label.font.GetInstanceID() : 0;
            if (_sizedText == text && _sizedFontId == fontId && !float.IsNaN(_sizedWidth)
                && Mathf.Abs(rt.sizeDelta.x - _sizedWidth) < 0.5f)
            {
                return;
            }

            _label.text = text;
            _label.ForceMeshUpdate();
            float width = Mathf.Max(54f, Mathf.Ceil(_label.preferredWidth) + 16f);
            rt.sizeDelta = new Vector2(width, 26f);
            _sizedText = text;
            _sizedFontId = fontId;
            _sizedWidth = width;
        }

        static bool Bind()
        {
            if (_bound)
            {
                return _setVisibility != null;
            }

            if (_missing)
            {
                return false;
            }

            // 1.5.52 FPS: this loop walks every loaded assembly (~120 plugins) and calls
            // GetName() on each. It used to run every frame until InventoryGui.instance exists
            // to declare VNEI missing. Retry on the shared 2s cadence instead.
            if (Time.unscaledTime < _nextHeavyScan)
            {
                return false;
            }

            _nextHeavyScan = Time.unscaledTime + 2f;

            Assembly assembly = null;
            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i].GetName().Name == "VNEI")
                {
                    assembly = loaded[i];
                    break;
                }
            }

            if (assembly == null)
            {
                if (InventoryGui.instance != null)
                {
                    _missing = true;
                }

                return false;
            }

            Type plugin = assembly.GetType("VNEI.Plugin");
            _uiType = assembly.GetType("VNEI.UI.BaseUI");
            _isOpen = plugin != null ? plugin.GetField("isUiOpen", BindingFlags.Public | BindingFlags.Static) : null;
            _setVisibility = _uiType != null ? _uiType.GetMethod("SetVisibility") : null;
            _bound = true;
            if (_isOpen == null || _setVisibility == null)
            {
                _missing = true;
                return false;
            }

            DetachFromCrafting(plugin);
            return true;
        }

        static void DetachFromCrafting(Type plugin)
        {
            FieldInfo attachField = plugin.GetField("attachToCrafting", BindingFlags.Public | BindingFlags.Static);
            FieldInfo hideField = plugin.GetField("hideUIAtStartup", BindingFlags.Public | BindingFlags.Static);
            ConfigEntry<bool> attach = attachField != null ? attachField.GetValue(null) as ConfigEntry<bool> : null;
            ConfigEntry<bool> hide = hideField != null ? hideField.GetValue(null) as ConfigEntry<bool> : null;
            _isOpen.SetValue(null, false);
            _open = false;
            if (attach != null && attach.Value)
            {
                attach.Value = false;
            }

            if (hide != null && !hide.Value)
            {
                hide.Value = true;
            }

            _isOpen.SetValue(null, false);
            ApplyWindow();
        }

        static void OnButton()
        {
            _open = !_open;
            ApplyWindow();
        }

        static void ApplyWindow()
        {
            Component ui = Window();
            if (ui == null)
            {
                return;
            }

            _isOpen.SetValue(null, _open);
            if (_open)
            {
                BringForward(ui);
            }

            _setVisibility.Invoke(ui, new object[] { _open });
        }

        static Component Window()
        {
            if (_ui != null)
            {
                return _ui;
            }

            if (_uiType == null)
            {
                return null;
            }

            if (Time.unscaledTime < _nextHeavyScan)
            {
                return null;
            }

            _nextHeavyScan = Time.unscaledTime + 2f;

            UnityEngine.Object[] found = Resources.FindObjectsOfTypeAll(_uiType);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] is Component component && component.gameObject.scene.IsValid())
                {
                    _ui = component;
                    return _ui;
                }
            }

            return null;
        }

        static void BringForward(Component ui)
        {
            Canvas host = _craft != null ? _craft.GetComponentInParent<Canvas>() : null;
            Transform parent = host != null ? host.transform : ui.transform.parent;
            if (parent != null && ui.transform.parent != parent)
            {
                ui.transform.SetParent(parent, false);
            }

            Canvas own = ui.GetComponent<Canvas>();
            if (own == null)
            {
                own = ui.gameObject.AddComponent<Canvas>();
            }

            own.overrideSorting = true;
            own.sortingOrder = (host != null ? host.sortingOrder : 0) + 30;
            if (ui.GetComponent<GraphicRaycaster>() == null)
            {
                ui.gameObject.AddComponent<GraphicRaycaster>();
            }

            ui.transform.SetAsLastSibling();
        }

        static bool AlignToButton(Component ui)
        {
            if (ui == null || _button == null)
            {
                return false;
            }

            RectTransform window = ui.transform as RectTransform;
            FieldInfo dragField = ui.GetType().GetField("dragHandler");
            RectTransform frame = dragField != null ? dragField.GetValue(ui) as RectTransform : null;
            if (frame == null)
            {
                frame = window;
            }

            if (window == null || frame == null || frame.rect.width < 8f)
            {
                return false;
            }

            Vector3[] buttonCorners = new Vector3[4];
            _button.GetComponent<RectTransform>().GetWorldCorners(buttonCorners);
            Vector3[] frameCorners = new Vector3[4];
            frame.GetWorldCorners(frameCorners);
            window.position += buttonCorners[3] - frameCorners[2];
            return true;
        }

        static bool EnsureButton()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
            {
                return false;
            }

            if (_craft == null || _button == null)
            {
                if (Time.unscaledTime < _nextHeavyScan)
                {
                    return false;
                }

                _craft = FindCraft(gui);
                if (_craft == null)
                {
                    return false;
                }

                Transform existing = _craft.Find(ButtonName);
                if (existing != null)
                {
                    _button = existing.GetComponent<Button>();
                    _label = existing.GetComponentInChildren<TextMeshProUGUI>();
                }

                if (_button == null || _label == null)
                {
                    if (existing != null)
                    {
                        UnityEngine.Object.Destroy(existing.gameObject);
                    }

                    GameObject go = new GameObject(ButtonName);
                    go.transform.SetParent(_craft, false);
                    Image plate = go.AddComponent<Image>();
                    plate.sprite = Pixel();
                    plate.color = new Color(0.07f, 0.05f, 0.03f, 0.72f);
                    plate.raycastTarget = true;
                    _button = go.AddComponent<Button>();
                    _button.targetGraphic = plate;
                    _button.onClick.AddListener(OnButton);

                    GameObject textGo = new GameObject("t");
                    textGo.transform.SetParent(go.transform, false);
                    _label = textGo.AddComponent<TextMeshProUGUI>();
                    _label.raycastTarget = false;
                    _label.alignment = TextAlignmentOptions.Center;
                    _label.textWrappingMode = TextWrappingModes.NoWrap;
                    _label.overflowMode = TextOverflowModes.Overflow;
                    _label.fontSize = 14f;
                    _label.fontStyle = FontStyles.Bold;
                    _label.color = new Color(0.93f, 0.82f, 0.52f, 1f);
                    _label.text = "VNEI";
                    RectTransform textRt = _label.rectTransform;
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = new Vector2(6f, 0f);
                    textRt.offsetMax = new Vector2(-6f, 0f);

                    TextMeshProUGUI sample = null;
                    Transform title = _craft.Find("orn/t");
                    if (title != null)
                    {
                        sample = title.GetComponent<TextMeshProUGUI>();
                    }

                    if (sample != null && sample.font != null)
                    {
                        _label.font = sample.font;
                        if (sample.fontSharedMaterial != null)
                        {
                            _label.fontSharedMaterial = sample.fontSharedMaterial;
                        }
                    }
                    else if (TMP_Settings.defaultFontAsset != null)
                    {
                        _label.font = TMP_Settings.defaultFontAsset;
                    }
                }
            }

            RectTransform rt = _button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -7f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            return true;
        }

        static RectTransform FindCraft(InventoryGui gui)
        {
            Transform[] transforms = gui.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate.name != "craft" || candidate.Find("search") == null || candidate.Find("orn") == null)
                {
                    continue;
                }

                return candidate as RectTransform;
            }

            return null;
        }

        static Sprite Pixel()
        {
            if (_pixel != null)
            {
                return _pixel;
            }

            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _pixel = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return _pixel;
        }
    }
}
