using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace SeneaLHudLayout
{
static class RepairTipPlace
{
static Type _type;
static FieldInfo _anchor;
static FieldInfo _root;
static FieldInfo _panel;
static FieldInfo _arrow;
static FieldInfo _arrowIn;
static FieldInfo _width;
static FieldInfo _height;
static FieldInfo _arrowOn;

    const float ScreenPadding = 8f;
    const float MinimumWidth = 256f;
    const float MaximumWidth = 900f;
    const float ContentPadding = 44f;

    static float _repairWidth;

    public static void Apply(Harmony harmony)
    {
        _type = AccessTools.TypeByName("SeneaLUI.Components.Tooltip");
        if (_type == null)
            return;

        _anchor = AccessTools.Field(_type, "_anchor");
        _root = AccessTools.Field(_type, "_root");
        _panel = AccessTools.Field(_type, "_panel");
        _arrow = AccessTools.Field(_type, "_arrow");
        _arrowIn = AccessTools.Field(_type, "_arrowIn");
        _width = AccessTools.Field(_type, "_width");
        _height = AccessTools.Field(_type, "_height");
        _arrowOn = AccessTools.Field(_type, "_arrowOn");

        MethodInfo build = AccessTools.Method(_type, "Build");
        MethodInfo layout = AccessTools.Method(_type, "Layout");
        MethodInfo position = AccessTools.Method(_type, "Position");

        if (build != null)
        {
            harmony.Patch(
                build,
                prefix: new HarmonyMethod(
                    typeof(RepairTipPlace),
                    nameof(BeforeBuild)
                )
            );
        }

        if (layout != null)
        {
            harmony.Patch(
                layout,
                prefix: new HarmonyMethod(
                    typeof(RepairTipPlace),
                    nameof(BeforeLayout)
                ),
                transpiler: new HarmonyMethod(
                    typeof(RepairTipPlace),
                    nameof(WidenLayout)
                )
            );
        }

        if (position != null)
        {
            harmony.Patch(
                position,
                postfix: new HarmonyMethod(
                    typeof(RepairTipPlace),
                    nameof(AfterPosition)
                )
            );
        }
    }

    static void BeforeBuild(object __0)
    {
        PrepareRepairWidth(__0);
    }

    static void BeforeLayout(object __0)
    {
        PrepareRepairWidth(__0);
    }

    static void PrepareRepairWidth(object tip)
    {
        _repairWidth = 0f;

        if (tip == null)
            return;

        RectTransform anchor = Get<RectTransform>(_anchor);

        if (anchor == null || !IsRepair(anchor))
            return;

        float required = MeasureTip(tip) + 100f;
        RectTransform root = Get<RectTransform>(_root);

        if (root != null)
        {
            float maxWidth = Mathf.Max(
                MinimumWidth,
                root.rect.width - ScreenPadding * 2f
            );

            required = Mathf.Min(required, maxWidth);
        }

        required = Mathf.Clamp(
            required,
            MinimumWidth,
            MaximumWidth
        );

        _repairWidth = required;

        FieldInfo tipWidth = AccessTools.Field(
            tip.GetType(),
            "Width"
        );

        if (tipWidth != null &&
            tipWidth.FieldType == typeof(float?))
        {
            tipWidth.SetValue(tip, (float?)required);
        }
    }

    // Tooltip.Layout normally forces its content width to 228 pixels.
    // When Width is set, use the requested width minus the panel padding.
    static float GetInnerWidth(object tip)
    {
        if (tip == null)
            return 228f;

        FieldInfo tipWidth = AccessTools.Field(
            tip.GetType(),
            "Width"
        );

        if (tipWidth == null ||
            tipWidth.GetValue(tip) is not float width)
        {
            return 228f;
        }

        return Mathf.Max(228f, width - 28f);
    }

    static IEnumerable<CodeInstruction> WidenLayout(
        IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes =
            new List<CodeInstruction>(instructions);

        MethodInfo getInnerWidth = AccessTools.Method(
            typeof(RepairTipPlace),
            nameof(GetInnerWidth)
        );

        for (int i = 1; i < codes.Count; i++)
        {
            if (codes[i].opcode != OpCodes.Stsfld ||
                !(codes[i].operand is FieldInfo field) ||
                field.Name != "_inner")
            {
                continue;
            }

            if (codes[i - 1].opcode != OpCodes.Ldc_R4 ||
                !(codes[i - 1].operand is float value) ||
                Mathf.Abs(value - 228f) > 0.001f)
            {
                continue;
            }

            // Replace:
            // _inner = 228f;
            //
            // With:
            // _inner = GetInnerWidth(tip);

            codes[i - 1] = new CodeInstruction(OpCodes.Ldarg_0);

            codes.Insert(
                i,
                new CodeInstruction(
                    OpCodes.Call,
                    getInnerWidth
                )
            );

            break;
        }

        return codes;
    }

    static float MeasureTip(object tip)
    {
        float required = MinimumWidth;

        RectTransform panel = Get<RectTransform>(_panel);

        if (panel == null)
            return required;

        TMP_Text[] texts =
            panel.GetComponentsInChildren<TMP_Text>(true);

        string[] content =
        {
            ReadString(tip, "Title"),
            ReadString(tip, "Sub"),
            ReadString(tip, "Body"),
            ReadString(tip, "Note"),
            ReadString(tip, "Warn")
        };

        foreach (string value in content)
        {
            if (string.IsNullOrEmpty(value))
                continue;

            foreach (TMP_Text text in texts)
            {
                if (text == null)
                    continue;

                float preferred = text.GetPreferredValues(
                    value,
                    10000f,
                    0f
                ).x;

                float scale =
                    text.rectTransform.lossyScale.x > 0.0001f
                        ? panel.lossyScale.x /
                          text.rectTransform.lossyScale.x
                        : 1f;

                required = Mathf.Max(
                    required,
                    preferred * scale + ContentPadding
                );
            }
        }

        FieldInfo rowsField = AccessTools.Field(
            tip.GetType(),
            "Rows"
        );

        if (rowsField != null &&
            rowsField.GetValue(tip) is IEnumerable rows)
        {
            foreach (object row in rows)
            {
                if (row == null)
                    continue;

                string label = ReadString(row, "Label");
                string rowValue = ReadString(row, "Value");
                string small = ReadString(row, "Small");

                foreach (string value in new[]
                {
                    label,
                    rowValue,
                    small
                })
                {
                    if (string.IsNullOrEmpty(value))
                        continue;

                    foreach (TMP_Text text in texts)
                    {
                        if (text == null)
                            continue;

                        float preferred = text.GetPreferredValues(
                            value,
                            10000f,
                            0f
                        ).x;

                        float scale =
                            text.rectTransform.lossyScale.x > 0.0001f
                                ? panel.lossyScale.x /
                                  text.rectTransform.lossyScale.x
                                : 1f;

                        required = Mathf.Max(
                            required,
                            preferred * scale + ContentPadding
                        );
                    }
                }
            }
        }

        return required;
    }

    static string ReadString(object instance, string name)
    {
        if (instance == null)
            return null;

        FieldInfo field = AccessTools.Field(
            instance.GetType(),
            name
        );

        return field != null &&
               field.FieldType == typeof(string)
            ? field.GetValue(instance) as string
            : null;
    }

    static void AfterPosition()
    {
        RectTransform anchor = Get<RectTransform>(_anchor);
        RectTransform root = Get<RectTransform>(_root);
        RectTransform panel = Get<RectTransform>(_panel);
        RectTransform arrow = Get<RectTransform>(_arrow);
        RectTransform arrowIn = Get<RectTransform>(_arrowIn);

        if (anchor == null ||
            root == null ||
            panel == null ||
            arrow == null ||
            arrowIn == null ||
            !IsRepair(anchor) ||
            _repairWidth <= 0f)
        {
            return;
        }

        Rect rect = root.rect;
        float screenW = rect.width;
        float screenH = rect.height;

        float width = Mathf.Min(
            _repairWidth,
            screenW - ScreenPadding * 2f
        );

        float height = Get<float>(_height);
        float originalWidth = Get<float>(_width);

        float scale = root.lossyScale.x > 0.0001f
            ? anchor.lossyScale.x / root.lossyScale.x
            : 1f;

        Vector2 anchorSize = anchor.rect.size * scale;

        Vector3 center = root.InverseTransformPoint(
            anchor.TransformPoint(anchor.rect.center)
        );

        float fromLeft = center.x - rect.xMin;
        float belowTop =
            rect.yMax - center.y + anchorSize.y * 0.5f;

        panel.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width
        );

        // Preserve the existing positioning behavior.
        float x = fromLeft + originalWidth * 0.5f - width;

        x = Mathf.Clamp(
            x,
            ScreenPadding,
            Mathf.Max(
                ScreenPadding,
                screenW - width - ScreenPadding
            )
        );

        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);

        float y = belowTop + 12f;

        float fitted = y + height > screenH - ScreenPadding
            ? Mathf.Max(
                ScreenPadding,
                screenH - ScreenPadding - height
            )
            : y;

        bool showArrow = Mathf.Abs(fitted - y) < 0.5f;

        panel.anchoredPosition = new Vector2(x, -fitted);

        float arrowX = Mathf.Clamp(
            fromLeft - x,
            16f,
            Mathf.Max(16f, width - 16f)
        );

        Vector2 arrowPos = new Vector2(
            arrowX - width * 0.5f,
            height * 0.5f
        );

        arrow.anchoredPosition = arrowPos;
        arrowIn.anchoredPosition = arrowPos;

        arrow.SetSiblingIndex(0);
        arrowIn.SetAsLastSibling();

        SetArrow(arrow, arrowIn, showArrow);
    }

    static bool IsRepair(RectTransform anchor)
    {
        return anchor.name == "repair" ||
               anchor.name == "RepairButton";
    }

    static void SetArrow(
        RectTransform arrow,
        RectTransform arrowIn,
        bool on)
    {
        if (_arrowOn != null &&
            _arrowOn.GetValue(null) is bool current &&
            current == on &&
            arrow.gameObject.activeSelf == on &&
            arrowIn.gameObject.activeSelf == on)
        {
            return;
        }

        if (_arrowOn != null)
            _arrowOn.SetValue(null, on);

        if (arrow.gameObject.activeSelf != on)
            arrow.gameObject.SetActive(on);

        if (arrowIn.gameObject.activeSelf != on)
            arrowIn.gameObject.SetActive(on);
    }

    static T Get<T>(FieldInfo field)
    {
        if (field == null)
            return default(T);

        object value = field.GetValue(null);

        return value is T typed
            ? typed
            : default(T);
    }
}

}