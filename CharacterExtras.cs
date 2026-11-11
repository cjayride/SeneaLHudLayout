using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class CharacterExtras
    {
        const string LevelName = "SeneaLHudLayout_Level";
        const string FoodLevelName = "SeneaLHudLayout_FoodLevel";
        const string TalentsName = "SeneaLHudLayout_Talents";
        const string XpBarName = "SeneaLHudLayout_LevelXp";
        const float XpBarWidth = 100f;
        const float XpBarHeight = 6f;

        static Transform _tabs;

        // 1.5.52 FPS: gate for the whole-GUI tabs scan in EnsureLevel (2s retry while _tabs is
        // null; see the comment there).
        static float _nextTabsScan;
        static TextMeshProUGUI _level;
        static TextMeshProUGUI _foodLevel;
        static Image _foodPlate;
        static float _foodCenterX;
        static float _foodUnder;
        static bool _foodBoundsKnown;
        static int _foodLayoutStamp = int.MinValue;
        static string _foodPlacedText;
        static float _foodPlacedNudgeX = float.NaN;
        static float _foodPlacedNudgeY = float.NaN;
        static float _foodPlacedWidth;
        static Sprite _pixel;
        static MethodInfo _getLevel;
        static bool _levelLookup;
        static bool _loggedLevel;
        static Button _talents;
        static TextMeshProUGUI _talentsLabel;
        static MethodInfo _talentToggle;
        static bool _talentLookup;
        static Image _xpBack;
        static Image _xpFill;
        static TextMeshProUGUI _xpAmount;
        static TextMeshProUGUI _xpPercent;
        static string _xpMeasured;
        static float _xpLevelWidth;
        static MethodInfo _getFraction;
        static MethodInfo _getSkill;
        static MethodInfo _getRequirement;
        static bool _fractionLookup;

        public static void Tick()
        {
            TickLevel();
        }

        /// <summary>Player level from SkillsReworked, -1 when unavailable.</summary>
        public static int Level(Player player)
        {
            return ReadLevel(player);
        }

        /// <summary>
        /// Progress through the current level, 0-1, -1 when unavailable. Call this before
        /// <see cref="Xp"/>: it resolves the SkillsReworked reflection the XP numbers need.
        /// </summary>
        public static float Fraction(Player player)
        {
            return ReadLevelFraction(player);
        }

        /// <summary>Current and required XP for the current level, in the same units the +X XP notices use.</summary>
        public static void Xp(Player player, out float current, out float required)
        {
            ReadLevelXp(player, out current, out required);
        }

        /// <summary>XP number as the character-window bar writes it.</summary>
        public static string XpText(float value)
        {
            return FormatXp(value);
        }

        /// <summary>Solid white sprite, the same one the character-window bar fills use.</summary>
        public static Sprite PixelSprite()
        {
            return Pixel();
        }

        /// <summary>Copy the character-window level's font so a HUD copy of the bar matches it.</summary>
        public static void CopyLevelFont(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            if (_level != null && _level.font != null)
            {
                label.font = _level.font;
                if (_level.fontSharedMaterial != null)
                {
                    label.fontSharedMaterial = _level.fontSharedMaterial;
                }
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }
        }

        /// <summary>
        /// Copy the exact font AND material of the HUD's food-timer label — the same source
        /// the food-bar player level (ShowLevelFoodBar) copies, which is known to render.
        /// HUD text labels in Valheim carry their font's baked material; a label that copies
        /// the font asset but not that material samples the wrong atlas and every glyph
        /// draws as a random character. Never set outline / shadow / underline on a label
        /// styled this way: TMP replaces the text material with a clone of the font asset's
        /// base material, whose atlas reference does not survive on Valheim's atlases.
        /// </summary>
        public static void CopyFoodBarFont(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            TMP_Text sample = FoodTimerSample();
            if (sample != null && sample.font != null)
            {
                label.font = sample.font;
                if (sample.fontSharedMaterial != null)
                {
                    label.fontSharedMaterial = sample.fontSharedMaterial;
                }

                return;
            }

            CopyLevelFont(label);
        }

        static TMP_Text FoodTimerSample()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
            {
                return null;
            }

            Transform seneal = hud.m_rootObject.transform.Find("SeneaLUI_Hud");
            Transform vitals = seneal != null ? seneal.Find("Food/fx/vitals") : null;
            RectTransform food = vitals != null ? vitals.Find("food") as RectTransform : null;
            Transform timer = food != null ? food.Find("time2") : null;
            return timer != null ? timer.GetComponent<TMP_Text>() : null;
        }

        static void TickLevel()
        {
            bool show = SeneaLHudLayoutPlugin.ShowLevelCharacterWindow != null && SeneaLHudLayoutPlugin.ShowLevelCharacterWindow.Value;
            bool showFood = SeneaLHudLayoutPlugin.ShowLevelFoodBar != null && SeneaLHudLayoutPlugin.ShowLevelFoodBar.Value;
            int level = (show || showFood) && Player.m_localPlayer != null ? ReadLevel(Player.m_localPlayer) : -1;
            TickFoodLevel(showFood, level);

            if (!EnsureLevel())
            {
                return;
            }

            if (!show || level < 0 || !_tabs.gameObject.activeInHierarchy)
            {
                if (_level.gameObject.activeSelf)
                {
                    _level.gameObject.SetActive(false);
                }

                HideLevelExtras();
                return;
            }

            string text = "Level " + level;
            if (_level.text != text)
            {
                _level.text = text;
            }

            RectTransform rt = _level.rectTransform;
            rt.anchoredPosition = new Vector2(18f, -12f);

            if (!_level.gameObject.activeSelf)
            {
                _level.gameObject.SetActive(true);
            }

            TickTalents(true);
            TickXpBar(true);
        }

        static void HideLevelExtras()
        {
            TickTalents(false);
            TickXpBar(false);
        }

        static void TickTalents(bool levelShown)
        {
            bool show = levelShown && SeneaLHudLayoutPlugin.ShowTalentsButton != null && SeneaLHudLayoutPlugin.ShowTalentsButton.Value && FindTalentToggle() != null;
            if (!show)
            {
                if (_talents != null && _talents.gameObject.activeSelf)
                {
                    _talents.gameObject.SetActive(false);
                }

                return;
            }

            if (!EnsureTalents())
            {
                return;
            }

            if (!(_tabs.Find("gear") is RectTransform gear))
            {
                return;
            }

            RectTransform rt = _talents.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            float height = gear.sizeDelta.y > 8f ? gear.sizeDelta.y : 28f;
            rt.sizeDelta = new Vector2(88f, height);
            rt.anchoredPosition = new Vector2(gear.anchoredPosition.x - gear.sizeDelta.x - 8f, gear.anchoredPosition.y);
            rt.SetAsLastSibling();

            if (!_talents.gameObject.activeSelf)
            {
                _talents.gameObject.SetActive(true);
            }
        }

        static bool EnsureTalents()
        {
            if (_talents != null && _talentsLabel != null && _talents.transform.parent == _tabs)
            {
                return true;
            }

            Transform existing = _tabs.Find(TalentsName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject go = new GameObject(TalentsName);
            go.transform.SetParent(_tabs, false);
            Image plate = go.AddComponent<Image>();
            plate.sprite = Pixel();
            plate.raycastTarget = true;
            Frame(plate);
            _talents = go.AddComponent<Button>();
            _talents.targetGraphic = plate;
            _talents.onClick.AddListener(OnTalents);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            GameObject textGo = new GameObject("t");
            textGo.transform.SetParent(go.transform, false);
            _talentsLabel = textGo.AddComponent<TextMeshProUGUI>();
            _talentsLabel.raycastTarget = false;
            _talentsLabel.alignment = TextAlignmentOptions.Center;
            _talentsLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _talentsLabel.overflowMode = TextOverflowModes.Overflow;
            _talentsLabel.fontSize = 12f;
            _talentsLabel.fontStyle = FontStyles.Normal;
            _talentsLabel.color = new Color(0.93f, 0.82f, 0.52f, 1f);
            _talentsLabel.text = "Talents";
            if (_level.font != null)
            {
                _talentsLabel.font = _level.font;
                _talentsLabel.fontSharedMaterial = _level.fontSharedMaterial;
            }

            RectTransform textRt = _talentsLabel.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(6f, 0f);
            textRt.offsetMax = new Vector2(-6f, 0f);
            return true;
        }

        static void OnTalents()
        {
            MethodInfo toggle = FindTalentToggle();
            if (toggle == null)
            {
                return;
            }

            try
            {
                toggle.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SeneaL HUD Layout] Could not open TalentTree: " + e.GetBaseException().Message);
            }
        }

        static MethodInfo FindTalentToggle()
        {
            if (!_talentLookup)
            {
                _talentLookup = true;
                Type window = ModTypes.Find("TalentTree", "UI.TalentWindow");
                _talentToggle = window == null ? null : AccessTools.Method(window, "Toggle", Type.EmptyTypes);
            }

            return _talentToggle;
        }

        static void TickXpBar(bool levelShown)
        {
            bool show = levelShown && SeneaLHudLayoutPlugin.ShowLevelXpBar != null && SeneaLHudLayoutPlugin.ShowLevelXpBar.Value;
            float fraction = show ? ReadLevelFraction(Player.m_localPlayer) : -1f;
            if (fraction < 0f)
            {
                if (_xpBack != null && _xpBack.gameObject.activeSelf)
                {
                    _xpBack.gameObject.SetActive(false);
                }

                return;
            }

            if (!EnsureXpBar())
            {
                return;
            }

            if (_level.text != _xpMeasured)
            {
                _level.ForceMeshUpdate();
                _xpMeasured = _level.text;
                _xpLevelWidth = Mathf.Ceil(_level.preferredWidth);
            }

            float levelWidth = _xpLevelWidth;
            RectTransform level = _level.rectTransform;
            RectTransform back = _xpBack.rectTransform;
            back.anchoredPosition = new Vector2(18f + levelWidth + 10f, level.anchoredPosition.y - (level.sizeDelta.y - XpBarHeight) * 0.5f);
            back.sizeDelta = new Vector2(XpBarWidth, XpBarHeight);
            _xpFill.rectTransform.sizeDelta = new Vector2((XpBarWidth - 2f) * Mathf.Clamp01(fraction), XpBarHeight - 2f);

            ReadLevelXp(Player.m_localPlayer, out float current, out float required);
            string amount = FormatXp(current) + " / " + FormatXp(required);
            if (_xpAmount.text != amount)
            {
                _xpAmount.text = amount;
            }

            string percent = Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100f) + "%";
            if (_xpPercent.text != percent)
            {
                _xpPercent.text = percent;
            }

            if (!_xpBack.gameObject.activeSelf)
            {
                _xpBack.gameObject.SetActive(true);
            }
        }

        static bool EnsureXpBar()
        {
            if (_xpBack != null && _xpFill != null && _xpAmount != null && _xpPercent != null && _xpBack.transform.parent == _tabs)
            {
                return true;
            }

            Transform existing = _tabs.Find(XpBarName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            GameObject go = new GameObject(XpBarName);
            go.transform.SetParent(_tabs, false);
            _xpBack = go.AddComponent<Image>();
            _xpBack.sprite = Pixel();
            _xpBack.color = new Color(0f, 0f, 0f, 0.65f);
            _xpBack.raycastTarget = false;
            RectTransform rt = _xpBack.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            GameObject fillGo = new GameObject("fill");
            fillGo.transform.SetParent(go.transform, false);
            _xpFill = fillGo.AddComponent<Image>();
            _xpFill.sprite = Pixel();
            _xpFill.color = new Color(1f, 0.86f, 0.25f, 0.95f);
            _xpFill.raycastTarget = false;
            RectTransform fillRt = _xpFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = new Vector2(1f, 0f);

            _xpAmount = MakeXpLabel("amount", go.transform, TextAlignmentOptions.Left);
            RectTransform amountRt = _xpAmount.rectTransform;
            amountRt.anchorMin = new Vector2(0f, 0f);
            amountRt.anchorMax = new Vector2(0f, 0f);
            amountRt.pivot = new Vector2(0f, 1f);
            amountRt.anchoredPosition = new Vector2(0f, -2f);
            amountRt.sizeDelta = new Vector2(XpBarWidth, 12f);

            _xpPercent = MakeXpLabel("percent", go.transform, TextAlignmentOptions.Left);
            RectTransform percentRt = _xpPercent.rectTransform;
            percentRt.anchorMin = new Vector2(1f, 0.5f);
            percentRt.anchorMax = new Vector2(1f, 0.5f);
            percentRt.pivot = new Vector2(0f, 0.5f);
            percentRt.anchoredPosition = new Vector2(6f, 0f);
            percentRt.sizeDelta = new Vector2(36f, 14f);
            return true;
        }

        static TextMeshProUGUI MakeXpLabel(string name, Transform parent, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = align;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.fontSize = 10f;
            label.color = new Color(0.86f, 0.75f, 0.48f, 0.95f);
            if (_level != null && _level.font != null)
            {
                label.font = _level.font;
                label.fontSharedMaterial = _level.fontSharedMaterial;
            }

            return label;
        }

        static float ReadLevelFraction(Player player)
        {
            if (player == null)
            {
                return -1f;
            }

            if (!_fractionLookup)
            {
                _fractionLookup = true;
                Type type = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Progression.LevelSkillService");
                _getFraction = type == null ? null : AccessTools.Method(type, "GetCurrentLevelFraction", new[] { typeof(Player) });
                _getSkill = type == null ? null : AccessTools.Method(type, "GetSkill", new[] { typeof(Player) });
                Type calc = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Progression.LevelXpCalculator");
                _getRequirement = calc == null ? null : AccessTools.Method(calc, "GetRequirementForLevel", new[] { typeof(int) });
            }

            if (_getFraction == null)
            {
                return -1f;
            }

            try
            {
                return (float)_getFraction.Invoke(null, new object[] { player });
            }
            catch (Exception)
            {
                return -1f;
            }
        }

        static void ReadLevelXp(Player player, out float current, out float required)
        {
            current = 0f;
            required = 0f;
            if (player == null || _getSkill == null)
            {
                return;
            }

            try
            {
                if (!(_getSkill.Invoke(null, new object[] { player }) is Skills.Skill skill) || skill == null)
                {
                    return;
                }

                // SkillsReworked feeds the level skill through RaiseSkill, which multiplies
                // XP by the skill's increase step (0.1) and the global gain rate before it
                // reaches the accumulator. The +X XP notices report XP before that step,
                // so divide the raw accumulator numbers back out to show the same units.
                float unit = 1f;
                if (skill.m_info != null && skill.m_info.m_increseStep > 0f)
                {
                    unit = skill.m_info.m_increseStep;
                    if (Game.m_skillGainRate > 0f)
                    {
                        unit *= Game.m_skillGainRate;
                    }
                }

                current = Mathf.Max(0f, skill.m_accumulator) / unit;
                int levelIndex = Mathf.FloorToInt(skill.m_level);
                if (_getRequirement != null)
                {
                    required = (float)_getRequirement.Invoke(null, new object[] { levelIndex });
                }
                else
                {
                    float step = Mathf.Floor(levelIndex + 1f);
                    required = Mathf.Pow(step, 1.5f) * 0.5f + 0.5f;
                }

                required = required / unit;
            }
            catch (Exception)
            {
                current = 0f;
                required = 0f;
            }
        }

        static string FormatXp(float value)
        {
            if (value >= 9.95f)
            {
                return value.ToString("0");
            }

            return value.ToString("0.#");
        }

        static bool EnsureLevel()
        {
            if (_tabs == null)
            {
                InventoryGui gui = InventoryGui.instance;
                if (gui == null)
                {
                    return false;
                }

                // 1.5.52 FPS: TickLevel() runs EVERY frame from LateUpdate, and while the
                // character-window "tabs" node is absent (inventory closed = almost always) the
                // cache stays null, so the whole-GUI GetComponentsInChildren<Transform>(true)
                // below re-ran every frame forever - the top unverified per-frame suspicion the
                // FPS audit raised. Negative-cache: retry on a 2s cadence. The label is then
                // (re)built within 2s of the window opening, once per session.
                if (Time.unscaledTime < _nextTabsScan)
                {
                    return false;
                }

                _nextTabsScan = Time.unscaledTime + 2f;

                Transform[] transforms = gui.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform candidate = transforms[i];
                    if (candidate.name == "tabs" && candidate.Find("tab0") != null)
                    {
                        _tabs = candidate;
                        break;
                    }
                }
            }

            if (_tabs == null)
            {
                return false;
            }

            if (_level == null)
            {
                Transform existing = _tabs.Find(LevelName);
                _level = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
                if (_level == null)
                {
                    GameObject go = new GameObject(LevelName);
                    go.transform.SetParent(_tabs, false);
                    _level = go.AddComponent<TextMeshProUGUI>();
                    _level.raycastTarget = false;
                    _level.alignment = TextAlignmentOptions.Left;
                    _level.textWrappingMode = TextWrappingModes.NoWrap;
                    _level.overflowMode = TextOverflowModes.Overflow;
                    _level.fontSize = 12f;
                    _level.color = new Color(0.86f, 0.75f, 0.48f, 0.95f);

                    RectTransform rt = _level.rectTransform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(18f, -12f);
                    rt.sizeDelta = new Vector2(88f, 16f);

                    TMP_Text title = null;
                    Transform titleTransform = _tabs.Find("orn/t");
                    if (titleTransform != null)
                    {
                        title = titleTransform.GetComponent<TMP_Text>();
                    }

                    if (title != null && title.font != null)
                    {
                        _level.font = title.font;
                        if (title.fontSharedMaterial != null)
                        {
                            _level.fontSharedMaterial = title.fontSharedMaterial;
                        }
                    }
                    else if (TMP_Settings.defaultFontAsset != null)
                    {
                        _level.font = TMP_Settings.defaultFontAsset;
                    }
                }
            }

            return _level != null;
        }

        static void TickFoodLevel(bool show, int level)
        {
            if (!EnsureFoodLevel())
            {
                return;
            }

            if (!show || level < 0 || Player.m_localPlayer == null)
            {
                if (_foodPlate != null && _foodPlate.gameObject.activeSelf)
                {
                    _foodPlate.gameObject.SetActive(false);
                }

                return;
            }

            string text = "Level " + level;
            if (_foodLevel.text != text)
            {
                _foodLevel.text = text;
            }

            if (_foodPlate != null && !_foodPlate.gameObject.activeSelf)
            {
                _foodPlate.gameObject.SetActive(true);
            }
        }

        static bool EnsureFoodLevel()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
            {
                return false;
            }

            Transform seneal = hud.m_rootObject.transform.Find("SeneaLUI_Hud");

            /* foreach (Transform t in seneal.GetComponentsInChildren<Transform>(true)) // debug
            {
                if (t.name == "food" || t.name == "vitals" || t.name == "Health")
                {
                    Debug.LogWarning(
                        "[SeneaL HUD Layout] HUD node: " +
                        GetPath(t, seneal)
                    );
                }
            } */

            Transform vitals = seneal != null ? seneal.Find("Food/fx/vitals") : null;
            RectTransform food = vitals != null ? vitals.Find("food") as RectTransform : null;


           /* if (food == null)  // debug
            {
                Debug.LogWarning(
                    "[SeneaL HUD Layout] Food level lookup failed. " +
                    "SeneaL=" + (seneal != null) +
                    ", Vitals=" + (vitals != null) +
                    ", Food=" + (food != null)
                );
                return false;
            } */

            if (food == null)
            {
                return false;
            }

            if (_foodLevel == null || _foodPlate == null || _foodPlate.transform.parent != food)
            {
                Transform existing = vitals.Find(FoodLevelName);
                _foodPlate = existing != null ? existing.GetComponent<Image>() : null;
                _foodLevel = existing != null ? existing.GetComponentInChildren<TextMeshProUGUI>() : null;
                if (_foodPlate == null)
                {
                    GameObject plateGo = new GameObject(FoodLevelName);
                    plateGo.transform.SetParent(food, false);
                    _foodPlate = plateGo.AddComponent<Image>();
                    _foodPlate.raycastTarget = false;
                    IgnoreFoodLayout(_foodPlate.gameObject);
                    _foodPlate.sprite = Pixel();
                    _foodPlate.color = new Color(0.07f, 0.05f, 0.03f, 0.62f);
                    _foodPlate.type = Image.Type.Simple;

                    GameObject textGo = new GameObject("t");
                    textGo.transform.SetParent(plateGo.transform, false);
                    _foodLevel = textGo.AddComponent<TextMeshProUGUI>();
                    _foodLevel.raycastTarget = false;
                    _foodLevel.alignment = TextAlignmentOptions.Center;
                    _foodLevel.textWrappingMode = TextWrappingModes.NoWrap;
                    _foodLevel.overflowMode = TextOverflowModes.Overflow;
                    _foodLevel.fontSize = 13f;
                    _foodLevel.fontStyle = FontStyles.Bold;
                    _foodLevel.color = new Color(0.93f, 0.82f, 0.52f, 1f);

                    RectTransform textRt = _foodLevel.rectTransform;
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = new Vector2(2f, 0f);
                    textRt.offsetMax = new Vector2(-2f, 0f);

                    TMP_Text sample = null;
                    Transform timer = food.Find("time2");
                    if (timer != null)
                    {
                        sample = timer.GetComponent<TMP_Text>();
                    }

                    if (sample != null && sample.font != null)
                    {
                        _foodLevel.font = sample.font;
                        if (sample.fontSharedMaterial != null)
                        {
                            _foodLevel.fontSharedMaterial = sample.fontSharedMaterial;
                        }
                    }
                    else if (TMP_Settings.defaultFontAsset != null)
                    {
                        _foodLevel.font = TMP_Settings.defaultFontAsset;
                    }
                }
            }

            if (_foodPlate == null || _foodLevel == null)
            {
                return false;
            }

            if (_foodPlate.transform.parent != food)
            {
                _foodPlate.transform.SetParent(food, false);
                _foodBoundsKnown = false;
                _foodLayoutStamp = int.MinValue;
            }

            IgnoreFoodLayout(_foodPlate.gameObject);
            PlaceFoodPlate(food);
            return true;
        }

        static void IgnoreFoodLayout(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            LayoutElement layout = go.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = go.AddComponent<LayoutElement>();
            }

            layout.ignoreLayout = true;
        }

        static void PlaceFoodPlate(RectTransform food)
        {
            RectTransform plate = _foodPlate.rectTransform;
            plate.anchorMin = Vector2.zero;
            plate.anchorMax = Vector2.zero;
            plate.pivot = new Vector2(0.5f, 1f);
            plate.localScale = Vector3.one;
            plate.localRotation = Quaternion.identity;
            plate.SetAsLastSibling();

            int stamp = StampFoodSlots(food, plate);
            bool layoutChanged = stamp != _foodLayoutStamp;
            if (layoutChanged)
            {
                _foodLayoutStamp = stamp;
                if (TryFoodSlotBounds(food, plate, out float centerX, out float under))
                {
                    _foodCenterX = centerX;
                    _foodUnder = under;
                    _foodBoundsKnown = true;
                }
            }

            FitFoodPlate(layoutChanged);
        }

        static int StampFoodSlots(RectTransform food, RectTransform plate)
        {
            int hash = 17;
            int count = 0;
            for (int i = 0; i < food.childCount; i++)
            {
                Transform child = food.GetChild(i);
                if (!IsFoodSlot(child, plate))
                {
                    continue;
                }

                Vector2 pos = ((RectTransform)child).anchoredPosition;
                hash = hash * 31 + Mathf.RoundToInt(pos.x * 4f);
                hash = hash * 31 + Mathf.RoundToInt(pos.y * 4f);
                count++;
            }

            return hash * 31 + count;
        }

        static bool IsFoodTimer(Transform child)
        {
            return child is RectTransform && child.name.StartsWith("time", StringComparison.Ordinal);
        }

        static bool IsFoodSlot(Transform child, RectTransform plate)
        {
            if (!(child is RectTransform) || child == plate.transform)
            {
                return false;
            }

            if (child.name.StartsWith("SeneaLHudLayout", StringComparison.Ordinal))
            {
                return false;
            }

            return !IsFoodTimer(child);
        }

        static bool TryFoodSlotBounds(RectTransform food, RectTransform plate, out float centerX, out float under)
        {
            centerX = _foodCenterX;
            under = _foodUnder;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            int count = 0;
            for (int i = 0; i < food.childCount; i++)
            {
                Transform child = food.GetChild(i);
                if (!IsFoodSlot(child, plate))
                {
                    continue;
                }

                Vector2 pos = ((RectTransform)child).anchoredPosition;
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                count++;
            }

            if (count == 0)
            {
                return _foodBoundsKnown;
            }

            centerX = (minX + maxX) * 0.5f;
            under = minY - 1f;
            return true;
        }

        static void FitFoodPlate(bool layoutChanged)
        {
            if (_foodPlate == null || _foodLevel == null)
            {
                return;
            }

            float nudgeX = SeneaLHudLayoutPlugin.ShowLevelFoodBarOffsetX != null ? SeneaLHudLayoutPlugin.ShowLevelFoodBarOffsetX.Value : 12f;
            float nudgeY = SeneaLHudLayoutPlugin.ShowLevelFoodBarOffsetY != null ? SeneaLHudLayoutPlugin.ShowLevelFoodBarOffsetY.Value : -11f;
            bool textChanged = !string.Equals(_foodPlacedText, _foodLevel.text, StringComparison.Ordinal);
            bool nudgeChanged = !Mathf.Approximately(nudgeX, _foodPlacedNudgeX) || !Mathf.Approximately(nudgeY, _foodPlacedNudgeY);
            if (!layoutChanged && !textChanged && !nudgeChanged)
            {
                return;
            }

            _foodLevel.alignment = TextAlignmentOptions.Center;
            float width = _foodPlacedWidth;
            if (textChanged || width < 8f)
            {
                _foodLevel.ForceMeshUpdate();
                width = Mathf.Max(8f, Mathf.Ceil(_foodLevel.preferredWidth) + 6f);
                _foodPlacedText = _foodLevel.text;
                _foodPlacedWidth = width;
            }

            RectTransform plate = _foodPlate.rectTransform;
            plate.pivot = new Vector2(0.5f, 1f);
            plate.anchoredPosition = new Vector2(_foodCenterX + nudgeX, _foodUnder + nudgeY);
            plate.sizeDelta = new Vector2(width, 16f);

            _foodPlacedNudgeX = nudgeX;
            _foodPlacedNudgeY = nudgeY;
        }

        public static void Frame(Image plate)
        {
            plate.sprite = Pixel();
            plate.color = new Color(0.62f, 0.48f, 0.28f, 0.95f);
            var go = new GameObject("fill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(plate.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(1f, 1f);
            rt.offsetMax = new Vector2(-1f, -1f);
            var fill = go.GetComponent<Image>();
            fill.sprite = Pixel();
            fill.color = new Color(0.07f, 0.05f, 0.03f, 0.72f);
            fill.raycastTarget = false;
        }

        public static Sprite Plate()
        {
            return Pixel();
        }

        /// <summary>SeneaL crafting panel inner fill (sampled from the live craft UI when open).</summary>
        public static Color CraftPanelInnerFill()
        {
            return TrySampleCraftInnerFill(out Color color) ? color : new Color(0.07f, 0.05f, 0.03f, 0.72f);
        }

        public static bool TrySampleCraftInnerFill(out Color color)
        {
            color = default;
            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
            {
                return false;
            }

            Transform[] transforms = gui.GetComponentsInChildren<Transform>(true);
            Color best = default;
            float bestScore = -1f;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate.name != "craft" || candidate.Find("search") == null || candidate.Find("orn") == null)
                {
                    continue;
                }

                Image[] images = candidate.GetComponentsInChildren<Image>(true);
                for (int j = 0; j < images.Length; j++)
                {
                    Image img = images[j];
                    if (img == null || img.color.a < 0.4f)
                    {
                        continue;
                    }

                    Color c = img.color;
                    float luma = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                    if (luma > 0.28f)
                    {
                        continue;
                    }

                    float score = (1f - luma) * c.a;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }

                if (bestScore >= 0f)
                {
                    color = best;
                    return true;
                }
            }

            return false;
        }

        static Sprite Pixel()
        {
            if (_pixel != null)
            {
                return _pixel;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            _pixel = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
            return _pixel;
        }

        static int ReadLevel(Player player)
        {
            if (!_levelLookup)
            {
                _levelLookup = true;
                Type type = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Progression.LevelSkillService");
                _getLevel = type == null ? null : AccessTools.Method(type, "GetLevel", new[] { typeof(Player) });
                if (_getLevel == null && !_loggedLevel)
                {
                    _loggedLevel = true;
                    Debug.Log("[SeneaL HUD Layout] SkillsReworked level is unavailable, so the character level stays hidden.");
                }
            }

            if (_getLevel == null)
            {
                return -1;
            }

            try
            {
                return (int)_getLevel.Invoke(null, new object[] { player });
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static void Apply(Harmony harmony)
        {
            System.Type view = AccessTools.TypeByName("SeneaLUI.Hud.MinimapView");
            MethodInfo tick = AccessTools.Method(view, "Tick");
            if (tick != null)
            {
                harmony.Patch(tick, postfix: new HarmonyMethod(typeof(CharacterExtras), nameof(AfterMinimapTick)));
            }
        }

        static void AfterMinimapTick()
        {
            TickMinimap();
        }

        static void TickMinimap()
        {
            bool hide = SeneaLHudLayoutPlugin.HideMinimapStats != null && SeneaLHudLayoutPlugin.HideMinimapStats.Value;
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
            {
                return;
            }

            Transform root = hud.m_rootObject.transform.Find("SeneaLUI_Hud");
            Transform layer = root != null ? root.Find("mapFade/Minimap") : null;
            if (layer == null)
            {
                return;
            }

            Transform map = layer.Find("minimap");
            bool mapOn = map != null && map.gameObject.activeSelf;
            bool off = hide || !mapOn;
            SetPill(layer.Find("wind"), off);
            SetPill(layer.Find("time"), off);
            SetPill(layer.Find("info"), off);
        }

        static void SetPill(Transform pill, bool hidden)
        {
            if (pill != null && pill.gameObject.activeSelf == hidden)
            {
                pill.gameObject.SetActive(!hidden);
            }
        }

        static string GetPath(Transform t, Transform root) // added for debug
        {
            string path = t.name;

            while (t.parent != null && t.parent != root)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
