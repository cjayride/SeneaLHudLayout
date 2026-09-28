using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    static class CraftMaterials
    {
        static readonly Color Link = new Color(1f, 0.82f, 0.28f, 1f);
        static readonly List<Recipe> History = new List<Recipe>();
        static readonly HashSet<int> Wired = new HashSet<int>();

        static Type _viewType;
        static Type _stateType;
        static FieldInfo _gui;
        static FieldInfo _rows;
        static FieldInfo _chips;
        static FieldInfo _chipNames;
        static FieldInfo _panel;
        static FieldInfo _detail;
        static FieldInfo _detailW;
        static FieldInfo _recipeField;
        static FieldInfo _indexField;
        static FieldInfo _itemField;
        static FieldInfo _catField;
        static MethodInfo _onCat;
        static MethodInfo _selected;
        static MethodInfo _recipeOf;
        static object _view;
        static Button _back;
        static TextMeshProUGUI _backLabel;

        public static void Apply(Harmony harmony)
        {
            _viewType = AccessTools.TypeByName("SeneaLUI.Inv.CraftView");
            _stateType = AccessTools.TypeByName("SeneaLUI.Inv.CraftState");
            if (_viewType == null || _stateType == null)
            {
                return;
            }

            _gui = AccessTools.Field(_viewType, "_gui");
            _rows = AccessTools.Field(_viewType, "_rows");
            _chips = AccessTools.Field(_viewType, "_chips");
            _chipNames = AccessTools.Field(_viewType, "_chipNames");
            _panel = AccessTools.Field(_viewType, "_panel");
            _detail = AccessTools.Field(_viewType, "_detailView");
            _detailW = AccessTools.Field(_viewType, "_detailW");
            _selected = AccessTools.Method(_stateType, "Selected");
            _recipeOf = AccessTools.Method(_stateType, "RecipeOf");
            _onCat = AccessTools.Method(_viewType, "OnCat");

            MethodInfo tick = AccessTools.Method(_viewType, "TickRequirements");
            if (tick != null)
            {
                harmony.Patch(tick, postfix: new HarmonyMethod(typeof(CraftMaterials), nameof(AfterRequirements)));
            }

            MethodInfo row = AccessTools.Method(_viewType, "OnRow");
            if (row != null)
            {
                harmony.Patch(row, prefix: new HarmonyMethod(typeof(CraftMaterials), nameof(BeforeRow)));
            }
        }

        static bool On
        {
            get { return SeneaLHudLayoutPlugin.ClickMaterials == null || SeneaLHudLayoutPlugin.ClickMaterials.Value; }
        }

        static void BeforeRow()
        {
            History.Clear();
        }

        static void AfterRequirements(object __instance)
        {
            _view = __instance;
            if (!On)
            {
                History.Clear();
                Mark(__instance, false);
                return;
            }

            Mark(__instance, true);
        }

        public static void Tick()
        {
            if (_back == null)
            {
                return;
            }

            bool show = On && History.Count > 0 && _view != null && PanelOpen();
            if (_back.gameObject.activeSelf != show)
            {
                _back.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            RectTransform detail = _detail != null ? _detail.GetValue(_view) as RectTransform : null;
            float width = _detailW != null && _detailW.GetValue(_view) is float value ? value : 220f;
            if (detail == null)
            {
                return;
            }

            RectTransform rect = _back.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(detail.anchoredPosition.x + width - 4f, detail.anchoredPosition.y - 4f);
            rect.SetAsLastSibling();
            UsePanelFont();
        }

        static void Mark(object view, bool link)
        {
            IList chips = _chips != null ? _chips.GetValue(view) as IList : null;
            IList names = _chipNames != null ? _chipNames.GetValue(view) as IList : null;
            if (chips == null || names == null)
            {
                return;
            }

            int count = Mathf.Min(chips.Count, names.Count);
            for (int i = 0; i < count; i++)
            {
                object chip = chips[i];
                if (chip == null)
                {
                    continue;
                }

                RectTransform root = AccessTools.Field(chip.GetType(), "Root")?.GetValue(chip) as RectTransform;
                if (root == null || !root.gameObject.activeSelf)
                {
                    continue;
                }

                string item = names[i] as string;
                bool craftable = link && Find(view, item) >= 0;
                Tint(root, craftable);
                Wire(root, i);
            }
        }

        static void Wire(RectTransform root, int index)
        {
            int id = root.GetInstanceID();
            if (!Wired.Add(id))
            {
                return;
            }

            Transform hit = root.Find("hit");
            GameObject target = hit != null ? hit.gameObject : root.gameObject;
            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image plate = target.AddComponent<Image>();
                plate.color = new Color(0f, 0f, 0f, 0f);
                plate.raycastTarget = true;
            }

            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
            }

            int captured = index;
            button.onClick.AddListener(() => Open(captured));
        }

        static void Open(int index)
        {
            if (!On || _view == null)
            {
                return;
            }

            IList names = _chipNames.GetValue(_view) as IList;
            if (names == null || index < 0 || index >= names.Count)
            {
                return;
            }

            string item = names[index] as string;
            int target = Find(_view, item);
            if (target < 0)
            {
                return;
            }

            InventoryGui gui = Gui(_view);
            Recipe current = Selected(gui);
            if (current != null && FindRecipe(_view, current) == target)
            {
                return;
            }

            if (current != null && (History.Count == 0 || History[History.Count - 1] != current))
            {
                History.Add(current);
                if (History.Count > 12)
                {
                    History.RemoveAt(0);
                }
            }

            ShowRecipe(gui, target);
            EnsureBack();
        }

        static void Back()
        {
            if (_view == null || History.Count == 0)
            {
                return;
            }

            Recipe recipe = History[History.Count - 1];
            History.RemoveAt(History.Count - 1);
            InventoryGui gui = Gui(_view);
            int index = FindRecipe(_view, recipe);
            if (index >= 0)
            {
                ShowRecipe(gui, index);
            }
        }

        static void ShowRecipe(InventoryGui gui, int index)
        {
            if (gui == null || index < 0 || _view == null)
            {
                return;
            }

            object row = RowFor(index);
            ClearSearch();
            int cat = CatOf(row);
            if (_onCat != null)
            {
                _onCat.Invoke(_view, new object[] { cat + 1 });
            }

            Action<InventoryGui, int, bool> group = Delegate<Action<InventoryGui, int, bool>>("SetActiveGroup");
            Action<InventoryGui, int, bool> set = Delegate<Action<InventoryGui, int, bool>>("SetRecipe");
            if (group != null)
            {
                group(gui, 3, true);
            }

            if (set != null)
            {
                set(gui, index, false);
            }
        }

        static T Delegate<T>(string field) where T : class
        {
            return AccessTools.Field(_stateType, field)?.GetValue(null) as T;
        }

        static object RowFor(int available)
        {
            IList rows = _rows != null ? _rows.GetValue(_view) as IList : null;
            if (rows == null)
            {
                return null;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                if (IndexOfRow(rows[i]) == available)
                {
                    return rows[i];
                }
            }

            return null;
        }

        static int CatOf(object row)
        {
            if (row == null)
            {
                return -1;
            }

            if (_catField == null || _catField.DeclaringType != row.GetType())
            {
                _catField = AccessTools.Field(row.GetType(), "Cat");
            }

            object value = _catField != null ? _catField.GetValue(row) : null;
            return value is int cat ? cat : -1;
        }

        static void ClearSearch()
        {
            object search = AccessTools.Field(_viewType, "_search")?.GetValue(_view);
            if (search == null)
            {
                return;
            }

            MethodInfo clear = AccessTools.Method(search.GetType(), "Clear");
            if (clear == null)
            {
                return;
            }

            try
            {
                clear.Invoke(search, null);
            }
            catch (Exception)
            {
            }
        }

        static int Find(object view, string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                return -1;
            }

            IList rows = _rows != null ? _rows.GetValue(view) as IList : null;
            if (rows == null)
            {
                return -1;
            }

            int best = -1;
            for (int i = 0; i < rows.Count; i++)
            {
                object row = rows[i];
                Recipe recipe = RecipeOfRow(row);
                int index = IndexOfRow(row);
                if (recipe == null || index < 0 || !Same(recipe, item))
                {
                    continue;
                }

                best = index;
                if (ItemOfRow(row) == null)
                {
                    return best;
                }
            }

            return best;
        }

        static bool Same(Recipe recipe, string item)
        {
            if (recipe == null || recipe.m_item == null || recipe.m_item.m_itemData == null || recipe.m_item.m_itemData.m_shared == null)
            {
                return false;
            }

            string name = recipe.m_item.m_itemData.m_shared.m_name;
            if (name == item || recipe.m_item.name == item)
            {
                return true;
            }

            if (Localization.instance == null)
            {
                return false;
            }

            string shown = Localization.instance.Localize(name);
            string wanted = Localization.instance.Localize(item);
            return !string.IsNullOrEmpty(shown) && shown == wanted;
        }

        static ItemDrop.ItemData ItemOfRow(object row)
        {
            if (row == null)
            {
                return null;
            }

            if (_itemField == null || _itemField.DeclaringType != row.GetType())
            {
                _itemField = AccessTools.Field(row.GetType(), "Item");
            }

            return _itemField != null ? _itemField.GetValue(row) as ItemDrop.ItemData : null;
        }

        static int FindRecipe(object view, Recipe recipe)
        {
            IList rows = _rows != null ? _rows.GetValue(view) as IList : null;
            if (rows == null || recipe == null)
            {
                return -1;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                object row = rows[i];
                if (RecipeOfRow(row) == recipe && IndexOfRow(row) >= 0)
                {
                    return IndexOfRow(row);
                }
            }

            return -1;
        }

        static Recipe RecipeOfRow(object row)
        {
            if (row == null)
            {
                return null;
            }

            if (_recipeField == null || _recipeField.DeclaringType != row.GetType())
            {
                _recipeField = AccessTools.Field(row.GetType(), "Recipe");
                _indexField = AccessTools.Field(row.GetType(), "Index");
            }

            return _recipeField != null ? _recipeField.GetValue(row) as Recipe : null;
        }

        static int IndexOfRow(object row)
        {
            if (row == null || _indexField == null)
            {
                return -1;
            }

            object value = _indexField.GetValue(row);
            return value is int index ? index : -1;
        }

        static Recipe Selected(InventoryGui gui)
        {
            if (gui == null || _selected == null || _recipeOf == null)
            {
                return null;
            }

            object pair = _selected.Invoke(null, new object[] { gui });
            return pair != null ? _recipeOf.Invoke(null, new object[] { pair }) as Recipe : null;
        }

        static InventoryGui Gui(object view)
        {
            return _gui != null ? _gui.GetValue(view) as InventoryGui : null;
        }

        static bool PanelOpen()
        {
            RectTransform panel = _panel != null ? _panel.GetValue(_view) as RectTransform : null;
            return panel != null && panel.gameObject.activeInHierarchy;
        }

        static void Tint(RectTransform root, bool craftable)
        {
            TintChild(root, "name", craftable);
            TintChild(root, "count", craftable);
        }

        static void TintChild(RectTransform root, string child, bool craftable)
        {
            Transform found = root.Find(child);
            TMP_Text text = found != null ? found.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                return;
            }

            FontStyles style = craftable ? FontStyles.Underline : FontStyles.Normal;
            if (text.fontStyle != style)
            {
                text.fontStyle = style;
            }

            if (craftable && text.color != Link)
            {
                text.color = Link;
            }
        }

        static void EnsureBack()
        {
            if (_back != null || _view == null || _panel == null)
            {
                return;
            }

            RectTransform panel = _panel.GetValue(_view) as RectTransform;
            if (panel == null)
            {
                return;
            }

            GameObject host = new GameObject("SeneaLHudLayout_CraftBack", typeof(RectTransform), typeof(Image), typeof(Button));
            host.transform.SetParent(panel, false);
            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(76f, 24f);

            Image image = host.GetComponent<Image>();
            image.color = new Color(0.16f, 0.13f, 0.08f, 0.94f);

            GameObject labelHost = new GameObject("label", typeof(RectTransform));
            labelHost.transform.SetParent(host.transform, false);
            RectTransform labelRect = labelHost.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _backLabel = labelHost.AddComponent<TextMeshProUGUI>();
            _backLabel.alignment = TextAlignmentOptions.Center;
            _backLabel.fontSize = 14f;
            _backLabel.color = Link;
            _backLabel.text = "Back";
            _backLabel.raycastTarget = false;
            UsePanelFont();

            _back = host.GetComponent<Button>();
            _back.onClick.AddListener(Back);
            host.SetActive(false);
        }

        static void UsePanelFont()
        {
            if (_backLabel == null || _backLabel.font != null || _panel == null || _view == null)
            {
                return;
            }

            RectTransform panel = _panel.GetValue(_view) as RectTransform;
            TMP_Text sample = panel != null ? panel.GetComponentInChildren<TMP_Text>(true) : null;
            if (sample != null && sample.font != null)
            {
                _backLabel.font = sample.font;
                if (sample.fontSharedMaterial != null)
                {
                    _backLabel.fontSharedMaterial = sample.fontSharedMaterial;
                }
            }
        }
    }
}
