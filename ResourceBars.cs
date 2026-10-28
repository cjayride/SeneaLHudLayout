
using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class ResourceBars
    {
        static Type _hudRoot;
        static Type _staminaView;
        static Type _row;
        static FieldInfo _viewField;
        static FieldInfo _staminaRow;
        static FieldInfo _eitrRow;
        static FieldInfo _adrenalineRow;
        static FieldInfo _groupField;
        static FieldInfo _badgeField;
        static FieldInfo _badgeText;
        static FieldInfo _rowRoot;
        static bool _ready;

        static readonly Dictionary<int, SavedText> _saved =
            new Dictionary<int, SavedText>();

        static readonly List<TextMeshProUGUI> _tmp =
            new List<TextMeshProUGUI>(4);

        struct SavedText
        {
            public FontStyles Style;
            public float OutlineWidth;
            public Color OutlineColor;
            public FontWeight Weight;
            public float FontSize;
            public float FaceDilate;
            public float MatOutline;
            public Color MatOutlineColor;
            public Material Shared;
        }

        public static void Apply()
        {
            if (!Ready())
                return;

            object view = _viewField.GetValue(null);
            if (view == null)
            {
                _saved.Clear();
                return;
            }

            if (On(SeneaLHudLayoutPlugin.AlwaysShowStamina))
                Show(_staminaRow.GetValue(view));

            if (On(SeneaLHudLayoutPlugin.AlwaysShowEitr))
                Show(_eitrRow.GetValue(view));

            if (On(SeneaLHudLayoutPlugin.AlwaysShowAdrenaline))
                Show(_adrenalineRow.GetValue(view));

            ApplyNumberWeight(view);
        }

        static bool On(object entry)
        {
            var config = entry as ConfigEntry<bool>;
            return config != null && config.Value;
        }

        static void Show(object row)
        {
            if (row == null || _rowRoot == null || _groupField == null)
                return;

            RectTransform root = _rowRoot.GetValue(row) as RectTransform;
            CanvasGroup group = _groupField.GetValue(row) as CanvasGroup;

            if (!root || !group)
                return;

            root.gameObject.SetActive(true);
            group.gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            // Keep SeneaLUI's parent FX group from fading the row out.
            CanvasGroup[] parents =
                root.GetComponentsInParent<CanvasGroup>(true);

            foreach (CanvasGroup parent in parents)
            {
                if (parent != null && parent.name == "fx")
                {
                    parent.gameObject.SetActive(true);
                    parent.alpha = 1f;
                }
            }
        }

        static void ApplyNumberWeight(object view)
        {
            if (SeneaLHudLayoutPlugin.VitalsNumberBold == null)
                return;

            float bold = Mathf.Clamp01(
                SeneaLHudLayoutPlugin.VitalsNumberBold.Value);

            StyleRow(_staminaRow.GetValue(view), bold);
            StyleRow(_eitrRow.GetValue(view), bold);
            StyleRow(_adrenalineRow.GetValue(view), bold);
        }

        static void StyleRow(object row, float bold)
        {
            if (row == null)
                return;

            _tmp.Clear();

            if (_badgeField != null && _badgeText != null)
            {
                object badge = _badgeField.GetValue(row);

                if (badge != null &&
                    _badgeText.GetValue(badge) is TextMeshProUGUI badgeText &&
                    badgeText)
                {
                    _tmp.Add(badgeText);
                }
            }

            if (_rowRoot != null &&
                _rowRoot.GetValue(row) is RectTransform root &&
                root)
            {
                root.GetComponentsInChildren(true, _tmp);
            }

            for (int i = 0; i < _tmp.Count; i++)
                StyleTmp(_tmp[i], bold);
        }

        static void StyleTmp(TextMeshProUGUI text, float bold)
        {
            if (!text)
                return;

            int id = text.GetInstanceID();
            Material mat = text.fontMaterial;

            if (!mat)
                return;

            if (!_saved.TryGetValue(id, out SavedText saved))
            {
                saved = new SavedText
                {
                    Style = text.fontStyle,
                    OutlineWidth = text.outlineWidth,
                    OutlineColor = text.outlineColor,
                    Weight = text.fontWeight,
                    FontSize = text.fontSize,
                    FaceDilate = mat.HasProperty(ShaderUtilities.ID_FaceDilate)
                        ? mat.GetFloat(ShaderUtilities.ID_FaceDilate)
                        : 0f,
                    MatOutline = mat.HasProperty(ShaderUtilities.ID_OutlineWidth)
                        ? mat.GetFloat(ShaderUtilities.ID_OutlineWidth)
                        : 0f,
                    MatOutlineColor = mat.HasProperty(ShaderUtilities.ID_OutlineColor)
                        ? mat.GetColor(ShaderUtilities.ID_OutlineColor)
                        : Color.black,
                    Shared = text.fontSharedMaterial
                };

                _saved[id] = saved;
            }

            if (bold <= 0.001f)
            {
                text.fontStyle = saved.Style;
                text.fontWeight = saved.Weight;
                text.outlineWidth = saved.OutlineWidth;
                text.outlineColor = saved.OutlineColor;
                text.fontSize = saved.FontSize;

                if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
                    mat.SetFloat(
                        ShaderUtilities.ID_FaceDilate, saved.FaceDilate);

                if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
                    mat.SetFloat(
                        ShaderUtilities.ID_OutlineWidth, saved.MatOutline);

                if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
                    mat.SetColor(
                        ShaderUtilities.ID_OutlineColor, saved.MatOutlineColor);

                return;
            }

            text.fontStyle = saved.Style | FontStyles.Bold;
            text.fontWeight = FontWeight.Black;
            text.fontSize = saved.FontSize * (1f + 0.18f * bold);
            text.outlineWidth = Mathf.Lerp(saved.OutlineWidth, 0.55f, bold);
            text.outlineColor = new Color(
                0f, 0f, 0f, Mathf.Lerp(0.7f, 1f, bold));

            mat.EnableKeyword("OUTLINE_ON");

            if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
                mat.SetFloat(
                    ShaderUtilities.ID_FaceDilate,
                    Mathf.Lerp(saved.FaceDilate, 0.45f, bold));

            if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
                mat.SetFloat(
                    ShaderUtilities.ID_OutlineWidth,
                    Mathf.Lerp(saved.MatOutline, 0.5f, bold));

            if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
                mat.SetColor(
                    ShaderUtilities.ID_OutlineColor,
                    new Color(0f, 0f, 0f, 1f));

            if (mat.HasProperty(ShaderUtilities.ID_OutlineSoftness))
                mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        }

        static void PatchStaminaTick()
        {
            var harmony =
                new Harmony("SeneaLHudLayout.ResourceBarsVisibility");

            MethodInfo rowTick = AccessTools.Method(
                _row,
                "Tick",
                new[]
                {
                    typeof(float),
                    typeof(float),
                    typeof(float),
                    typeof(bool)
                });

            MethodInfo beforeRow = AccessTools.Method(
                typeof(ResourceBars), nameof(BeforeRowTick));

            MethodInfo afterRow = AccessTools.Method(
                typeof(ResourceBars), nameof(AfterRowTick));

            if (rowTick == null || beforeRow == null || afterRow == null)
            {
                Debug.LogError(
                    "[SeneaL HUD Layout] Could not find Row.Tick or its patches.");
                return;
            }

            harmony.Patch(
                rowTick,
                prefix: new HarmonyMethod(beforeRow),
                postfix: new HarmonyMethod(afterRow));

            MethodInfo viewTick = AccessTools.Method(_staminaView, "Tick");

            MethodInfo afterView = AccessTools.Method(
                typeof(ResourceBars), nameof(AfterStaminaViewTick));

            if (viewTick == null || afterView == null)
            {
                Debug.LogError(
                    "[SeneaL HUD Layout] Could not find StaminaView.Tick or its postfix.");
                return;
            }

            harmony.Patch(
                viewTick,
                postfix: new HarmonyMethod(afterView));

            Debug.Log(
                "[SeneaL HUD Layout] Patched Row.Tick and StaminaView.Tick.");
        }

        static void AfterStaminaViewTick(object __instance)
        {
            if (__instance == null)
                return;

            if (On(SeneaLHudLayoutPlugin.AlwaysShowStamina))
                ForceRowVisible(_staminaRow.GetValue(__instance));

            if (On(SeneaLHudLayoutPlugin.AlwaysShowEitr))
                ForceRowVisible(_eitrRow.GetValue(__instance));

            if (On(SeneaLHudLayoutPlugin.AlwaysShowAdrenaline))
                ForceRowVisible(_adrenalineRow.GetValue(__instance));
        }

        static void ForceRowVisible(object row)
        {
            if (row == null)
                return;

            Show(row);

            // Prevent SeneaLUI's idle timer from hiding the row.
            FieldInfo timerField = AccessTools.Field(_row, "Timer");
            if (timerField != null)
                timerField.SetValue(row, 0f);
        }

        static void BeforeRowTick(object __instance, ref bool force)
        {
            if (__instance != null && IsAlwaysShownRow(__instance))
                force = true;
        }

        static void AfterRowTick(object __instance)
        {
            if (__instance == null || !IsAlwaysShownRow(__instance))
                return;

            Show(__instance);
        }

        static bool IsAlwaysShownRow(object row)
        {
            object view = _viewField?.GetValue(null);

            if (view == null)
                return false;

            if (On(SeneaLHudLayoutPlugin.AlwaysShowStamina) &&
                ReferenceEquals(row, _staminaRow?.GetValue(view)))
                return true;

            if (On(SeneaLHudLayoutPlugin.AlwaysShowEitr) &&
                ReferenceEquals(row, _eitrRow?.GetValue(view)))
                return true;

            if (On(SeneaLHudLayoutPlugin.AlwaysShowAdrenaline) &&
                ReferenceEquals(row, _adrenalineRow?.GetValue(view)))
                return true;

            return false;
        }

        static bool Ready()
        {
            if (_ready)
            {
                return _viewField != null
                    && _staminaRow != null
                    && _eitrRow != null
                    && _adrenalineRow != null
                    && _groupField != null
                    && _rowRoot != null;
            }

            _hudRoot = AccessTools.TypeByName("SeneaLUI.Hud.HudRoot");
            _staminaView = AccessTools.TypeByName("SeneaLUI.Hud.StaminaView");

            if (_hudRoot == null || _staminaView == null)
            {
                Debug.LogError(
                    "[SeneaL HUD Layout] Type lookup failed: " +
                    $"HudRoot={_hudRoot != null}, " +
                    $"StaminaView={_staminaView != null}");

                return false;
            }

            _row = AccessTools.Inner(_staminaView, "Row");

            if (_row == null)
            {
                Debug.LogError(
                    "[SeneaL HUD Layout] Could not find nested Row type.");
                return false;
            }

            _viewField = AccessTools.Field(_hudRoot, "_stamina");
            _staminaRow = AccessTools.Field(_staminaView, "_stamina");
            _eitrRow = AccessTools.Field(_staminaView, "_eitr");
            _adrenalineRow = AccessTools.Field(_staminaView, "_adrenaline");
            _groupField = AccessTools.Field(_row, "Group");
            _badgeField = AccessTools.Field(_row, "Badge");
            _rowRoot = AccessTools.Field(_row, "Root");

            Type badge = AccessTools.TypeByName("SeneaLUI.Components.Badge");

            _badgeText = badge != null
                ? AccessTools.Field(badge, "_text")
                : null;

            bool ready = _viewField != null
                && _staminaRow != null
                && _eitrRow != null
                && _adrenalineRow != null
                && _groupField != null
                && _rowRoot != null;

            _ready = ready;

            if (!ready)
            {
                Debug.LogError(
                    "[SeneaL HUD Layout] Required fields were not found.");
                return false;
            }

            Debug.Log(
                "[SeneaL HUD Layout] Ready=True, " +
                $"Row={_row.FullName}, " +
                $"StaminaView={_staminaView.FullName}");

            PatchStaminaTick();
            return true;
        }
    }
}
