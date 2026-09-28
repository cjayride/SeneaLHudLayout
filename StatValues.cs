using System;
using System.Collections;
using System.Collections.Generic;
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
        static readonly List<Job> _jobs = new List<Job>();

        sealed class Job
        {
            public TMP_Text Label;
            public TMP_Text A;
            public TMP_Text B;
            public TMP_Text C;
            public TMP_Text D;
            public int Frames;
        }

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

        [HarmonyPriority(Priority.Last)]
        static void AfterLayRow(object __1)
        {
            if (!On || __1 == null)
            {
                return;
            }

            Beside(Text(__1, "Label"), Text(__1, "Value"), Text(__1, "Small"));
        }

        [HarmonyPriority(Priority.Last)]
        static void AfterPlaceStat(object __0)
        {
            if (!On || __0 == null)
            {
                return;
            }

            Queue(Text(__0, "Label"), Text(__0, "Value"), Text(__0, "Small"), null, null);
        }

        [HarmonyPriority(Priority.Last)]
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
                Queue(Text(row, "Label"), Text(row, "Cur"), Text(row, "Arrow"), Text(row, "Value"), Text(row, "Delta"));
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

            RectTransform labelRect = label.rectTransform;
            float cursor = labelRect.anchoredPosition.x - labelRect.pivot.x * ShownWidth(labelRect);
            if (!wrapped)
            {
                cursor += Width(label) + Gap;
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
                rect.anchorMin = new Vector2(labelRect.anchorMin.x, rect.anchorMin.y);
                rect.anchorMax = new Vector2(labelRect.anchorMin.x, rect.anchorMax.y);
                rect.pivot = new Vector2(0f, rect.pivot.y);
                part.alignment = TextAlignmentOptions.TopLeft;
                part.textWrappingMode = TextWrappingModes.NoWrap;
                part.overflowMode = TextOverflowModes.Overflow;
                rect.sizeDelta = new Vector2(width + 2f, rect.sizeDelta.y);
                Vector2 pos = rect.anchoredPosition;
                pos.x = cursor;
                rect.anchoredPosition = pos;
                cursor += width + Gap;
            }
        }

        public static void Tick()
        {
            for (int i = _jobs.Count - 1; i >= 0; i--)
            {
                Job job = _jobs[i];
                if (job.Label == null)
                {
                    _jobs.RemoveAt(i);
                    continue;
                }

                PlaceCraft(job.Label, job.A, job.B, job.C, job.D);
                job.Frames++;
                if (job.Frames >= 8 || Settled(job.Label))
                {
                    _jobs.RemoveAt(i);
                }
            }
        }

        static void Queue(TMP_Text label, TMP_Text a, TMP_Text b, TMP_Text c, TMP_Text d)
        {
            PlaceCraft(label, a, b, c, d);
            if (label == null)
            {
                return;
            }

            for (int i = 0; i < _jobs.Count; i++)
            {
                if (_jobs[i].Label == label)
                {
                    _jobs[i].A = a;
                    _jobs[i].B = b;
                    _jobs[i].C = c;
                    _jobs[i].D = d;
                    _jobs[i].Frames = 0;
                    return;
                }
            }

            _jobs.Add(new Job { Label = label, A = a, B = b, C = c, D = d });
        }

        static void PlaceCraft(TMP_Text label, TMP_Text a, TMP_Text b, TMP_Text c, TMP_Text d)
        {
            if (label == null)
            {
                return;
            }

            TMP_Text[] parts = { a, b, c, d };
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

            RectTransform labelRect = label.rectTransform;
            float rectWidth = labelRect.rect.width > 1f ? labelRect.rect.width : Mathf.Max(0f, labelRect.sizeDelta.x);
            float cursor = labelRect.anchoredPosition.x - labelRect.pivot.x * rectWidth;
            if (!wrapped)
            {
                int horizontal = (int)label.alignment & 0xFF;
                bool leftAlign = horizontal == 0 || horizontal == 1;
                float textWidth = Preferred(label);
                if (!leftAlign)
                {
                    cursor += Mathf.Max(0f, rectWidth - textWidth);
                }

                cursor += textWidth + Gap;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                TMP_Text part = parts[i];
                if (!Shown(part))
                {
                    continue;
                }

                float width = Preferred(part);
                RectTransform rect = part.rectTransform;
                rect.anchorMin = new Vector2(labelRect.anchorMin.x, rect.anchorMin.y);
                rect.anchorMax = new Vector2(labelRect.anchorMin.x, rect.anchorMax.y);
                rect.pivot = new Vector2(0f, rect.pivot.y);
                part.alignment = TextAlignmentOptions.TopLeft;
                part.textWrappingMode = TextWrappingModes.NoWrap;
                part.overflowMode = TextOverflowModes.Overflow;
                rect.sizeDelta = new Vector2(width + 2f, rect.sizeDelta.y);
                Vector2 pos = rect.anchoredPosition;
                pos.x = cursor;
                rect.anchoredPosition = pos;
                cursor += width + Gap;
            }
        }

        static bool Settled(TMP_Text label)
        {
            if (!Shown(label) || label.font == null || label.fontSize < 1f)
            {
                return false;
            }

            float expect = Mathf.Max(6f, label.text.Length * label.fontSize * 0.28f);
            return Preferred(label) >= expect;
        }

        static float Preferred(TMP_Text text)
        {
            float width = text.GetPreferredValues(text.text).x;
            if (width < 0.5f)
            {
                text.ForceMeshUpdate();
                width = text.preferredWidth;
            }

            return Mathf.Max(0f, width);
        }

        static float ShownWidth(RectTransform rect)
        {
            float width = rect.rect.width;
            if (width > 1f)
            {
                return width;
            }

            return Mathf.Max(0f, rect.sizeDelta.x);
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
