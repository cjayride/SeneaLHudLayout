using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;

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
        static readonly Dictionary<int, SavedText> _saved = new Dictionary<int, SavedText>();
        static readonly List<TextMeshProUGUI> _tmp = new List<TextMeshProUGUI>(4);

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
            {
                return;
            }

            object view = _viewField.GetValue(null);
            if (view == null)
            {
                _saved.Clear();
                return;
            }

            if (On(SeneaLHudLayoutPlugin.AlwaysShowStamina))
            {
                Show(_staminaRow.GetValue(view));
            }

            if (On(SeneaLHudLayoutPlugin.AlwaysShowEitr))
            {
                Show(_eitrRow.GetValue(view));
            }

            if (On(SeneaLHudLayoutPlugin.AlwaysShowAdrenaline))
            {
                Show(_adrenalineRow.GetValue(view));
            }

            ApplyNumberWeight(view);
        }

        static bool On(object entry)
        {
            var config = entry as ConfigEntry<bool>;
            return config != null && config.Value;
        }

        static void Show(object row)
        {
            if (row == null)
            {
                return;
            }

            CanvasGroup group = _groupField.GetValue(row) as CanvasGroup;
            if (!group)
            {
                return;
            }

            if (!group.gameObject.activeSelf)
            {
                group.gameObject.SetActive(true);
            }

            if (group.alpha < 1f)
            {
                group.alpha = 1f;
            }
        }

        static void ApplyNumberWeight(object view)
        {
            if (SeneaLHudLayoutPlugin.VitalsNumberBold == null)
            {
                return;
            }

            float bold = Mathf.Clamp01(SeneaLHudLayoutPlugin.VitalsNumberBold.Value);
            StyleRow(_staminaRow.GetValue(view), bold);
            StyleRow(_eitrRow.GetValue(view), bold);
            StyleRow(_adrenalineRow.GetValue(view), bold);
        }

        static void StyleRow(object row, float bold)
        {
            if (row == null)
            {
                return;
            }

            _tmp.Clear();
            if (_badgeField != null && _badgeText != null)
            {
                object badge = _badgeField.GetValue(row);
                if (badge != null && _badgeText.GetValue(badge) is TextMeshProUGUI badgeText && badgeText)
                {
                    _tmp.Add(badgeText);
                }
            }

            if (_rowRoot != null && _rowRoot.GetValue(row) is RectTransform root && root)
            {
                root.GetComponentsInChildren(true, _tmp);
            }

            for (int i = 0; i < _tmp.Count; i++)
            {
                StyleTmp(_tmp[i], bold);
            }
        }

        static void StyleTmp(TextMeshProUGUI text, float bold)
        {
            if (!text)
            {
                return;
            }

            int id = text.GetInstanceID();
            Material mat = text.fontMaterial;
            if (!mat)
            {
                return;
            }

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
                {
                    mat.SetFloat(ShaderUtilities.ID_FaceDilate, saved.FaceDilate);
                }

                if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
                {
                    mat.SetFloat(ShaderUtilities.ID_OutlineWidth, saved.MatOutline);
                }

                if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
                {
                    mat.SetColor(ShaderUtilities.ID_OutlineColor, saved.MatOutlineColor);
                }

                return;
            }

            text.fontStyle = saved.Style | FontStyles.Bold;
            text.fontWeight = FontWeight.Black;
            text.fontSize = saved.FontSize * (1f + 0.18f * bold);
            text.outlineWidth = Mathf.Lerp(saved.OutlineWidth, 0.55f, bold);
            text.outlineColor = new Color(0f, 0f, 0f, Mathf.Lerp(0.7f, 1f, bold));
            mat.EnableKeyword("OUTLINE_ON");
            if (mat.HasProperty(ShaderUtilities.ID_FaceDilate))
            {
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, Mathf.Lerp(saved.FaceDilate, 0.45f, bold));
            }

            if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
            {
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, Mathf.Lerp(saved.MatOutline, 0.5f, bold));
            }

            if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
            {
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
            }

            if (mat.HasProperty(ShaderUtilities.ID_OutlineSoftness))
            {
                mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            }
        }

        static bool Ready()
        {
            if (_ready)
            {
                return _viewField != null;
            }

            _ready = true;
            _hudRoot = AccessTools.TypeByName("SeneaLUI.Hud.HudRoot");
            _staminaView = AccessTools.TypeByName("SeneaLUI.Hud.StaminaView");
            _row = AccessTools.Inner(_staminaView, "Row");
            _viewField = AccessTools.Field(_hudRoot, "_stamina");
            _staminaRow = AccessTools.Field(_staminaView, "_stamina");
            _eitrRow = AccessTools.Field(_staminaView, "_eitr");
            _adrenalineRow = AccessTools.Field(_staminaView, "_adrenaline");
            _groupField = AccessTools.Field(_row, "Group");
            _badgeField = AccessTools.Field(_row, "Badge");
            _rowRoot = AccessTools.Field(_row, "Root");
            Type badge = AccessTools.TypeByName("SeneaLUI.Components.Badge");
            _badgeText = badge != null ? AccessTools.Field(badge, "_text") : null;
            return _viewField != null && _groupField != null;
        }
    }
}
