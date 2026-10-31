using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    public enum ItemTipLeftOverflow
    {
        Off,
        Set,
        SetThenMagic
    }

    static class TipColumnOverflow
    {
        enum SectionKind
        {
            Misc,
            Combat,
            Stats,
            Magic,
            Set
        }

        sealed class Section
        {
            public SectionKind Kind;
            public readonly List<object> Rows = new List<object>();

            public float Height(FieldInfo muted, FieldInfo value, FieldInfo small, FieldInfo label, FieldInfo head)
            {
                float h = 0f;
                for (int i = 0; i < Rows.Count; i++)
                {
                    h += RowHeight(Rows[i], muted, value, small, label, head);
                }

                return h;
            }
        }

        sealed class Snapshot
        {
            public int Version;
            public object[] Order;
            public int Split;
        }

        static FieldInfo _split;
        static FieldInfo _rows;
        static FieldInfo _label;
        static FieldInfo _value;
        static FieldInfo _small;
        static FieldInfo _head;
        static FieldInfo _muted;
        static FieldInfo _version;
        static MethodInfo _lang;
        static MethodInfo _locStats;
        static MethodInfo _locDamage;
        static MethodInfo _locDefense;
        static MethodInfo _locEffects;
        static MethodInfo _locSetTitle;
        static MethodInfo _locSetBonus;

        static string _langKey = "";
        static string _stats;
        static string _damage;
        static string _defense;
        static string _effects;
        static string _setTitle;
        static string _setBonus;

        static readonly Dictionary<object, Snapshot> _snapshots = new Dictionary<object, Snapshot>();
        static readonly List<Section> _sections = new List<Section>(8);
        static readonly List<object> _rebuild = new List<object>(64);

        static object _doneTip;
        static int _doneVer;
        static int _doneSplit;
        static int _doneCount;
        static ItemTipLeftOverflow _doneMode;

        public static void Apply(Harmony harmony)
        {
            Type tooltip = AccessTools.TypeByName("SeneaLUI.Components.Tooltip");
            Type tip = AccessTools.TypeByName("SeneaLUI.Components.Tip");
            Type row = AccessTools.TypeByName("SeneaLUI.Components.Tip+Row")
                ?? AccessTools.Inner(tip, "Row");
            Type loc = AccessTools.TypeByName("SeneaLUI.Core.Loc");
            Type tips = AccessTools.TypeByName("SeneaLUI.Inv.Tips");
            if (tooltip == null || tip == null || row == null)
            {
                return;
            }

            _split = AccessTools.Field(tip, "Split");
            _rows = AccessTools.Field(tip, "Rows");
            _label = AccessTools.Field(row, "Label");
            _value = AccessTools.Field(row, "Value");
            _small = AccessTools.Field(row, "Small");
            _head = AccessTools.Field(row, "Head");
            _muted = AccessTools.Field(row, "Muted");
            _version = AccessTools.Field(tip, "Version");
            _lang = tips != null ? AccessTools.PropertyGetter(tips, "Lang") : null;
            if (loc != null)
            {
                _locStats = Getter(loc, "Stats");
                _locDamage = Getter(loc, "Damage");
                _locDefense = Getter(loc, "Defense");
                _locEffects = Getter(loc, "TipEffects");
                _locSetTitle = Getter(loc, "SetTitle");
                _locSetBonus = Getter(loc, "TipSetBonus");
            }

            MethodInfo build = AccessTools.Method(tooltip, "Build");
            MethodInfo addSet = tips != null ? AccessTools.Method(tips, "AddSet") : null;
            if (_split == null || _rows == null || _label == null)
            {
                return;
            }

            if (build != null)
            {
                harmony.Patch(build, prefix: new HarmonyMethod(typeof(TipColumnOverflow), nameof(BeforeBuild)));
            }

            if (addSet != null)
            {
                harmony.Patch(addSet, postfix: new HarmonyMethod(typeof(TipColumnOverflow), nameof(AfterAddSet)));
            }

            if (SeneaLHudLayoutPlugin.ItemTipOverflow != null)
            {
                SeneaLHudLayoutPlugin.ItemTipOverflow.SettingChanged += (_, __) =>
                {
                    _doneTip = null;
                    _snapshots.Clear();
                };
            }
        }

        static MethodInfo Getter(Type loc, string name)
        {
            return AccessTools.PropertyGetter(loc, name) ?? AccessTools.Method(loc, "get_" + name);
        }

        [HarmonyPriority(Priority.Low)]
        static void BeforeBuild(object __0)
        {
            ApplyToTip(__0);
        }

        [HarmonyPriority(Priority.Low)]
        static void AfterAddSet(object __0)
        {
            ApplyToTip(__0);
        }

        static void ApplyToTip(object tip)
        {
            if (tip == null)
            {
                return;
            }

            ItemTipLeftOverflow mode = Mode;
            IList rows = _rows.GetValue(tip) as IList;
            if (rows == null || rows.Count < 3)
            {
                return;
            }

            EnsureSnapshot(tip, rows);

            int split = (int)_split.GetValue(tip);
            int count = rows.Count;
            if (Already(tip, split, count, mode))
            {
                return;
            }

            RefreshLoc();
            RestoreSnapshot(tip, rows);

            if (!HasCombatSection(rows))
            {
                Remember(tip, (int)_split.GetValue(tip), rows.Count, mode);
                return;
            }

            if (mode == ItemTipLeftOverflow.Off)
            {
                Remember(tip, (int)_split.GetValue(tip), rows.Count, mode);
                return;
            }

            ParseSections(rows);
            if (_sections.Count < 2 || _sections[0].Kind != SectionKind.Combat)
            {
                Remember(tip, (int)_split.GetValue(tip), rows.Count, mode);
                return;
            }

            if (!TryPickLayout(mode, out bool statsLeft, out bool magicLeft, out bool setLeft, out bool miscLeft))
            {
                Remember(tip, (int)_split.GetValue(tip), rows.Count, mode);
                return;
            }

            ApplyLayout(rows, tip, statsLeft, magicLeft, setLeft, miscLeft);
            Remember(tip, (int)_split.GetValue(tip), rows.Count, mode);
        }

        static ItemTipLeftOverflow Mode
        {
            get
            {
                if (SeneaLHudLayoutPlugin.ItemTipOverflow == null)
                {
                    return ItemTipLeftOverflow.SetThenMagic;
                }

                return SeneaLHudLayoutPlugin.ItemTipOverflow.Value;
            }
        }

        static void EnsureSnapshot(object tip, IList rows)
        {
            int ver = Ver(tip);
            Snapshot snap;
            if (_snapshots.TryGetValue(tip, out snap) && snap.Version == ver)
            {
                return;
            }

            object[] order = new object[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                order[i] = rows[i];
            }

            _snapshots[tip] = new Snapshot
            {
                Version = ver,
                Order = order,
                Split = (int)_split.GetValue(tip)
            };
        }

        static void RestoreSnapshot(object tip, IList rows)
        {
            Snapshot snap;
            if (!_snapshots.TryGetValue(tip, out snap))
            {
                return;
            }

            rows.Clear();
            for (int i = 0; i < snap.Order.Length; i++)
            {
                rows.Add(snap.Order[i]);
            }

            _split.SetValue(tip, snap.Split);
        }

        static void ApplyLayout(IList rows, object tip, bool statsLeft, bool magicLeft, bool setLeft, bool miscLeft)
        {
            _rebuild.Clear();
            AppendSections(_rebuild, statsLeft, magicLeft, setLeft, miscLeft, true);
            int leftCount = _rebuild.Count;
            AppendSections(_rebuild, statsLeft, magicLeft, setLeft, miscLeft, false);

            rows.Clear();
            for (int i = 0; i < _rebuild.Count; i++)
            {
                rows.Add(_rebuild[i]);
            }

            _split.SetValue(tip, leftCount);
        }

        static void AppendSections(List<object> target, bool statsLeft, bool magicLeft, bool setLeft, bool miscLeft, bool left)
        {
            for (int i = 0; i < _sections.Count; i++)
            {
                Section sec = _sections[i];
                if (!OnSide(sec.Kind, statsLeft, magicLeft, setLeft, miscLeft, left))
                {
                    continue;
                }

                target.AddRange(sec.Rows);
            }
        }

        static bool OnSide(SectionKind kind, bool statsLeft, bool magicLeft, bool setLeft, bool miscLeft, bool leftColumn)
        {
            bool onLeft;
            switch (kind)
            {
                case SectionKind.Combat:
                    onLeft = true;
                    break;
                case SectionKind.Stats:
                    onLeft = statsLeft;
                    break;
                case SectionKind.Magic:
                    onLeft = magicLeft;
                    break;
                case SectionKind.Set:
                    onLeft = setLeft;
                    break;
                default:
                    onLeft = miscLeft;
                    break;
            }

            return onLeft == leftColumn;
        }

        static bool TryPickLayout(
            ItemTipLeftOverflow mode,
            out bool statsLeft,
            out bool magicLeft,
            out bool setLeft,
            out bool miscLeft)
        {
            statsLeft = false;
            magicLeft = false;
            setLeft = false;
            miscLeft = false;

            bool hasStats = false;
            bool hasMagic = false;
            bool hasSet = false;
            bool hasMisc = false;
            for (int i = 0; i < _sections.Count; i++)
            {
                switch (_sections[i].Kind)
                {
                    case SectionKind.Stats: hasStats = true; break;
                    case SectionKind.Magic: hasMagic = true; break;
                    case SectionKind.Set: hasSet = true; break;
                    case SectionKind.Misc: hasMisc = true; break;
                }
            }

            float best = float.MaxValue;
            bool found = false;
            bool bs = false, bm = false, bset = false, bmisc = false;

            for (int mask = 0; mask < 16; mask++)
            {
                bool s = hasStats && (mask & 1) != 0;
                bool m = hasMagic && (mask & 2) != 0;
                bool set = hasSet && (mask & 4) != 0;
                bool misc = hasMisc && (mask & 8) != 0;

                if (!Allowed(mode, m, set))
                {
                    continue;
                }

                float leftH = 0f;
                float rightH = 0f;
                for (int i = 0; i < _sections.Count; i++)
                {
                    Section sec = _sections[i];
                    float h = sec.Height(_muted, _value, _small, _label, _head);
                    if (OnSide(sec.Kind, s, m, set, misc, true))
                    {
                        leftH += h;
                    }
                    else
                    {
                        rightH += h;
                    }
                }

                if (rightH < 0.5f)
                {
                    continue;
                }

                float score = Math.Abs(leftH - rightH);
                if (score < best)
                {
                    best = score;
                    bs = s;
                    bm = m;
                    bset = set;
                    bmisc = misc;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            statsLeft = bs;
            magicLeft = bm;
            setLeft = bset;
            miscLeft = bmisc;
            return true;
        }

        static bool Allowed(ItemTipLeftOverflow mode, bool magicLeft, bool setLeft)
        {
            if (mode == ItemTipLeftOverflow.Off)
            {
                return false;
            }

            if (mode == ItemTipLeftOverflow.Set)
            {
                return !magicLeft;
            }

            return true;
        }

        static void ParseSections(IList rows)
        {
            _sections.Clear();
            Section current = null;

            for (int i = 0; i < rows.Count; i++)
            {
                object row = rows[i];
                if (IsHead(row))
                {
                    SectionKind kind = ClassifyHead(Label(row));
                    if (current != null && current.Kind == SectionKind.Set && kind == SectionKind.Set)
                    {
                        current.Rows.Add(row);
                        continue;
                    }

                    current = new Section { Kind = kind };
                    _sections.Add(current);
                }
                else if (current == null)
                {
                    current = new Section { Kind = SectionKind.Misc };
                    _sections.Add(current);
                }

                current.Rows.Add(row);
            }
        }

        static SectionKind ClassifyHead(string label)
        {
            if (Match(label, _defense) || Match(label, _damage))
            {
                return SectionKind.Combat;
            }

            if (Match(label, _stats))
            {
                return SectionKind.Stats;
            }

            if (Match(label, _effects))
            {
                return SectionKind.Magic;
            }

            if (IsSetStart(label) || Match(label, _setBonus))
            {
                return SectionKind.Set;
            }

            return SectionKind.Misc;
        }

        static bool HasCombatSection(IList rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (!IsHead(rows[i]))
                {
                    continue;
                }

                string label = Label(rows[i]);
                if (Match(label, _defense) || Match(label, _damage))
                {
                    return true;
                }
            }

            return false;
        }

        static float RowHeight(object row, FieldInfo muted, FieldInfo value, FieldInfo small, FieldInfo labelField, FieldInfo head)
        {
            string label = labelField != null ? labelField.GetValue(row) as string : null;
            string valueText = value != null ? value.GetValue(row) as string : null;
            string smallText = small != null ? small.GetValue(row) as string : null;
            int chars = (label != null ? label.Length : 0)
                + (valueText != null ? valueText.Length : 0)
                + (smallText != null ? smallText.Length : 0);

            float h = 1f;
            if (head != null && head.GetValue(row) is bool on && on)
            {
                h += 0.35f;
            }

            if (muted != null && muted.GetValue(row) is bool mutedOn && mutedOn)
            {
                h += 0.15f;
            }

            h += chars / 34f;
            return h;
        }

        static bool Already(object tip, int split, int count, ItemTipLeftOverflow mode)
        {
            return ReferenceEquals(tip, _doneTip)
                && Ver(tip) == _doneVer
                && split == _doneSplit
                && count == _doneCount
                && mode == _doneMode;
        }

        static void Remember(object tip, int split, int count, ItemTipLeftOverflow mode)
        {
            _doneTip = tip;
            _doneVer = Ver(tip);
            _doneSplit = split;
            _doneCount = count;
            _doneMode = mode;
        }

        static int Ver(object tip)
        {
            return _version == null ? 0 : (int)_version.GetValue(tip);
        }

        static bool IsHead(object row)
        {
            return _head != null && row != null && _head.GetValue(row) is bool on && on;
        }

        static string Label(object row)
        {
            return row == null ? null : _label.GetValue(row) as string;
        }

        static bool IsSetStart(string label)
        {
            if (Match(label, _setTitle))
            {
                return true;
            }

            return !string.IsNullOrEmpty(label)
                && label.StartsWith("Set", StringComparison.OrdinalIgnoreCase);
        }

        static void RefreshLoc()
        {
            string lang = "";
            if (_lang != null)
            {
                lang = _lang.Invoke(null, null) as string ?? "";
            }

            if (lang == _langKey && _stats != null)
            {
                return;
            }

            _langKey = lang;
            _stats = OrDefault(ReadLoc(_locStats), "Stats");
            _damage = OrDefault(ReadLoc(_locDamage), "Damage");
            _defense = OrDefault(ReadLoc(_locDefense), "Defense");
            _effects = OrDefault(ReadLoc(_locEffects), "Magic Effects");
            _setTitle = OrDefault(ReadLoc(_locSetTitle), "Set");
            _setBonus = OrDefault(ReadLoc(_locSetBonus), "Set bonus");
        }

        static string OrDefault(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        static string ReadLoc(MethodInfo get)
        {
            return get == null ? null : get.Invoke(null, null) as string;
        }

        static bool Match(string label, string expect)
        {
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(expect))
            {
                return false;
            }

            int a0 = 0;
            int a1 = label.Length;
            int b0 = 0;
            int b1 = expect.Length;
            while (a1 > a0 && (label[a1 - 1] == ':' || char.IsWhiteSpace(label[a1 - 1])))
            {
                a1--;
            }

            while (a0 < a1 && char.IsWhiteSpace(label[a0]))
            {
                a0++;
            }

            while (b1 > b0 && (expect[b1 - 1] == ':' || char.IsWhiteSpace(expect[b1 - 1])))
            {
                b1--;
            }

            while (b0 < b1 && char.IsWhiteSpace(expect[b0]))
            {
                b0++;
            }

            int len = a1 - a0;
            if (len <= 0 || len != b1 - b0)
            {
                return false;
            }

            return string.Compare(label, a0, expect, b0, len, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
