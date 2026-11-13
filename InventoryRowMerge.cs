using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using SeneaLUI.Inv;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// [Inventory] MergeSlotRows - FOOD | ACTION | QUIVER ARROWS on ONE row under the last
    /// inventory grid row (FooterTop). Display only.
    ///
    /// SeneaL's _compact flag is NOT "Hide equipment". It is Equipment ON + no quiver row,
    /// which moves food onto the last grid row and right by _eqX. The merge must NOT follow
    /// that: pin fork/grab/arrow and ActionTop/QuiverTop to FooterTop at X=0.
    /// Do not patch QuiverRow (_quiverShown). Only subtract QuiverExtra from PanelH while
    /// a quiver is actually equipped. Do not grow W: 9 cells use the existing panel width
    /// (Relayout still widens _gridHost from the hardcoded 560 to stock W). When compact
    /// hides _quickTitle, PlaceFootLabels postfix turns Food Slots back on.
    /// </summary>
    static class InventoryRowMerge
    {
        public static ConfigEntry<bool> MergeSlotRows;

        const float Cell = 70f;
        const float Face = 64f;
        const float TitlePad = 22f;
        const float TrashPad = 86f;
        const int ArrowCols = 3;

        static FieldInfo _actTitle, _quiverTitle, _quickTitle, _gridHost, _qGrid, _foodRow, _qKeyRoom, _labelsOn, _trash, _slotRoot;
        static MethodInfo _footerTop, _bodyTop, _h, _place, _quiverPlace, _placeLabels;
        static MethodInfo _actionTop, _quiverTop, _width, _footEnd, _rowStep, _panelH, _quiverExtra;
        static MethodInfo _quiverRow, _customRow, _dockTop;
        static PropertyInfo _quickCount, _actionCount, _equipCfg, _trashCfg;
        static bool _loggedOn;

        public static void Apply(Harmony harmony)
        {
            Type view = AccessTools.TypeByName("SeneaLUI.Inv.InventoryView");
            Type slots = AccessTools.TypeByName("SeneaLUI.Inv.Slots");
            Type gridSkin = AccessTools.TypeByName("SeneaLUI.Inv.GridSkin");
            Type cellv = gridSkin != null ? gridSkin.GetNestedType("Cellv") : null;
            if (view == null || slots == null || cellv == null)
            {
                return;
            }

            _actTitle = AccessTools.Field(view, "_actTitle");
            _quiverTitle = AccessTools.Field(view, "_quiverTitle");
            _quickTitle = AccessTools.Field(view, "_quickTitle");
            _gridHost = AccessTools.Field(view, "_gridHost");
            _qGrid = AccessTools.Field(view, "_qGrid");
            _foodRow = AccessTools.Field(view, "_foodRow");
            _qKeyRoom = AccessTools.Field(view, "_qKeyRoom");
            _labelsOn = AccessTools.Field(view, "_labels");
            _trash = AccessTools.Field(view, "_trash");
            _slotRoot = _trash != null ? AccessTools.Field(_trash.FieldType, "Root") : null;
            _footerTop = AccessTools.PropertyGetter(view, "FooterTop");
            _bodyTop = AccessTools.PropertyGetter(view, "BodyTop");
            _h = AccessTools.PropertyGetter(view, "H");
            _dockTop = AccessTools.PropertyGetter(view, "DockTop");
            _actionTop = AccessTools.PropertyGetter(view, "ActionTop");
            _quiverTop = AccessTools.PropertyGetter(view, "QuiverTop");
            _width = AccessTools.PropertyGetter(view, "W");
            _footEnd = AccessTools.PropertyGetter(view, "FootEnd");
            _rowStep = AccessTools.PropertyGetter(view, "RowStep");
            _panelH = AccessTools.PropertyGetter(view, "PanelH");
            _quiverExtra = AccessTools.PropertyGetter(view, "QuiverExtra");
            _quiverRow = AccessTools.PropertyGetter(view, "QuiverRow");
            _customRow = AccessTools.PropertyGetter(view, "CustomRow");
            _place = AccessTools.Method(view, "Place", new[] { cellv });
            _quiverPlace = AccessTools.Method(view, "QuiverPlace", new[] { cellv });
            _placeLabels = AccessTools.Method(view, "PlaceFootLabels");
            MethodInfo placeTrash = AccessTools.Method(view, "PlaceTrash");
            _quickCount = AccessTools.Property(slots, "QuickCount");
            _actionCount = AccessTools.Property(slots, "ActionCount");
            Type plugin = AccessTools.TypeByName("SeneaLUI.Plugin");
            _equipCfg = plugin != null ? AccessTools.Property(plugin, "Equipment") : null;
            _trashCfg = plugin != null ? AccessTools.Property(plugin, "Trash") : null;

            if (_actTitle == null || _quiverTitle == null || _gridHost == null ||
                _footerTop == null || _bodyTop == null || _actionTop == null ||
                _quiverTop == null || _width == null || _footEnd == null ||
                _rowStep == null || _panelH == null || _quiverExtra == null ||
                _quiverRow == null || _place == null || _quiverPlace == null ||
                _placeLabels == null || _quickTitle == null ||
                _dockTop == null || _h == null || _quickCount == null ||
                _actionCount == null)
            {
                Debug.Log(
                    "[SeneaL HUD Layout] MergeSlotRows: SeneaL's inventory view has changed - " +
                    "the merge stays off, everything else keeps running.");
                return;
            }

            PatchOne(harmony, _place, nameof(AfterPlace), "InventoryView::Place");
            PatchOne(harmony, _quiverPlace, nameof(AfterQuiverPlace), "InventoryView::QuiverPlace");
            PatchOne(harmony, _actionTop, nameof(AfterActionTop), "InventoryView::ActionTop");
            PatchOne(harmony, _quiverTop, nameof(AfterQuiverTop), "InventoryView::QuiverTop");
            PatchOne(harmony, _footEnd, nameof(AfterFootEnd), "InventoryView::FootEnd");
            PatchOne(harmony, _panelH, nameof(AfterPanelH), "InventoryView::PanelH");
            PatchOne(harmony, _dockTop, nameof(AfterDockTop), "InventoryView::DockTop");
            PatchOne(harmony, _placeLabels, nameof(AfterLabels), "InventoryView::PlaceFootLabels");
            if (placeTrash != null && _trash != null && _slotRoot != null)
            {
                PatchOne(harmony, placeTrash, nameof(AfterPlaceTrash), "InventoryView::PlaceTrash");
            }
        }

        static void PatchOne(Harmony harmony, MethodInfo target, string postfix, string label)
        {
            try
            {
                harmony.Patch(target,
                    postfix: new HarmonyMethod(typeof(InventoryRowMerge), postfix));
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[SeneaL HUD Layout] MergeSlotRows: could not patch " + label +
                    " - that piece stays stock SeneaL, everything else in the mod keeps " +
                    "running. " + e.Message);
            }
        }

        static bool Merged()
        {
            if (MergeSlotRows == null || !MergeSlotRows.Value)
            {
                return false;
            }

            if (!_loggedOn)
            {
                _loggedOn = true;
                Debug.Log(
                    "[SeneaL HUD Layout] MergeSlotRows ON v1.5.64: live player-grid arrows " +
                    "shift after ACTION; Dead ghosts stay parked.");
            }

            return true;
        }

        static int QuickCount()
        {
            try
            {
                return _quickCount != null ? (int)_quickCount.GetValue(null, null) : 3;
            }
            catch
            {
                return 3;
            }
        }

        static int ActionCount()
        {
            try
            {
                return _actionCount != null ? (int)_actionCount.GetValue(null, null) : 3;
            }
            catch
            {
                return 3;
            }
        }

        static bool QuiverLive()
        {
            try
            {
                return _quiverRow != null && (bool)_quiverRow.Invoke(null, null);
            }
            catch
            {
                return false;
            }
        }

        static bool FoodRowOn()
        {
            try
            {
                return _foodRow != null && (bool)_foodRow.GetValue(null);
            }
            catch
            {
                return true;
            }
        }

        static float F(MethodInfo getter)
        {
            try
            {
                return getter == null ? 0f : (float)getter.Invoke(null, null);
            }
            catch
            {
                return 0f;
            }
        }

        static float RowY()
        {
            return F(_footerTop) - F(_bodyTop);
        }

        static int RowCols()
        {
            return QuickCount() + ActionCount() + (QuiverLive() ? ArrowCols : 0);
        }

        static bool TrashFitsOnRow()
        {
            float trashX = F(_width) - TrashPad;
            float last = (RowCols() - 1) * Cell;
            return last + Face <= trashX + 1f;
        }

        /// <summary>
        /// Stock 70px when 9 cells plus trash fit. Hide equipment is too narrow for that, so
        /// pitch is (W-22)/cols but never under 64 (the cell face) — trash then sits below.
        /// </summary>
        static float Pitch()
        {
            int cols = Mathf.Max(RowCols(), 1);
            if (TrashFitsOnRow())
            {
                return Cell;
            }

            float p = (F(_width) - TitlePad) / cols;
            if (p > Cell)
            {
                return Cell;
            }

            if (p < Face)
            {
                return Face;
            }

            return p;
        }

        static void AfterPlace(GridSkin.Cellv __0, ref Placement __result)
        {
            try
            {
                if (!Merged() || __0 == null)
                {
                    return;
                }

                Placement pl = __result;
                if (pl.Dead)
                {
                    return;
                }

                float rowY = RowY();
                float pitch = Pitch();
                int qn = QuickCount();
                int an = ActionCount();

                if (pl.Ghost == "fork")
                {
                    pl.X = __0.X * pitch;
                    pl.Y = rowY;
                    __result = pl;
                    return;
                }

                if (pl.Ghost == "grab")
                {
                    pl.X = (__0.X + qn) * pitch;
                    pl.Y = rowY;
                    __result = pl;
                    return;
                }

                // Live quiver ammo is on the PLAYER grid (last row, X<3), not BBH _qGrid.
                // Stock X is 0,1,2 — same as food. Shift after FOOD+ACTION so the three
                // groups sit on one row. Dead ghosts stay parked (do not un-Dead).
                if (pl.Ghost == "arrow")
                {
                    pl.X = (__0.X + qn + an) * pitch;
                    pl.Y = rowY;
                    __result = pl;
                }
            }
            catch
            {
            }
        }

        static void AfterQuiverPlace(GridSkin.Cellv __0, ref Placement __result)
        {
            try
            {
                if (!Merged())
                {
                    return;
                }

                Placement pl = __result;
                if (pl.Dead)
                {
                    return;
                }

                float pitch = Pitch();
                int col = __0 != null ? __0.X : Mathf.RoundToInt(pl.X / Cell);
                pl.X = (QuickCount() + ActionCount() + col) * pitch;
                pl.Y = RowY();
                __result = pl;
            }
            catch
            {
            }
        }

        static void AfterActionTop(ref float __result)
        {
            if (Merged())
            {
                __result = F(_footerTop);
            }
        }

        static void AfterQuiverTop(ref float __result)
        {
            if (Merged())
            {
                __result = F(_footerTop);
            }
        }

        static void AfterFootEnd(ref float __result)
        {
            if (Merged() && FoodRowOn())
            {
                __result -= F(_rowStep);
            }
        }

        static void AfterDockTop(ref float __result)
        {
            if (!Merged())
            {
                return;
            }

            __result = F(_h) - 12f;
        }

        static bool CfgOn(PropertyInfo prop, bool fallback)
        {
            try
            {
                if (prop == null)
                {
                    return fallback;
                }

                ConfigEntry<bool> e = prop.GetValue(null, null) as ConfigEntry<bool>;
                return e == null ? fallback : e.Value;
            }
            catch
            {
                return fallback;
            }
        }

        static void AfterPlaceTrash(object __instance)
        {
            try
            {
                if (!Merged() || __instance == null)
                {
                    return;
                }

                object slot = _trash.GetValue(__instance);
                RectTransform root = slot != null ? _slotRoot.GetValue(slot) as RectTransform : null;
                if (root == null)
                {
                    return;
                }

                bool visible = CfgOn(_trashCfg, true) && CfgOn(_equipCfg, true);
                if (root.gameObject.activeSelf != visible)
                {
                    root.gameObject.SetActive(visible);
                }
            }
            catch
            {
            }
        }

        static void AfterPanelH(ref float __result)
        {
            if (!Merged() || !QuiverLive())
            {
                return;
            }

            try
            {
                if (_customRow != null && (bool)_customRow.Invoke(null, null))
                {
                    return;
                }

                __result -= F(_quiverExtra);
                if (_qKeyRoom != null)
                {
                    __result -= (float)_qKeyRoom.GetValue(null);
                }
            }
            catch
            {
            }
        }

        static void AfterLabels(object __instance)
        {
            try
            {
                if (!Merged() || __instance == null)
                {
                    return;
                }

                bool labelsOn = _labelsOn == null || (bool)_labelsOn.GetValue(null);
                TMPro.TextMeshProUGUI food =
                    _quickTitle.GetValue(__instance) as TMPro.TextMeshProUGUI;
                if (food != null && labelsOn && QuickCount() > 0)
                {
                    food.gameObject.SetActive(true);
                }

                float pitch = Pitch();
                SlideTitle(_actTitle.GetValue(__instance) as TMPro.TextMeshProUGUI,
                    TitlePad + QuickCount() * pitch);
                SlideTitle(_quiverTitle.GetValue(__instance) as TMPro.TextMeshProUGUI,
                    TitlePad + (QuickCount() + ActionCount()) * pitch);
            }
            catch
            {
            }
        }

        static void SlideTitle(TMPro.TextMeshProUGUI title, float x)
        {
            if (title == null)
            {
                return;
            }

            title.alignment = TMPro.TextAlignmentOptions.TopLeft;
            title.overflowMode = TMPro.TextOverflowModes.Overflow;
            RectTransform rt = title.rectTransform;
            title.ForceMeshUpdate();
            float w = Mathf.Max(8f, Mathf.Ceil(title.preferredWidth) + 4f);
            Vector2 s = rt.sizeDelta;
            if (!Mathf.Approximately(s.x, w))
            {
                rt.sizeDelta = new Vector2(w, s.y);
            }

            Vector2 p = rt.anchoredPosition;
            if (!Mathf.Approximately(p.x, x))
            {
                rt.anchoredPosition = new Vector2(x, p.y);
            }
        }
    }
}
