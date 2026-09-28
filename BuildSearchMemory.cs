using System;
using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    static class BuildSearchMemory
    {
        static FieldInfo _search;
        static FieldInfo _term;
        static FieldInfo _shown;
        static FieldInfo _pieceSig;
        static PropertyInfo _text;
        static MethodInfo _setText;
        static string _keep;

        public static void Apply(Harmony harmony)
        {
            Type menu = AccessTools.TypeByName("SeneaLUI.Hud.BuildMenuView");
            MethodInfo tick = menu != null ? AccessTools.Method(menu, "Tick") : null;
            if (tick == null)
            {
                return;
            }

            _search = AccessTools.Field(menu, "_search");
            _term = AccessTools.Field(menu, "_term");
            _shown = AccessTools.Field(menu, "_shown");
            _pieceSig = AccessTools.Field(menu, "_pieceSig");
            Type box = AccessTools.TypeByName("SeneaLUI.Components.SearchBox");
            _text = box != null ? AccessTools.Property(box, "Text") : null;
            _setText = box != null ? AccessTools.Method(box, "SetText") : null;
            if (_search == null || _text == null || _setText == null)
            {
                return;
            }

            harmony.Patch(tick,
                prefix: new HarmonyMethod(typeof(BuildSearchMemory), nameof(Before)),
                postfix: new HarmonyMethod(typeof(BuildSearchMemory), nameof(After)));
        }

        static void Before(object __instance)
        {
            string text = Read(__instance);
            if (!string.IsNullOrEmpty(text))
            {
                _keep = text;
            }
            else if (Shown(__instance))
            {
                _keep = null;
            }
        }

        static void After(object __instance)
        {
            if (string.IsNullOrEmpty(_keep) || !Shown(__instance))
            {
                return;
            }

            if (!string.IsNullOrEmpty(Read(__instance)))
            {
                return;
            }

            object box = _search.GetValue(__instance);
            if (box == null)
            {
                return;
            }

            _setText.Invoke(box, new object[] { _keep });
            if (_term != null)
            {
                _term.SetValue(__instance, _keep.Replace(" ", "").ToLowerInvariant());
            }

            if (_pieceSig != null)
            {
                _pieceSig.SetValue(__instance, int.MinValue);
            }
        }

        static string Read(object menu)
        {
            object box = _search.GetValue(menu);
            if (box == null)
            {
                return null;
            }

            return _text.GetValue(box, null) as string;
        }

        static bool Shown(object menu)
        {
            return _shown != null && _shown.GetValue(menu) is bool shown && shown;
        }
    }
}
