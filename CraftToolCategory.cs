using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Pickaxes: Tools only (vanilla ItemType is TwoHandedWeapon).
    /// 1H axes: stay Weapons, and also appear under Tools (they are both).
    /// Battleaxes stay Weapons only.
    /// </summary>
    static class CraftToolCategory
    {
        const int CatWeapons = 0;
        const int CatTools = 2;
        const int ToolsChip = 3;

        static readonly List<(object Row, int Cat, int Kind)> Moved = new List<(object, int, int)>(16);

        static Type _viewType;
        static Type _rowType;
        static FieldInfo _rows;
        static FieldInfo _cat;
        static FieldInfo _catShow;
        static FieldInfo _catsOn;
        static FieldInfo _rowCat;
        static FieldInfo _rowKind;
        static FieldInfo _rowItem;
        static FieldInfo _rowRecipe;
        static MethodInfo _layout;

        public static void Apply(Harmony harmony)
        {
            Type slots = AccessTools.TypeByName("SeneaLUI.Inv.Slots");
            _viewType = AccessTools.TypeByName("SeneaLUI.Inv.CraftView");
            MethodInfo category = slots != null
                ? AccessTools.Method(slots, "Category", new[] { typeof(ItemDrop.ItemData) })
                : null;
            if (category != null)
            {
                harmony.Patch(category, postfix: new HarmonyMethod(typeof(CraftToolCategory), nameof(AfterCategory)));
            }

            if (_viewType == null)
            {
                return;
            }

            _rowType = AccessTools.Inner(_viewType, "Row");
            _rows = AccessTools.Field(_viewType, "_rows");
            _cat = AccessTools.Field(_viewType, "_cat");
            _catShow = AccessTools.Field(_viewType, "_catShow");
            _catsOn = AccessTools.Field(_viewType, "_catsOn");
            _layout = AccessTools.Method(_viewType, "LayoutFrame");
            if (_rowType != null)
            {
                _rowCat = AccessTools.Field(_rowType, "Cat");
                _rowKind = AccessTools.Field(_rowType, "Kind");
                _rowItem = AccessTools.Field(_rowType, "Item");
                _rowRecipe = AccessTools.Field(_rowType, "Recipe");
            }

            MethodInfo rebuild = AccessTools.Method(_viewType, "Rebuild");
            if (rebuild != null)
            {
                harmony.Patch(rebuild, postfix: new HarmonyMethod(typeof(CraftToolCategory), nameof(AfterRebuild)));
            }

            MethodInfo filter = AccessTools.Method(_viewType, "Filter");
            if (filter != null)
            {
                harmony.Patch(filter,
                    prefix: new HarmonyMethod(typeof(CraftToolCategory), nameof(BeforeFilter)),
                    postfix: new HarmonyMethod(typeof(CraftToolCategory), nameof(AfterFilter)));
            }
        }

        static void AfterCategory(ItemDrop.ItemData item, ref int __result)
        {
            if (item?.m_shared == null || __result == CatTools)
            {
                return;
            }

            if (item.m_shared.m_skillType == Skills.SkillType.Pickaxes)
            {
                __result = CatTools;
            }
        }

        static void AfterRebuild(object __instance)
        {
            IList rows = _rows != null ? _rows.GetValue(__instance) as IList : null;
            if (rows == null || !HasOneHandAxe(rows))
            {
                return;
            }

            bool[] show = _catShow != null ? _catShow.GetValue(__instance) as bool[] : null;
            if (show == null || show.Length <= ToolsChip)
            {
                return;
            }

            bool needLayout = false;
            if (!show[ToolsChip])
            {
                show[ToolsChip] = true;
                needLayout = true;
            }

            if (_catsOn != null && _catsOn.GetValue(__instance) is bool on && !on)
            {
                _catsOn.SetValue(__instance, true);
                needLayout = true;
            }

            if (needLayout)
            {
                _layout?.Invoke(__instance, null);
            }
        }

        static void BeforeFilter(object __instance)
        {
            Moved.Clear();
            if (_cat == null || !(_cat.GetValue(__instance) is int cat) || cat != CatTools)
            {
                return;
            }

            IList rows = _rows != null ? _rows.GetValue(__instance) as IList : null;
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                object row = rows[i];
                if (!IsOneHandAxe(row) || !(_rowCat.GetValue(row) is int rowCat) || rowCat != CatWeapons)
                {
                    continue;
                }

                int kind = _rowKind != null && _rowKind.GetValue(row) is int k ? k : -1;
                Moved.Add((row, rowCat, kind));
                _rowCat.SetValue(row, CatTools);
                if (_rowKind != null)
                {
                    _rowKind.SetValue(row, -1);
                }
            }
        }

        static void AfterFilter()
        {
            for (int i = 0; i < Moved.Count; i++)
            {
                (object row, int cat, int kind) = Moved[i];
                _rowCat.SetValue(row, cat);
                if (_rowKind != null)
                {
                    _rowKind.SetValue(row, kind);
                }
            }

            Moved.Clear();
        }

        static bool HasOneHandAxe(IList rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (IsOneHandAxe(rows[i]))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsOneHandAxe(object row)
        {
            if (row == null)
            {
                return false;
            }

            ItemDrop.ItemData item = _rowItem != null ? _rowItem.GetValue(row) as ItemDrop.ItemData : null;
            if (item?.m_shared == null)
            {
                Recipe recipe = _rowRecipe != null ? _rowRecipe.GetValue(row) as Recipe : null;
                item = recipe != null && recipe.m_item != null ? recipe.m_item.m_itemData : null;
            }

            if (item?.m_shared == null)
            {
                return false;
            }

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon
                && item.m_shared.m_skillType == Skills.SkillType.Axes;
        }
    }
}
