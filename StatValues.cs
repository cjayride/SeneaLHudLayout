using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class StatValues
    {
        const float Gap = 4f;

        static FieldInfo _cmp;

        public static void Apply(Harmony harmony)
        {
            Type tooltip = AccessTools.TypeByName("SeneaLUI.Components.Tooltip");
            MethodInfo layRow = tooltip != null ? AccessTools.Method(tooltip, "LayRow") : null;
            if (layRow != null)
            {
                harmony.Patch(layRow, postfix: new HarmonyMethod(typeof(StatValues), nameof(AfterLayRow)));
            }

            Type craft = AccessTools.TypeByName("SeneaLUI.Inv.CraftView");
            MethodInfo place = craft != null ? AccessTools.Method(craft, "PlaceStat") : null;
            if (place != null)
            {
                harmony.Patch(place, postfix: new HarmonyMethod(typeof(StatValues), nameof(AfterPlaceStat)));
            }

            MethodInfo compare = craft != null ? AccessTools.Method(craft, "BuildCompare") : null;
            if (compare != null)
            {
                _cmp = AccessTools.Field(craft, "_cmp");
                harmony.Patch(compare, postfix: new HarmonyMethod(typeof(StatValues), nameof(AfterCompare)));
            }
        }

        static bool On
        {
            get { return SeneaLHudLayoutPlugin.StatValuesBesideLabels == null || SeneaLHudLayoutPlugin.StatValuesBesideLabels.Value; }
        }

        static void AfterLayRow(object __1)
        {
            if (!On || __1 == null)
            {
                return;
            }

            Beside(Text(__1, "Label"), Text(__1, "Value"), Text(__1, "Small"));
        }

        static void AfterPlaceStat(object __0)
        {
            if (!On || __0 == null)
            {
                return;
            }

            Beside(Text(__0, "Label"), Text(__0, "Value"), Text(__0, "Small"));
        }

        static void AfterCompare(object __instance)
        {
            if (!On || __instance == null || _cmp == null)
            {
                return;
            }

            IList rows = _cmp.GetValue(__instance) as IList;
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                object row = rows[i];
                Beside(Text(row, "Label"), Text(row, "Cur"), Text(row, "Arrow"), Text(row, "Value"), Text(row, "Delta"));
            }
        }

        static void Beside(TMP_Text label, params TMP_Text[] parts)
        {
            if (label == null)
            {
                return;
            }

            bool wrapped = false;
            for (int i = 0; i < parts.Length; i++)
            {
                TMP_Text part = parts[i];
                if (!Shown(part))
                {
                    continue;
                }

                if (Mathf.Abs(part.rectTransform.anchoredPosition.y - label.rectTransform.anchoredPosition.y) > 8f)
                {
                    wrapped = true;
                }

                break;
            }

            float cursor = Edge(label, !wrapped);
            if (!wrapped)
            {
                cursor += Gap;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                TMP_Text part = parts[i];
                if (!Shown(part))
                {
                    continue;
                }

                float width = Width(part);
                RectTransform rect = part.rectTransform;
                part.alignment = TextAlignmentOptions.TopLeft;
                part.textWrappingMode = TextWrappingModes.NoWrap;
                part.overflowMode = TextOverflowModes.Overflow;
                rect.sizeDelta = new Vector2(width + 2f, rect.sizeDelta.y);
                float current = Edge(part, false);
                rect.anchoredPosition += new Vector2(cursor - current, 0f);
                cursor += width + Gap;
            }
        }

        static float Edge(TMP_Text text, bool right)
        {
            RectTransform rect = text.rectTransform;
            text.ForceMeshUpdate(true);
            Bounds bounds = text.textBounds;
            float local;
            if (bounds.size.x >= 0.5f)
            {
                local = right ? bounds.max.x : bounds.min.x;
            }
            else
            {
                float width = Width(text);
                float rectWidth = rect.rect.width > 1f ? rect.rect.width : rect.sizeDelta.x;
                float textLeft = (0f - rect.pivot.x) * rectWidth + Mathf.Max(0f, rectWidth - width) * Align(text);
                local = right ? textLeft + width : textLeft;
            }

            RectTransform parent = rect.parent as RectTransform;
            if (parent == null)
            {
                return local;
            }

            Vector3 world = rect.TransformPoint(new Vector3(local, 0f, 0f));
            return parent.InverseTransformPoint(world).x;
        }

        static float Align(TMP_Text text)
        {
            int horizontal = (int)text.alignment & 0xFF;
            if (horizontal == 2)
            {
                return 0.5f;
            }

            if (horizontal == 4 || horizontal == 8 || horizontal == 16)
            {
                return 1f;
            }

            return 0f;
        }

        static float Width(TMP_Text text)
        {
            text.ForceMeshUpdate();
            float width = text.preferredWidth;
            if (width < 0.5f)
            {
                width = text.GetPreferredValues(text.text).x;
            }

            return Mathf.Max(1f, width);
        }

        static bool Shown(TMP_Text text)
        {
            return text != null && text.gameObject.activeSelf && !string.IsNullOrEmpty(text.text);
        }

        static TMP_Text Text(object row, string field)
        {
            if (row == null)
            {
                return null;
            }

            FieldInfo info = AccessTools.Field(row.GetType(), field);
            return info != null ? info.GetValue(row) as TMP_Text : null;
        }
    }
}
