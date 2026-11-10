using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Live character sheet opened from a SeneaL character tab (shield / person icon).
    /// </summary>
    static class StatsWindow
    {
        const string TabId = "cjay_stats";
        const string WindowName = "SeneaLHudLayout_Stats";
        const string PrefRect = "SeneaLHudLayout_StatsRect";
        const float HeaderPadExtra = 25f;
        const float HeaderPortraitLeftPad = 14f;
        const float PortraitOuter = 45.6f;
        const float HeaderBodyHeight = 118f;
        const float HeaderRuleHeight = 1f;
        const float HeaderBottomGap = 6f;
        const float SectionFontSize = 18f;
        const float SectionIconPx = 14f;
        static readonly Color ScrollTrackFill = new Color(0.07f, 0.05f, 0.03f, 0.92f);
        static readonly Color HeaderFill = ScrollTrackFill;
        static readonly Color TitleFill = ScrollTrackFill;
        static readonly Color SectionTitleColor = new Color(0.9f, 0.78f, 0.48f, 1f);
        static readonly Color SectionIconColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        static readonly FieldInfo StatusEffectTimeField = AccessTools.Field(typeof(StatusEffect), "m_time");
        static readonly FieldInfo GuardianPowerField = AccessTools.Field(typeof(Player), "m_guardianPower");
        static readonly Type PassiveModType = ModTypes.Find("PassivePowers", "PassivePowers.PassivePowers");
        // Passive Powers exposes these as public static readonly fields, not properties.
        static readonly FieldInfo PassiveBossConfigs = PassiveModType != null
            ? AccessTools.Field(PassiveModType, "activeBossConfigs")
            : null;
        static readonly FieldInfo PassiveMaxPowers = PassiveModType != null
            ? AccessTools.Field(PassiveModType, "maximumBossPowers")
            : null;
        static float HeaderStackHeight => HeaderBodyHeight + HeaderRuleHeight + HeaderBottomGap;

        static GameObject _window;
        static RectTransform _root;
        static RectTransform _titleBar;
        static RectTransform _headerBlock;
        static RectTransform _headerRule;
        static RectTransform _list;
        static ScrollRect _scroll;
        static Transform _fontTabs;
        static TMP_Text _panelTitleSample;
        static TMP_FontAsset _bodyFont;
        static TMP_FontAsset _titleFont;

        static readonly Skills.SkillType[] AllWeaponSkills =
        {
            Skills.SkillType.Swords,
            Skills.SkillType.Knives,
            Skills.SkillType.Clubs,
            Skills.SkillType.Polearms,
            Skills.SkillType.Spears,
            Skills.SkillType.Axes,
            Skills.SkillType.Bows,
            Skills.SkillType.Crossbows,
            Skills.SkillType.Unarmed,
            Skills.SkillType.ElementalMagic,
            Skills.SkillType.BloodMagic
        };
        static TextMeshProUGUI _title;
        static TextMeshProUGUI _headerName;
        static TextMeshProUGUI _headerSpec;
        static RawImage _portrait;
        static Texture2D _portraitTex;
        static bool _open;
        static bool _registered;
        static float _nextRefresh;
        static bool _dragging;
        static Vector2 _dragOffset;
        static object _tab;

        enum RowKind
        {
            Header,
            Data
        }

        struct StatRow
        {
            public RowKind Kind;
            public string Label;
            public string Value;
            public string Icon;
        }

        static readonly List<StatRow> _rows = new List<StatRow>();

        public static void Apply(Harmony harmony)
        {
            TryRegisterTab();
            Type tabsView = AccessTools.TypeByName("SeneaLUI.Inv.TabsView");
            MethodInfo layout = tabsView != null ? AccessTools.Method(tabsView, "Layout") : null;
            if (layout != null)
            {
                harmony.Patch(layout, postfix: new HarmonyMethod(typeof(StatsWindow), nameof(AfterTabsLayout)));
            }
        }

        static void AfterTabsLayout(object __instance)
        {
            if (!Enabled() || _tab == null)
            {
                return;
            }

            FieldInfo orderField = AccessTools.Field(__instance.GetType(), "_order");
            if (orderField == null)
            {
                return;
            }

            object orderObj = orderField.GetValue(__instance);
            if (!(orderObj is IList<int> order))
            {
                return;
            }

            int extIndex = TabExtIndex();
            if (extIndex < 0)
            {
                return;
            }

            int id = -(extIndex + 1);
            order.Remove(id);
            int insertAt = 0;
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i] >= 0)
                {
                    insertAt = i;
                    break;
                }

                insertAt = i + 1;
            }

            order.Insert(Mathf.Clamp(insertAt, 0, order.Count), id);
        }

        static int TabExtIndex()
        {
            Type tabType = AccessTools.TypeByName("SeneaL.API.CharacterTab");
            if (tabType == null || _tab == null)
            {
                return -1;
            }

            PropertyInfo idProp = AccessTools.Property(tabType, "Id");
            string id = idProp?.GetValue(_tab) as string;
            if (id != TabId)
            {
                return -1;
            }

            Type tabsType = AccessTools.TypeByName("SeneaL.API.CharacterTabs");
            PropertyInfo allProp = tabsType != null ? AccessTools.Property(tabsType, "All") : null;
            if (allProp == null)
            {
                return -1;
            }

            object all = allProp.GetValue(null, null);
            if (!(all is System.Collections.IList list))
            {
                return -1;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == _tab)
                {
                    return i;
                }
            }

            return -1;
        }

        static bool Enabled()
        {
            return SeneaLHudLayoutPlugin.StatsTab != null && SeneaLHudLayoutPlugin.StatsTab.Value;
        }

        static void TryRegisterTab()
        {
            if (_registered || !Enabled())
            {
                return;
            }

            Type tabsType = AccessTools.TypeByName("SeneaL.API.CharacterTabs");
            Type tabType = AccessTools.TypeByName("SeneaL.API.CharacterTab");
            if (tabsType == null || tabType == null)
            {
                return;
            }

            MethodInfo add = AccessTools.Method(tabsType, "Add", new[]
            {
                typeof(string),
                typeof(string),
                typeof(Func<string>),
                typeof(Action)
            });
            if (add == null)
            {
                return;
            }

            Func<string> title = () => "Stats";
            Action open = () =>
            {
                if (!Enabled())
                {
                    return;
                }

                EnsureWindow();
                ApplyPanelChrome();
                _open = true;
                _window.SetActive(true);
                _window.transform.SetAsLastSibling();
                ApplySavedRect();
                Refresh(true);
            };

            try
            {
                _tab = add.Invoke(null, new object[] { SeneaLHudLayoutPlugin.GUID, TabId, title, open });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SeneaL HUD Layout] Could not register Stats tab: " + e.Message);
                return;
            }

            if (_tab == null)
            {
                return;
            }

            SetProp(tabType, "IconName", "user-circle");
            SetProp(tabType, "Order", 12);
            SetProp(tabType, "Tip", (Func<string>)(() => "Vitals, defense, buffs, and skills"));
            SetProp(tabType, "Visible", (Func<bool>)(() => Enabled()));
            SetProp(tabType, "Active", (Func<bool>)(() => _open && _window != null && _window.activeSelf));
            _registered = true;
        }

        static void SetProp(Type tabType, string name, object value)
        {
            PropertyInfo prop = AccessTools.Property(tabType, name);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(_tab, value, null);
            }
        }

        public static void Tick()
        {
            if (!Enabled())
            {
                if (_window != null && _window.activeSelf)
                {
                    _window.SetActive(false);
                }

                _open = false;
                return;
            }

            if (!_registered)
            {
                TryRegisterTab();
            }

            KeyboardShortcut hotkey = SeneaLHudLayoutPlugin.StatsHotkey != null
                ? SeneaLHudLayoutPlugin.StatsHotkey.Value
                : default;
            if (hotkey.MainKey != KeyCode.None && hotkey.IsDown())
            {
                if (_open && _window != null && _window.activeSelf)
                {
                    Close();
                }
                else if (InventoryGui.IsVisible())
                {
                    EnsureWindow();
                    ApplyPanelChrome();
                    _open = true;
                    _window.SetActive(true);
                    _window.transform.SetAsLastSibling();
                    ApplySavedRect();
                    Refresh(true);
                }
            }

            bool inventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            if (_window != null && _window.activeSelf && !inventoryOpen)
            {
                Close();
            }

            if (_window == null || !_window.activeSelf)
            {
                return;
            }

            DragTick();
            if (Time.unscaledTime >= _nextRefresh)
            {
                Refresh(false);
            }
        }

        static void Close()
        {
            _open = false;
            if (_window != null)
            {
                SaveRect();
                _window.SetActive(false);
            }
        }

        static void DragTick()
        {
            if (_titleBar == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(_titleBar, Input.mousePosition))
            {
                _dragging = true;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root.parent as RectTransform, Input.mousePosition, null, out _dragOffset);
                _dragOffset -= _root.anchoredPosition;
            }

            if (_dragging)
            {
                if (!Input.GetMouseButton(0))
                {
                    _dragging = false;
                    SaveRect();
                    return;
                }

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_root.parent as RectTransform, Input.mousePosition, null, out Vector2 local))
                {
                    _root.anchoredPosition = local - _dragOffset;
                }
            }
        }

        static void EnsureWindow()
        {
            if (_window != null || InventoryGui.instance == null)
            {
                return;
            }

            Transform parent = InventoryGui.instance.transform;
            var go = new GameObject(WindowName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(420f, 520f);
            var border = go.GetComponent<Image>();
            border.sprite = CharacterExtras.Plate();
            border.color = new Color(0.62f, 0.48f, 0.28f, 1f);
            border.raycastTarget = true;
            _window = go;

            var fillGo = new GameObject("fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(_root, false);
            fillGo.transform.SetAsFirstSibling();
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = CharacterExtras.Plate();
            fill.color = StatsBodyFill();
            fill.raycastTarget = true;

            _titleBar = new GameObject("titleBar", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _titleBar.SetParent(_root, false);
            _titleBar.anchorMin = new Vector2(0f, 1f);
            _titleBar.anchorMax = new Vector2(1f, 1f);
            _titleBar.pivot = new Vector2(0.5f, 1f);
            _titleBar.sizeDelta = new Vector2(0f, 36f);
            _titleBar.anchoredPosition = Vector2.zero;
            var titleBg = _titleBar.GetComponent<Image>();
            titleBg.color = TitleFill;
            titleBg.raycastTarget = true;

            var titleGo = new GameObject("t", typeof(RectTransform));
            titleGo.transform.SetParent(_titleBar, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(12f, 0f);
            titleRt.offsetMax = new Vector2(-36f, 0f);
            _title = titleGo.AddComponent<TextMeshProUGUI>();
            _title.fontSize = 16f;
            _title.fontStyle = FontStyles.Bold;
            _title.alignment = TextAlignmentOptions.MidlineLeft;
            _title.color = new Color(0.93f, 0.82f, 0.52f, 1f);
            _title.text = "Character Stats";

            AddCloseButton(_root);

            BuildHeader(_root);
            BuildHeaderRule(_root);

            var viewportGo = new GameObject("view", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(_root, false);
            var viewport = (RectTransform)viewportGo.transform;
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(10f, 10f);
            viewport.offsetMax = new Vector2(-28f, -(36f + HeaderStackHeight));
            viewportGo.GetComponent<Image>().color = StatsBodyFill();
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("list", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewport, false);
            _list = (RectTransform)contentGo.transform;
            _list.anchorMin = new Vector2(0f, 1f);
            _list.anchorMax = new Vector2(1f, 1f);
            _list.pivot = new Vector2(0.5f, 1f);
            _list.anchoredPosition = Vector2.zero;
            _list.sizeDelta = new Vector2(0f, 0f);
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 1f;
            layout.padding = new RectOffset(4, 4, 6, 10);
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll = viewportGo.AddComponent<ScrollRect>();
            _scroll.viewport = viewport;
            _scroll.content = _list;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 48f;
            _scroll.verticalScrollbar = AddScrollbar(_root);
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            ApplyTitleFont(_title);
            ApplyBodyFont(_headerName);
            ApplyBodyFont(_headerSpec);
            var closeLabel = _root.Find("close/x")?.GetComponent<TextMeshProUGUI>();
            if (closeLabel != null)
            {
                ApplyBodyFont(closeLabel);
            }

            go.SetActive(false);
            ApplySavedRect();
            ApplyPanelChrome();
        }

        static void BuildHeader(RectTransform root)
        {
            var headerGo = new GameObject("header", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerGo.transform.SetParent(root, false);
            _headerBlock = (RectTransform)headerGo.transform;
            var headerBg = headerGo.GetComponent<Image>();
            headerBg.color = HeaderFill;
            headerBg.raycastTarget = false;
            _headerBlock.anchorMin = new Vector2(0f, 1f);
            _headerBlock.anchorMax = new Vector2(1f, 1f);
            _headerBlock.pivot = new Vector2(0.5f, 1f);
            _headerBlock.anchoredPosition = new Vector2(0f, -36f);
            _headerBlock.sizeDelta = new Vector2(0f, HeaderBodyHeight);
            var headerLayout = headerGo.GetComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(
                (int)HeaderPortraitLeftPad,
                4,
                (int)(6f + HeaderPadExtra),
                (int)(4f + HeaderPadExtra));
            headerLayout.spacing = 12f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            headerGo.GetComponent<LayoutElement>().minHeight = HeaderBodyHeight;

            var frameGo = new GameObject("portraitFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            frameGo.transform.SetParent(_headerBlock, false);
            var frameRt = (RectTransform)frameGo.transform;
            frameRt.sizeDelta = new Vector2(PortraitOuter, PortraitOuter);
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.sprite = CharacterExtras.Plate();
            frameImg.color = new Color(0.72f, 0.52f, 0.22f, 1f);
            var frameLe = frameGo.GetComponent<LayoutElement>();
            frameLe.preferredWidth = PortraitOuter;
            frameLe.preferredHeight = PortraitOuter;
            frameLe.minWidth = PortraitOuter;
            frameLe.minHeight = PortraitOuter;
            frameLe.flexibleWidth = 0f;
            frameLe.flexibleHeight = 0f;

            var portraitGo = new GameObject("portrait", typeof(RectTransform), typeof(RawImage));
            portraitGo.transform.SetParent(frameRt, false);
            var portraitRt = (RectTransform)portraitGo.transform;
            portraitRt.anchorMin = Vector2.zero;
            portraitRt.anchorMax = Vector2.one;
            portraitRt.offsetMin = new Vector2(3f, 3f);
            portraitRt.offsetMax = new Vector2(-3f, -3f);
            _portrait = portraitGo.GetComponent<RawImage>();
            _portrait.color = Color.white;
            _portrait.raycastTarget = false;
            var fit = portraitGo.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1f;

            var textCol = new GameObject("textCol", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textCol.transform.SetParent(_headerBlock, false);
            var textLayout = textCol.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childAlignment = TextAnchor.UpperLeft;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;
            textCol.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var nameGo = new GameObject("name", typeof(RectTransform));
            nameGo.transform.SetParent(textCol.transform, false);
            _headerName = nameGo.AddComponent<TextMeshProUGUI>();
            _headerName.fontSize = 20f;
            _headerName.fontStyle = FontStyles.Bold;
            _headerName.alignment = TextAlignmentOptions.TopLeft;
            _headerName.color = new Color(0.96f, 0.88f, 0.58f, 1f);
            _headerName.text = "—";
            nameGo.AddComponent<LayoutElement>().minHeight = 26f;

            var specGo = new GameObject("spec", typeof(RectTransform));
            specGo.transform.SetParent(textCol.transform, false);
            _headerSpec = specGo.AddComponent<TextMeshProUGUI>();
            _headerSpec.fontSize = 14f;
            _headerSpec.fontStyle = FontStyles.Italic;
            _headerSpec.alignment = TextAlignmentOptions.TopLeft;
            _headerSpec.color = new Color(0.92f, 0.62f, 0.28f, 1f);
            _headerSpec.text = "";
            specGo.AddComponent<LayoutElement>().minHeight = 20f;
        }

        static void BuildHeaderRule(RectTransform root)
        {
            var ruleGo = new GameObject("headerRule", typeof(RectTransform), typeof(Image));
            ruleGo.transform.SetParent(root, false);
            _headerRule = (RectTransform)ruleGo.transform;
            _headerRule.anchorMin = new Vector2(0f, 1f);
            _headerRule.anchorMax = new Vector2(1f, 1f);
            _headerRule.pivot = new Vector2(0.5f, 1f);
            _headerRule.anchoredPosition = new Vector2(0f, -(36f + HeaderBodyHeight));
            _headerRule.sizeDelta = new Vector2(0f, HeaderRuleHeight);
            var img = ruleGo.GetComponent<Image>();
            img.sprite = CharacterExtras.Plate();
            img.color = new Color(0.62f, 0.48f, 0.28f, 0.42f);
            img.raycastTarget = false;
        }

        static Color StatsBodyFill()
        {
            Color c = CharacterExtras.CraftPanelInnerFill();
            float luma = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            float grey = Mathf.Lerp(luma, 0.11f, 0.72f);
            grey = Mathf.Clamp(grey, 0.08f, 0.2f);
            return new Color(grey, grey, grey * 1.02f, Mathf.Clamp01(c.a + 0.06f));
        }

        static void ApplyPortraitFrameSize(RectTransform frameRt)
        {
            if (frameRt == null)
            {
                return;
            }

            frameRt.sizeDelta = new Vector2(PortraitOuter, PortraitOuter);
            if (frameRt.TryGetComponent(out LayoutElement le))
            {
                le.preferredWidth = PortraitOuter;
                le.preferredHeight = PortraitOuter;
                le.minWidth = PortraitOuter;
                le.minHeight = PortraitOuter;
            }
        }

        static void ApplyPanelChrome()
        {
            if (_root == null)
            {
                return;
            }

            Color body = StatsBodyFill();
            Transform fillT = _root.Find("fill");
            if (fillT != null && fillT.TryGetComponent(out Image fillImg))
            {
                fillImg.color = body;
            }

            Transform viewT = _root.Find("view");
            if (viewT != null && viewT.TryGetComponent(out Image viewImg))
            {
                viewImg.color = body;
            }

            if (_headerBlock != null)
            {
                _headerBlock.sizeDelta = new Vector2(0f, HeaderBodyHeight);
                if (_headerBlock.TryGetComponent(out Image headerImg))
                {
                    headerImg.color = HeaderFill;
                }

                if (_headerBlock.TryGetComponent(out HorizontalLayoutGroup headerLayout))
                {
                    headerLayout.padding = new RectOffset(
                        (int)HeaderPortraitLeftPad,
                        4,
                        (int)(6f + HeaderPadExtra),
                        (int)(4f + HeaderPadExtra));
                }

                ApplyPortraitFrameSize(_headerBlock.Find("portraitFrame") as RectTransform);
            }

            if (_titleBar != null && _titleBar.TryGetComponent(out Image titleImg))
            {
                titleImg.color = TitleFill;
            }

            if (_headerRule != null && _headerRule.TryGetComponent(out Image ruleImg))
            {
                ruleImg.sprite = CharacterExtras.Plate();
                ruleImg.color = new Color(0.62f, 0.48f, 0.28f, 0.42f);
                _headerRule.sizeDelta = new Vector2(0f, HeaderRuleHeight);
            }
        }

        static Scrollbar AddScrollbar(RectTransform window)
        {
            var trackGo = new GameObject("scroll", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(window, false);
            var track = (RectTransform)trackGo.transform;
            track.anchorMin = new Vector2(1f, 0f);
            track.anchorMax = new Vector2(1f, 1f);
            track.pivot = new Vector2(1f, 1f);
            track.offsetMin = new Vector2(-24f, 12f);
            track.offsetMax = new Vector2(-6f, -(36f + HeaderStackHeight + 4f));
            trackGo.GetComponent<Image>().color = ScrollTrackFill;

            var areaGo = new GameObject("area", typeof(RectTransform));
            areaGo.transform.SetParent(track, false);
            var area = (RectTransform)areaGo.transform;
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(3f, 3f);
            area.offsetMax = new Vector2(-3f, -3f);

            var handleGo = new GameObject("handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(area, false);
            var handle = (RectTransform)handleGo.transform;
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.sizeDelta = Vector2.zero;
            var handleImg = handleGo.GetComponent<Image>();
            handleImg.sprite = CharacterExtras.Plate();
            handleImg.color = new Color(0.78f, 0.55f, 0.22f, 0.98f);

            var bar = trackGo.AddComponent<Scrollbar>();
            bar.handleRect = handle;
            bar.targetGraphic = handleImg;
            bar.direction = Scrollbar.Direction.BottomToTop;
            return bar;
        }

        static void EnsureFontTabs()
        {
            if (_fontTabs != null || InventoryGui.instance == null)
            {
                return;
            }

            Transform[] transforms = InventoryGui.instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == "tabs" && transforms[i].Find("gear") != null)
                {
                    _fontTabs = transforms[i];
                    break;
                }
            }
        }

        static TMP_Text PanelTitleSample()
        {
            EnsureFontTabs();
            if (_panelTitleSample != null)
            {
                return _panelTitleSample;
            }

            if (_fontTabs != null)
            {
                Transform orn = _fontTabs.Find("orn/t");
                if (orn != null)
                {
                    _panelTitleSample = orn.GetComponent<TMP_Text>();
                }
            }

            return _panelTitleSample;
        }

        static void EnsureFonts()
        {
            double now = Time.unscaledTimeAsDouble;
            if (_bodyFont == null)
            {
                _bodyFont = ChatFont.Resolve("Valheim-AveriaSansLibre", now);
            }

            if (_titleFont != null)
            {
                return;
            }

            TMP_Text sample = PanelTitleSample();
            if (sample != null && sample.font != null)
            {
                _titleFont = sample.font;
                return;
            }

            _titleFont = ChatFont.Resolve("Valheim-AveriaSerifLibre", now) ?? _bodyFont;
        }

        static void ApplyTitleFont(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            EnsureFonts();
            TMP_Text sample = PanelTitleSample();
            if (sample != null && sample.font != null)
            {
                label.font = sample.font;
                if (sample.fontSharedMaterial != null)
                {
                    label.fontSharedMaterial = sample.fontSharedMaterial;
                }

                return;
            }

            ApplyFontAsset(label, _titleFont);
        }

        static void ApplyBodyFont(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            EnsureFonts();
            ApplyFontAsset(label, _bodyFont);
        }

        static void ApplyFontAsset(TextMeshProUGUI label, TMP_FontAsset font)
        {
            if (label == null || font == null)
            {
                return;
            }

            label.font = font;
            if (font.material != null)
            {
                label.fontSharedMaterial = font.material;
            }
        }

        static void ScrollToTop()
        {
            if (_scroll == null || _list == null)
            {
                return;
            }

            _scroll.velocity = Vector2.zero;
            _scroll.verticalNormalizedPosition = 1f;
            _list.anchoredPosition = new Vector2(_list.anchoredPosition.x, 0f);
        }

        static void AddCloseButton(RectTransform window)
        {
            var closeGo = new GameObject("close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(window, false);
            var rt = (RectTransform)closeGo.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(28f, 28f);
            rt.anchoredPosition = new Vector2(-4f, -4f);
            var img = closeGo.GetComponent<Image>();
            img.sprite = CharacterExtras.Plate();
            img.color = new Color(0.62f, 0.48f, 0.28f, 0.95f);
            var btn = closeGo.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Close);
            var label = new GameObject("x", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(closeGo.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.text = "×";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.93f, 0.82f, 0.52f, 1f);
            label.raycastTarget = false;
        }

        static void Refresh(bool force)
        {
            if (_list == null)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + 0.75f;
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            _title.text = "Character Stats";
            ApplyTitleFont(_title);
            RefreshHeader(player, force);

            Collect(player);
            if (!force && TryPaintExistingRows())
            {
                return;
            }

            if (force && TryPaintExistingRows())
            {
                ScrollToTop();
                return;
            }

            float scrollPos = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(_list.GetChild(i).gameObject);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                AddRow(_rows[i]);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_list);
            Canvas.ForceUpdateCanvases();
            if (force)
            {
                ScrollToTop();
            }
            else if (_scroll != null)
            {
                _scroll.verticalNormalizedPosition = scrollPos;
            }
        }

        static bool TryPaintExistingRows()
        {
            if (_list.childCount != _rows.Count)
            {
                return false;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                Transform row = _list.GetChild(i);
                StatRow data = _rows[i];
                if (data.Kind == RowKind.Header)
                {
                    Transform labelGroup = row.Find("h/labelGroup");
                    if (labelGroup == null)
                    {
                        return false;
                    }

                    var heading = labelGroup.Find("heading")?.GetComponent<TextMeshProUGUI>();
                    if (heading == null)
                    {
                        return false;
                    }

                    Transform iconSlot = labelGroup.Find("icon");
                    if (iconSlot == null || iconSlot.Find("g") == null)
                    {
                        return false;
                    }

                    var iconRt = iconSlot as RectTransform;
                    if (iconRt == null || Mathf.Abs(iconRt.sizeDelta.x - SectionIconPx) > 0.5f)
                    {
                        return false;
                    }

                    if (heading.text != data.Label)
                    {
                        heading.text = data.Label;
                    }

                    if (iconSlot != null)
                    {
                        var slotLe = iconSlot.GetComponent<LayoutElement>();
                        if (slotLe != null)
                        {
                            slotLe.preferredWidth = SectionIconPx;
                            slotLe.preferredHeight = SectionIconPx;
                            slotLe.minWidth = SectionIconPx;
                            slotLe.minHeight = SectionIconPx;
                        }
                    }

                    continue;
                }

                var label = row.Find("label")?.GetComponent<TextMeshProUGUI>();
                var value = row.Find("value")?.GetComponent<TextMeshProUGUI>();
                if (label == null || value == null)
                {
                    return false;
                }

                string wantLabel = DisplayLabel(data.Label);
                if (label.text != wantLabel)
                {
                    label.text = wantLabel;
                }

                string wantValue = FormatRowValue(data);
                if (value.text != wantValue)
                {
                    value.text = wantValue;
                }
            }

            return true;
        }

        static string DisplayLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return "";
            }

            return label[label.Length - 1] == ':' ? label : label + ":";
        }

        static string FormatRowValue(StatRow row)
        {
            if (row.Value == null)
            {
                return "—";
            }

            if (row.Value.Length == 0)
            {
                return string.IsNullOrEmpty(row.Label) ? "—" : "";
            }

            return row.Value;
        }

        static void RowLabelOnly(string label)
        {
            _rows.Add(new StatRow { Kind = RowKind.Data, Label = label, Value = "" });
        }

        static void RefreshHeader(Player player, bool forcePortrait)
        {
            if (_headerName == null)
            {
                return;
            }

            _headerName.text = player.GetPlayerName();
            ApplyBodyFont(_headerName);
            string spec = StatsTalentFocus.Describe(player);
            if (_headerSpec != null)
            {
                _headerSpec.text = string.IsNullOrEmpty(spec) ? "" : spec;
                _headerSpec.gameObject.SetActive(!string.IsNullOrEmpty(spec));
                ApplyBodyFont(_headerSpec);
            }

            bool wantPortrait = SeneaLHudLayoutPlugin.StatsPortrait == null || SeneaLHudLayoutPlugin.StatsPortrait.Value;
            if (_portrait == null)
            {
                return;
            }

            if (!wantPortrait)
            {
                _portrait.texture = null;
                _portrait.color = new Color(0.15f, 0.12f, 0.08f, 1f);
                return;
            }

            if (!forcePortrait && _portraitTex != null)
            {
                return;
            }

            if (StatsSteamPortrait.TryGetLocal(out Texture2D tex))
            {
                if (_portraitTex != null && _portraitTex != tex)
                {
                    UnityEngine.Object.Destroy(_portraitTex);
                }

                _portraitTex = tex;
                _portrait.texture = tex;
                _portrait.color = Color.white;
                var fit = _portrait.GetComponent<AspectRatioFitter>();
                if (fit != null && tex.height > 0)
                {
                    fit.aspectRatio = (float)tex.width / tex.height;
                }
            }
            else if (_portraitTex == null)
            {
                _portrait.texture = null;
                _portrait.color = new Color(0.18f, 0.14f, 0.1f, 1f);
            }
        }

        static void AddRow(StatRow row)
        {
            if (row.Kind == RowKind.Header)
            {
                var section = new GameObject("section", typeof(RectTransform), typeof(LayoutElement));
                section.transform.SetParent(_list, false);
                var le = section.GetComponent<LayoutElement>();
                le.minHeight = 34f;
                le.preferredHeight = 34f;

                var rowH = new GameObject("h", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                rowH.transform.SetParent(section.transform, false);
                var headerRow = rowH.GetComponent<HorizontalLayoutGroup>();
                headerRow.spacing = 0f;
                headerRow.childAlignment = TextAnchor.MiddleCenter;
                headerRow.childControlWidth = true;
                headerRow.childControlHeight = true;
                headerRow.childForceExpandWidth = true;
                headerRow.childForceExpandHeight = false;
                headerRow.padding = new RectOffset(0, 0, 4, 4);
                var rowHRt = (RectTransform)rowH.transform;
                rowHRt.anchorMin = Vector2.zero;
                rowHRt.anchorMax = Vector2.one;
                rowHRt.offsetMin = Vector2.zero;
                rowHRt.offsetMax = Vector2.zero;

                var groupGo = new GameObject("labelGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                groupGo.transform.SetParent(rowH.transform, false);
                var groupRow = groupGo.GetComponent<HorizontalLayoutGroup>();
                groupRow.spacing = 3f;
                groupRow.childAlignment = TextAnchor.MiddleCenter;
                groupRow.childControlWidth = true;
                groupRow.childControlHeight = true;
                groupRow.childForceExpandWidth = false;
                groupRow.childForceExpandHeight = false;
                var groupLe = groupGo.GetComponent<LayoutElement>();
                groupLe.flexibleWidth = 0f;
                groupLe.flexibleHeight = 0f;

                AddSectionIcon(groupGo.transform, row.Icon);

                var headingGo = new GameObject("heading", typeof(RectTransform));
                headingGo.transform.SetParent(groupGo.transform, false);
                var headingTmp = headingGo.AddComponent<TextMeshProUGUI>();
                headingTmp.text = row.Label;
                headingTmp.fontSize = SectionFontSize;
                headingTmp.fontStyle = FontStyles.Bold;
                headingTmp.color = SectionTitleColor;
                headingTmp.alignment = TextAlignmentOptions.MidlineLeft;
                headingTmp.textWrappingMode = TextWrappingModes.NoWrap;
                var headingLe = headingGo.AddComponent<LayoutElement>();
                headingLe.flexibleWidth = 0f;
                headingLe.preferredHeight = SectionFontSize + 4f;
                headingLe.minHeight = SectionFontSize + 2f;
                ApplyBodyFont(headingTmp);
                return;
            }

            var rowGo = new GameObject("row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGo.transform.SetParent(_list, false);
            var h = rowGo.GetComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.spacing = 6f;
            h.padding = new RectOffset(4, 2, 0, 0);
            h.childAlignment = TextAnchor.MiddleLeft;
            rowGo.GetComponent<LayoutElement>().minHeight = 20f;

            var labelGo = new GameObject("label", typeof(RectTransform));
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = DisplayLabel(row.Label);
            labelTmp.fontSize = 13f;
            labelTmp.color = new Color(0.75f, 0.68f, 0.55f, 1f);
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.enableWordWrapping = false;
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 0f;
            labelLe.minWidth = 40f;
            ApplyBodyFont(labelTmp);

            var valueGo = new GameObject("value", typeof(RectTransform));
            valueGo.transform.SetParent(rowGo.transform, false);
            var valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
            valueTmp.text = FormatRowValue(row);
            valueTmp.fontSize = 13f;
            valueTmp.color = new Color(0.96f, 0.92f, 0.82f, 1f);
            valueTmp.alignment = TextAlignmentOptions.MidlineLeft;
            valueTmp.enableWordWrapping = true;
            var valueLe = valueGo.AddComponent<LayoutElement>();
            valueLe.flexibleWidth = 1f;
            ApplyBodyFont(valueTmp);
        }

        static void Collect(Player player)
        {
            _rows.Clear();
            Section("Vitals", "flame");
            Row("Health", Pair(player.GetHealth(), player.GetMaxHealth()));
            Row("Stamina", Pair(player.GetStamina(), player.GetMaxStamina()));
            Row("Eitr", Pair(player.GetEitr(), player.GetMaxEitr()));
            Row("Health regen", HealthRegenPerSecond(player));
            Row("Stamina regen", StaminaRegenPerSecond(player));
            Row("Eitr regen", EitrRegenPerSecond(player));

            Section("Defense", "shield");
            Row("Total armor", F1(player.GetBodyArmor()));
            Row("Move speed", MoveSpeed(player));
            AddResists(player);
            Row("Block", F0(BlockPower(player)));

            Section("Inventory & mobility", "weight");
            Row("Weight", Pair(player.GetInventory().GetTotalWeight(), player.GetMaxCarryWeight(), "kg"));
            Row("Encumbered", player.IsEncumbered() ? "Yes" : "No");

            Section("Buffs & status", "spark");
            AddFoods(player);
            AddStatusEffects(player);
            AddGuardian(player);

            Section("Skills", "swords");
            AddWeaponSkills(player);
            Row("Blocking", SkillLevel(player, Skills.SkillType.Blocking));
            Row("Run", SkillLevel(player, Skills.SkillType.Run));
            Row("Jump", SkillLevel(player, Skills.SkillType.Jump));
            Row("Dodge", SkillLevel(player, Skills.SkillType.Dodge));
        }

        static void AddSectionIcon(Transform parent, string iconName)
        {
            Sprite icon = SeneaLIcons.Get(iconName);
            if (icon == null)
            {
                return;
            }

            var slotGo = new GameObject("icon", typeof(RectTransform), typeof(LayoutElement));
            slotGo.transform.SetParent(parent, false);
            var slotRt = (RectTransform)slotGo.transform;
            slotRt.sizeDelta = new Vector2(SectionIconPx, SectionIconPx);
            var slotLe = slotGo.GetComponent<LayoutElement>();
            slotLe.ignoreLayout = false;
            slotLe.preferredWidth = SectionIconPx;
            slotLe.preferredHeight = SectionIconPx;
            slotLe.minWidth = SectionIconPx;
            slotLe.minHeight = SectionIconPx;
            slotLe.flexibleWidth = 0f;
            slotLe.flexibleHeight = 0f;

            var iconGo = new GameObject("g", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(SectionIconPx, SectionIconPx);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.color = SectionIconColor;
            iconImg.raycastTarget = false;
            iconImg.SetNativeSize();
            iconRt.sizeDelta = new Vector2(SectionIconPx, SectionIconPx);
        }

        static void Section(string title, string icon = null)
        {
            _rows.Add(new StatRow { Kind = RowKind.Header, Label = title, Value = "", Icon = icon });
        }

        static void Row(string label, string value)
        {
            _rows.Add(new StatRow { Kind = RowKind.Data, Label = label, Value = value ?? "—" });
        }

        static string Pair(float current, float max, string suffix = "")
        {
            string s = suffix.Length > 0 ? " " + suffix : "";
            return F0(current) + " / " + F0(max) + s;
        }

        static string F0(float v)
        {
            return v.ToString("0.#", CultureInfo.InvariantCulture);
        }

        static string F1(float v)
        {
            return v.ToString("0.#", CultureInfo.InvariantCulture);
        }

        static string MoveSpeed(Player player)
        {
            float mod = player.GetEquipmentMovementModifier();
            float pct = mod * 100f;
            return (pct >= 0f ? "+" : "") + pct.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        static string HealthRegenPerSecond(Player player)
        {
            float sum = 0f;
            foreach (Player.Food food in player.GetFoods())
            {
                if (food?.m_item?.m_shared != null)
                {
                    sum += food.m_item.m_shared.m_foodRegen;
                }
            }

            float mult = 1f;
            player.GetSEMan().ModifyHealthRegen(ref mult);
            sum *= mult;
            return sum > 0.01f ? F1(sum / 10f) + " / s" : "—";
        }

        static string StaminaRegenPerSecond(Player player)
        {
            float max = player.GetMaxStamina();
            if (max < 1f)
            {
                return "—";
            }

            float stamina = player.GetStamina();
            float rate = (player.m_staminaRegen + (1f - stamina / max) * player.m_staminaRegen * player.m_staminaRegenTimeMultiplier);
            float mult = 1f;
            player.GetSEMan().ModifyStaminaRegen(ref mult);
            rate *= mult * Game.m_staminaRegenRate;
            return F1(rate) + " / s";
        }

        static string EitrRegenPerSecond(Player player)
        {
            float max = player.GetMaxEitr();
            if (max < 1f)
            {
                return "—";
            }

            float eitr = player.GetEitr();
            float rate = (player.m_eiterRegen + (1f - eitr / max) * player.m_eiterRegen);
            float mult = 1f;
            player.GetSEMan().ModifyEitrRegen(ref mult);
            rate *= mult;
            rate *= 1f + player.GetEquipmentEitrRegenModifier();
            return F1(rate) + " / s";
        }

        static void AddResists(Player player)
        {
            HitData.DamageModifiers mods = player.GetDamageModifiers();
            string[] names = { "Slash", "Pierce", "Blunt", "Fire", "Frost", "Lightning", "Poison", "Spirit" };
            HitData.DamageType[] types =
            {
                HitData.DamageType.Slash,
                HitData.DamageType.Pierce,
                HitData.DamageType.Blunt,
                HitData.DamageType.Fire,
                HitData.DamageType.Frost,
                HitData.DamageType.Lightning,
                HitData.DamageType.Poison,
                HitData.DamageType.Spirit
            };

            var resist = new List<string>();
            var weak = new List<string>();
            for (int i = 0; i < types.Length; i++)
            {
                HitData.DamageModifier mod = mods.GetModifier(types[i]);
                if (mod == HitData.DamageModifier.Resistant || mod == HitData.DamageModifier.VeryResistant || mod == HitData.DamageModifier.Immune)
                {
                    resist.Add(names[i] + " (" + ModLabel(mod) + ")");
                }
                else if (mod == HitData.DamageModifier.Weak || mod == HitData.DamageModifier.VeryWeak)
                {
                    weak.Add(names[i] + " (" + ModLabel(mod) + ")");
                }
            }

            Row("Resistances", resist.Count > 0 ? string.Join(", ", resist) : "—");
            Row("Weaknesses", weak.Count > 0 ? string.Join(", ", weak) : "—");
        }

        static string ModLabel(HitData.DamageModifier mod)
        {
            switch (mod)
            {
                case HitData.DamageModifier.Immune: return "Immune";
                case HitData.DamageModifier.VeryResistant: return "Very resistant";
                case HitData.DamageModifier.Resistant: return "Resistant";
                case HitData.DamageModifier.Weak: return "Weak";
                case HitData.DamageModifier.VeryWeak: return "Very weak";
                default: return mod.ToString();
            }
        }

        static ItemDrop.ItemData FindShield(Player player)
        {
            foreach (ItemDrop.ItemData item in player.GetInventory().GetEquippedItems())
            {
                if (item?.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                {
                    return item;
                }
            }

            return null;
        }

        static float BlockPower(Player player)
        {
            ItemDrop.ItemData shield = FindShield(player);
            if (shield != null)
            {
                return shield.GetArmor();
            }

            return player.GetSkillLevel(Skills.SkillType.Blocking);
        }

        static void AddFoods(Player player)
        {
            List<Player.Food> foods = player.GetFoods();
            if (foods == null || foods.Count == 0)
            {
                Row("Food", "—");
                return;
            }

            for (int i = 0; i < foods.Count; i++)
            {
                Player.Food food = foods[i];
                if (food?.m_item == null)
                {
                    continue;
                }

                string name = food.m_item.m_shared.m_name;
                string time = FormatTime(food.m_time);
                Row(i == 0 ? "Food" : "", Localization.instance.Localize(name) + "  " + time);
            }
        }

        static void AddStatusEffects(Player player)
        {
            List<StatusEffect> list = player.GetSEMan().GetStatusEffects();
            if (list == null || list.Count == 0)
            {
                Row("Status effects", "—");
                return;
            }

            int shown = 0;
            for (int i = 0; i < list.Count; i++)
            {
                StatusEffect se = list[i];
                if (se == null || string.IsNullOrEmpty(se.m_name))
                {
                    continue;
                }

                if (se.m_name.IndexOf("guardian", StringComparison.OrdinalIgnoreCase) >= 0
                    || se.m_name.StartsWith("PassivePowers", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                float remain = StatusTime(se);
                string time = remain > 0.5f ? FormatTime(remain) : "";
                string name = Localization.instance != null
                    ? Localization.instance.Localize(se.m_name)
                    : se.m_name;
                string line = name + (time.Length > 0 ? "  " + time : "");
                if (shown == 0)
                {
                    RowLabelOnly("Status effects");
                }

                Row("", line);
                shown++;
            }

            if (shown == 0)
            {
                Row("Status effects", "—");
            }
        }

        static void AddGuardian(Player player)
        {
            player.GetGuardianPowerHUD(out StatusEffect se, out float cooldown);
            string name = se != null && !string.IsNullOrEmpty(se.m_name)
                ? (Localization.instance != null ? Localization.instance.Localize(se.m_name) : se.m_name)
                : "";
            string cd = cooldown > 0.5f ? FormatTime(cooldown) + " CD" : "Ready";
            Row("Forsaken power", name.Length > 0 ? name + " · " + cd : cd);
            AddGuardianPowers(player);
        }

        /// <summary>
        /// One row per equipped boss power (Passive Powers equips up to "Maximum boss
        /// powers", 2 in the pack): name + passive state — "Active 1m 59s" while the
        /// activation buff runs (120 s), "Depleted 1m 45s" during the lockout (120 s),
        /// "Passive" at rest — then the power's passive lines from the mod's own config,
        /// the same text the boss stone tooltip builds. Nothing without Passive Powers
        /// installed; vanilla's single power stays on the Forsaken power row.
        /// </summary>
        static void AddGuardianPowers(Player player)
        {
            if (player == null
                || PassiveModType == null
                || GuardianPowerField == null
                || (SeneaLHudLayoutPlugin.StatsGuardianPowers != null && !SeneaLHudLayoutPlugin.StatsGuardianPowers.Value))
            {
                return;
            }

            List<string> powers = new List<string>();
            string raw = GuardianPowerField.GetValue(player) as string;
            if (!string.IsNullOrEmpty(raw))
            {
                string[] parts = raw.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i].Trim();
                    if (part.Length > 0)
                    {
                        powers.Add(part);
                    }
                }
            }

            int slots = Mathf.Clamp(2, 1, 7);
            object maxEntry = PassiveMaxPowers != null ? PassiveMaxPowers.GetValue(null) : null;
            if (maxEntry is ConfigEntry<int> max)
            {
                slots = Mathf.Clamp(max.Value, 1, 7);
            }

            slots = Mathf.Max(slots, powers.Count);
            if (slots > 7)
            {
                slots = 7;
            }

            if (powers.Count == 0)
            {
                Row("Guardian powers", "—");
                return;
            }

            RowLabelOnly("Guardian powers");

            for (int i = 0; i < slots; i++)
            {
                if (i >= powers.Count)
                {
                    Row("", "—");
                    continue;
                }

                string key = powers[i];
                string name = GuardianPowerName(key);
                Row("", name + " · " + GuardianPowerState(player, key));
                List<string> descs = GuardianPowerDescriptions(key, "passive");
                for (int d = 0; d < descs.Count; d++)
                {
                    Row("", "· " + descs[d]);
                }
            }
        }

        static string GuardianPowerName(string key)
        {
            StatusEffect se = ObjectDB.instance != null
                ? ObjectDB.instance.GetStatusEffect(key.GetStableHashCode())
                : null;
            if (se == null || string.IsNullOrEmpty(se.m_name))
            {
                return key;
            }

            return Localization.instance != null ? Localization.instance.Localize(se.m_name) : se.m_name;
        }

        static string GuardianPowerState(Player player, string key)
        {
            float buff = GuardianPowerEffectTime(player, "PassivePowers " + key);
            if (buff > 0.5f)
            {
                return "Active " + FormatTime(buff);
            }

            float down = GuardianPowerEffectTime(player, "PassivePowers Depletion " + key);
            if (down > 0.5f)
            {
                return "Depleted " + FormatTime(down);
            }

            return "Passive";
        }

        static float GuardianPowerEffectTime(Player player, string effectName)
        {
            if (player == null)
            {
                return 0f;
            }

            StatusEffect se = player.GetSEMan().GetStatusEffect(effectName.GetStableHashCode());
            return StatusTime(se);
        }

        /// <summary>
        /// Passive (or active) effect lines straight from Passive Powers' own
        /// config tables (PassivePowers.activeBossConfigs[power].Configs), localized
        /// with the mod's own key so the stats tab says exactly what the boss stone
        /// tooltip says. All reflection, all optional.
        /// </summary>
        static List<string> GuardianPowerDescriptions(string powerName, string type)
        {
            List<string> lines = new List<string>();
            if (PassiveBossConfigs == null || Localization.instance == null)
            {
                return lines;
            }

            try
            {
                IDictionary dict = PassiveBossConfigs.GetValue(null) as IDictionary;
                if (dict == null || !dict.Contains(powerName))
                {
                    return lines;
                }

                object bossConfig = dict[powerName];
                if (bossConfig == null)
                {
                    return lines;
                }

                object configs = Member(bossConfig, "Configs");
                if (!(configs is IEnumerable list))
                {
                    return lines;
                }

                string boxed = type == "active" ? "BoxedActive" : "BoxedPassive";
                foreach (object cfg in list)
                {
                    if (cfg == null)
                    {
                        continue;
                    }

                    int amount = ToInt(Member(cfg, boxed));
                    if (amount <= 0)
                    {
                        continue;
                    }

                    string desc = Member(cfg, "Desc") as string;
                    if (string.IsNullOrEmpty(desc))
                    {
                        continue;
                    }

                    string line = Localization.instance.Localize(
                        "$powers_type_" + type + ": " + desc,
                        new string[1] { amount.ToString(CultureInfo.InvariantCulture) });
                    if (!string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                    }
                }
            }
            catch (Exception)
            {
                // Passive Powers updated its internals: the state row stays, the
                // description lines drop.
            }

            return lines;
        }

        static object Member(object target, string name)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            FieldInfo field = AccessTools.Field(type, name);
            if (field != null)
            {
                return field.GetValue(target);
            }

            PropertyInfo property = AccessTools.Property(type, name);
            return property != null ? property.GetValue(target, null) : null;
        }

        static int ToInt(object value)
        {
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        static void AddWeaponSkills(Player player)
        {
            for (int i = 0; i < AllWeaponSkills.Length; i++)
            {
                Skills.SkillType skill = AllWeaponSkills[i];
                Row(SkillName(skill), WeaponSkillValue(player, skill));
            }
        }

        static string WeaponSkillValue(Player player, Skills.SkillType skill)
        {
            float level = player.GetSkillLevel(skill);
            return level > 0.001f ? F0(level) : "—";
        }

        static string SkillName(Skills.SkillType skill)
        {
            switch (skill)
            {
                case Skills.SkillType.WoodCutting: return "Woodcutting";
                case Skills.SkillType.ElementalMagic: return "Elemental magic";
                case Skills.SkillType.BloodMagic: return "Blood magic";
                default: return skill.ToString();
            }
        }

        static string SkillLevel(Player player, Skills.SkillType skill)
        {
            return F0(player.GetSkillLevel(skill));
        }

        static float StatusTime(StatusEffect se)
        {
            // Valheim has no GetRemainingTime(); remaining duration is m_ttl - m_time (same as PowersLayout).
            if (se == null || se.m_ttl <= 0f)
            {
                return 0f;
            }

            float elapsed = StatusEffectTimeField?.GetValue(se) is float time ? time : 0f;
            return Mathf.Max(0f, se.m_ttl - elapsed);
        }

        static string FormatTime(float seconds)
        {
            if (seconds < 0f)
            {
                return "";
            }

            if (seconds >= 3600f)
            {
                return Mathf.FloorToInt(seconds / 3600f) + "h " + Mathf.FloorToInt(seconds % 3600f / 60f) + "m";
            }

            if (seconds >= 60f)
            {
                return Mathf.FloorToInt(seconds / 60f) + "m " + Mathf.FloorToInt(seconds % 60f) + "s";
            }

            return Mathf.CeilToInt(seconds) + "s";
        }

        static void SaveRect()
        {
            if (_root == null)
            {
                return;
            }

            PlayerPrefs.SetString(PrefRect, Format(_root.anchoredPosition.x, _root.anchoredPosition.y, _root.sizeDelta.x, _root.sizeDelta.y));
            PlayerPrefs.Save();
        }

        static void ApplySavedRect()
        {
            if (_root == null)
            {
                return;
            }

            string raw = PlayerPrefs.GetString(PrefRect, "");
            if (TryParse(raw, out float x, out float y, out float w, out float h))
            {
                _root.anchoredPosition = new Vector2(x, y);
                _root.sizeDelta = new Vector2(Mathf.Max(320f, w), Mathf.Max(360f, h));
            }
        }

        internal static string Format(float x, float y, float w, float h)
        {
            return x.ToString(CultureInfo.InvariantCulture) + "|" + y.ToString(CultureInfo.InvariantCulture) + "|"
                + w.ToString(CultureInfo.InvariantCulture) + "|" + h.ToString(CultureInfo.InvariantCulture);
        }

        internal static bool TryParse(string raw, out float x, out float y, out float w, out float h)
        {
            x = y = w = h = 0f;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            string[] parts = raw.Split('|');
            if (parts.Length != 4)
            {
                return false;
            }

            return float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out w)
                && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out h);
        }
    }
}
