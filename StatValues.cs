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
        static readonly Dictionary<int, Snapshot> _original = new Dictionary<int, Snapshot>();
        static readonly List<TMP_Text> _tracked = new List<TMP_Text>();

        struct Snapshot
        {
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 SizeDelta;
            public Vector2 AnchoredPosition;
            public TextAlignmentOptions Alignment;
            public TextWrappingModes Wrap;
            public TextOverflowModes Overflow;
        }

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
            System.Type tooltip = AccessTools.TypeByName("SeneaLUI.Components.Tooltip");
            MethodInfo layRow = tooltip != null ? AccessTools.Method(tooltip, "LayRow") : null;
            if (layRow != null)
            {
                harmony.Patch(layRow, postfix: new HarmonyMethod(typeof(StatValues), nameof(AfterLayRow)));
            }

            System.Type craft = AccessTools.TypeByName("SeneaLUI.Inv.CraftView");
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

            if (SeneaLHudLayoutPlugin.StatValuesBesideLabels != null)
            {
                SeneaLHudLayoutPlugin.StatValuesBesideLabels.SettingChanged += (_, __) =>
                {
                    if (!On)
                    {
                        for (int i = 0; i < _tracked.Count; i++)
                        {
                            Restore(_tracked[i]);
                        }

                        _jobs.Clear();
                    }
                };
            }
        }

        static bool On
        {
            get { return SeneaLHudLayoutPlugin.StatValuesBesideLabels == null || SeneaLHudLayoutPlugin.StatValuesBesideLabels.Value; }
        }

        [HarmonyPriority(Priority.Last)]
        static void AfterLayRow(object __1)
        {
            if (__1 == null)
            {
                return;
            }

            TMP_Text label = Text(__1, "Label");
            TMP_Text value = Text(__1, "Value");
            TMP_Text small = Text(__1, "Small");
            if (On)
            {
                Beside(label, value, small);
            }
            else
            {
                Restore(label);
                Restore(value);
                Restore(small);
            }
        }

        [HarmonyPriority(Priority.Last)]
        static void AfterPlaceStat(object __0)
        {
            if (__0 == null)
            {
                return;
            }

            TMP_Text label = Text(__0, "Label");
            TMP_Text value = Text(__0, "Value");
            TMP_Text small = Text(__0, "Small");
            if (On)
            {
                Queue(label, value, small, null, null);
            }
            else
            {
                Restore(label);
                Restore(value);
                Restore(small);
            }
        }

        [HarmonyPriority(Priority.Last)]
        static void AfterCompare(object __instance)
        {
            if (__instance == null || _cmp == null)
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
                TMP_Text label = Text(row, "Label");
                TMP_Text cur = Text(row, "Cur");
                TMP_Text arrow = Text(row, "Arrow");
                TMP_Text value = Text(row, "Value");
                TMP_Text delta = Text(row, "Delta");
                if (On)
                {
                    Queue(label, cur, arrow, value, delta);
                }
                else
                {
                    Restore(label);
                    Restore(cur);
                    Restore(arrow);
                    Restore(value);
                    Restore(delta);
                }
            }
        }

        static void Capture(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            int id = text.GetInstanceID();
            if (_original.ContainsKey(id))
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            _original[id] = new Snapshot
            {
                AnchorMin = rect.anchorMin,
                AnchorMax = rect.anchorMax,
                Pivot = rect.pivot,
                SizeDelta = rect.sizeDelta,
                AnchoredPosition = rect.anchoredPosition,
                Alignment = text.alignment,
                Wrap = text.textWrappingMode,
                Overflow = text.overflowMode
            };
            _tracked.Add(text);
        }

        static void Restore(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            int id = text.GetInstanceID();
            if (!_original.TryGetValue(id, out Snapshot snap))
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = snap.AnchorMin;
            rect.anchorMax = snap.AnchorMax;
            rect.pivot = snap.Pivot;
            rect.sizeDelta = snap.SizeDelta;
            rect.anchoredPosition = snap.AnchoredPosition;
            text.alignment = snap.Alignment;
            text.textWrappingMode = snap.Wrap;
            text.overflowMode = snap.Overflow;
        }

        static void Beside(TMP_Text label, params TMP_Text[] parts)
        {
            if (label == null)
            {
                return;
            }

            Capture(label);
            for (int i = 0; i < parts.Length; i++)
            {
                Capture(parts[i]);
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
            if (!On)
            {
                return;
            }

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

            Capture(label);
            Capture(a);
            Capture(b);
            Capture(c);
            Capture(d);

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
