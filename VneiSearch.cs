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
            if (_label.text != text)
            {
                _label.text = text;
            }

            _label.ForceMeshUpdate();
            float width = Mathf.Ceil(_label.preferredWidth) + 16f;
            _button.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(54f, width), 26f);

            if (!_button.gameObject.activeSelf)
            {
                _button.gameObject.SetActive(true);
            }

            if (_open && !_aligned && AlignToButton(Window()))
            {
                _aligned = true;
            }
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
            rt.anchoredPosition = new Vector2(-16f, -14f);
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
