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

        static Transform _tabs;
        static TextMeshProUGUI _level;
        static TextMeshProUGUI _foodLevel;
        static Image _foodPlate;
        static float _foodCenterX = 58f;
        static float _foodUnder;
        static Sprite _pixel;
        static MethodInfo _getLevel;
        static bool _levelLookup;
        static bool _loggedLevel;

        public static void Tick()
        {
            TickLevel();
            TickMinimap();
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

            FitFoodPlate();

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
            Transform vitals = seneal != null ? seneal.Find("Health/vitals") : null;
            RectTransform food = vitals != null ? vitals.Find("food") as RectTransform : null;
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
                    _foodPlate.sprite = Pixel();
                    _foodPlate.color = new Color(0.07f, 0.05f, 0.03f, 0.62f);
                    _foodPlate.type = Image.Type.Simple;

                    GameObject textGo = new GameObject("t");
                    textGo.transform.SetParent(plateGo.transform, false);
                    _foodLevel = textGo.AddComponent<TextMeshProUGUI>();
                    _foodLevel.raycastTarget = false;
                    _foodLevel.alignment = TextAlignmentOptions.MidlineLeft;
                    _foodLevel.textWrappingMode = TextWrappingModes.NoWrap;
                    _foodLevel.overflowMode = TextOverflowModes.Overflow;
                    _foodLevel.fontSize = 13f;
                    _foodLevel.fontStyle = FontStyles.Bold;
                    _foodLevel.color = new Color(0.93f, 0.82f, 0.52f, 1f);

                    RectTransform textRt = _foodLevel.rectTransform;
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = new Vector2(3f, 0f);
                    textRt.offsetMax = new Vector2(-3f, 0f);

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
            }

            PlaceFoodPlate(food);
            return true;
        }

        static void PlaceFoodPlate(RectTransform food)
        {
            RectTransform timer = food.Find("time2") as RectTransform;
            RectTransform plate = _foodPlate.rectTransform;
            plate.anchorMin = new Vector2(0f, 0f);
            plate.anchorMax = new Vector2(0f, 0f);
            plate.pivot = new Vector2(0f, 1f);
            float centerX = 58f;
            float under = 0f;
            if (timer != null)
            {
                float width = timer.rect.width;
                if (width < 4f || width > 80f)
                {
                    width = 40f;
                }

                float height = timer.rect.height;
                if (height < 4f || height > 28f)
                {
                    height = 14f;
                }

                centerX = timer.anchoredPosition.x + width * (0.5f - timer.pivot.x);
                under = timer.anchoredPosition.y - height * timer.pivot.y - 1f;
            }

            _foodCenterX = centerX;
            _foodUnder = under;
            plate.localScale = Vector3.one;
            plate.localRotation = Quaternion.identity;
            plate.SetAsLastSibling();
            FitFoodPlate();
        }

        static void FitFoodPlate()
        {
            if (_foodPlate == null || _foodLevel == null)
            {
                return;
            }

            _foodLevel.alignment = TextAlignmentOptions.MidlineLeft;
            string shown = _foodLevel.text;
            _foodLevel.text = "Level 00";
            _foodLevel.ForceMeshUpdate();
            float reference = Mathf.Ceil(_foodLevel.preferredWidth) + 6f;
            _foodLevel.text = shown;
            _foodLevel.ForceMeshUpdate();
            float width = Mathf.Ceil(_foodLevel.preferredWidth) + 6f;
            float nudgeX = SeneaLHudLayoutPlugin.ShowLevelFoodBarOffsetX != null ? SeneaLHudLayoutPlugin.ShowLevelFoodBarOffsetX.Value : -39f;
            float nudgeY = SeneaLHudLayoutPlugin.ShowLevelFoodBarOffsetY != null ? SeneaLHudLayoutPlugin.ShowLevelFoodBarOffsetY.Value : -20.4f;
            RectTransform plate = _foodPlate.rectTransform;
            plate.pivot = new Vector2(0f, 1f);
            plate.anchoredPosition = new Vector2(_foodCenterX + nudgeX - reference * 0.5f, _foodUnder + nudgeY);
            plate.sizeDelta = new Vector2(Mathf.Max(8f, width), 16f);
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
            SetPill(layer.Find("wind"), hide || !mapOn);
            SetPill(layer.Find("time"), hide || !mapOn);
        }

        static void SetPill(Transform pill, bool hidden)
        {
            if (pill != null && pill.gameObject.activeSelf == hidden)
            {
                pill.gameObject.SetActive(!hidden);
            }
        }
    }
}
