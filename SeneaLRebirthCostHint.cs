using System;
using System.Reflection;
using System.Text;
using BepInEx.Bootstrap;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// SkillsReworked rebirth cost to the right of the Rebirth button on SeneaL UI's skills window.
    /// Talent Pace covers the vanilla SkillsReworked window.
    /// </summary>
    static class SeneaLRebirthCostHint
    {
        const string SkillsReworkedGuid = "M2Valheim.SkillsReworked";

        static TextMeshProUGUI _label;
        static RectTransform _labelRoot;
        static bool _loggedHook;
        static bool _loggedShow;

        static Type _skillsViewType;
        static Type _goldButtonType;
        static FieldInfo _resetButton;
        static FieldInfo _prestigeButton;
        static FieldInfo _goldRoot;
        static PropertyInfo _goldText;
        static MethodInfo _getTotalAllocated;
        static PropertyInfo _rebirthCostEnabled;
        static PropertyInfo _rebirthCostMode;
        static PropertyInfo _rebirthLevelLoss;
        static PropertyInfo _rebirthItemName;
        static PropertyInfo _rebirthItemAmount;
        static MethodInfo _getItemDisplayName;

        public static void OnToggleChanged()
        {
            if (ShowRebirthCostEnabled())
            {
                return;
            }

            HideLabel();
        }

        static bool ShowRebirthCostEnabled()
        {
            return SeneaLHudLayoutPlugin.ShowRebirthCost == null || SeneaLHudLayoutPlugin.ShowRebirthCost.Value;
        }

        public static void Apply(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.ContainsKey(SkillsReworkedGuid))
            {
                return;
            }

            _skillsViewType = ModTypes.Find("SeneaL-UI", "SeneaLUI.Inv.SkillsView") ??
                              ModTypes.FindAny("SeneaLUI.Inv.SkillsView");
            MethodInfo layout = _skillsViewType == null
                ? null
                : ModTypes.DeclaredMethod(_skillsViewType, "LayoutButtons", new[] { typeof(bool), typeof(bool) })
                  ?? AccessTools.Method(_skillsViewType, "LayoutButtons");
            if (layout == null)
            {
                Debug.LogWarning("[SeneaL HUD Layout] Rebirth cost hint: SeneaL SkillsView.LayoutButtons was not found.");
                return;
            }

            harmony.Patch(layout, postfix: new HarmonyMethod(typeof(SeneaLRebirthCostHint), nameof(AfterLayoutButtons)));
            if (!_loggedHook)
            {
                _loggedHook = true;
                Debug.Log("[SeneaL HUD Layout] Rebirth cost hint: hooked " + layout.DeclaringType.FullName + "." + layout.Name);
            }
        }

        static bool EnsureTypes()
        {
            if (_resetButton != null && _goldRoot != null)
            {
                return true;
            }

            if (_skillsViewType == null)
            {
                _skillsViewType = ModTypes.Find("SeneaL-UI", "SeneaLUI.Inv.SkillsView") ??
                                  ModTypes.FindAny("SeneaLUI.Inv.SkillsView");
            }

            _goldButtonType = _goldButtonType ??
                              ModTypes.Find("SeneaL-UI", "SeneaLUI.Components.GoldButton") ??
                              ModTypes.FindAny("SeneaLUI.Components.GoldButton");
            Type modConfig = ModTypes.Find("SkillsReworked", "SkillsReworked.Bootstrap.ModConfig");
            Type itemCost = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Costs.ItemCostService");
            Type ui = ModTypes.Find("SkillsReworked", "SkillsReworked.UI.SkillPanel.SkillUiController");

            if (_skillsViewType == null || _goldButtonType == null || modConfig == null)
            {
                return false;
            }

            _resetButton = AccessTools.Field(_skillsViewType, "_reset");
            _prestigeButton = AccessTools.Field(_skillsViewType, "_prestige");
            _goldRoot = AccessTools.Field(_goldButtonType, "Root");
            _goldText = AccessTools.Property(_goldButtonType, "Text");
            _getTotalAllocated = ui == null ? null : AccessTools.Method(ui, "GetTotalAllocated");
            _rebirthCostEnabled = AccessTools.Property(modConfig, "RebirthCostEnabled");
            _rebirthCostMode = AccessTools.Property(modConfig, "RebirthCostModeSetting");
            _rebirthLevelLoss = AccessTools.Property(modConfig, "RebirthLevelLoss");
            _rebirthItemName = AccessTools.Property(modConfig, "RebirthItemName");
            _rebirthItemAmount = AccessTools.Property(modConfig, "RebirthItemAmount");
            if (itemCost != null)
            {
                _getItemDisplayName = AccessTools.Method(itemCost, "GetItemDisplayName");
            }

            return _resetButton != null && _goldRoot != null;
        }

        static void AfterLayoutButtons(object __instance)
        {
            if (!ShowRebirthCostEnabled() || __instance == null || !EnsureTypes())
            {
                return;
            }

            object resetObj = _resetButton.GetValue(__instance);
            RectTransform buttonRoot = resetObj == null ? null : _goldRoot.GetValue(resetObj) as RectTransform;
            if (buttonRoot == null || !buttonRoot.gameObject.activeInHierarchy)
            {
                HideLabel();
                return;
            }

            if (IsResetMode(__instance, resetObj))
            {
                HideLabel();
                return;
            }

            string cost = BuildCostLine();
            if (string.IsNullOrEmpty(cost))
            {
                HideLabel();
                return;
            }

            TMP_Text template = _goldText == null ? null : _goldText.GetValue(resetObj) as TMP_Text;
            EnsureLabel(buttonRoot, template);
            if (_label == null)
            {
                return;
            }

            _labelRoot.gameObject.SetActive(true);
            float maxWidth = PlaceBeside(buttonRoot, PrestigeRoot(__instance));
            FitSingleLine(cost, maxWidth, buttonRoot.sizeDelta.y);

            if (!_loggedShow)
            {
                _loggedShow = true;
                Debug.Log("[SeneaL HUD Layout] Rebirth cost hint: showing '" + cost + "'");
            }
        }

        static bool IsResetMode(object skillsView, object resetObj)
        {
            if (_getTotalAllocated != null)
            {
                try
                {
                    object n = _getTotalAllocated.Invoke(null, null);
                    if (n is int allocated && allocated > 0)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }

            TMP_Text text = _goldText == null ? null : _goldText.GetValue(resetObj) as TMP_Text;
            if (text == null || Localization.instance == null)
            {
                return false;
            }

            string reset = Localization.instance.Localize("$m2_sr_btn_reset");
            return !string.IsNullOrEmpty(reset) &&
                   string.Equals(text.text, reset, StringComparison.OrdinalIgnoreCase);
        }

        static RectTransform PrestigeRoot(object skillsView)
        {
            if (_prestigeButton == null)
            {
                return null;
            }

            object prestige = _prestigeButton.GetValue(skillsView);
            RectTransform root = prestige == null ? null : _goldRoot.GetValue(prestige) as RectTransform;
            if (root == null || !root.gameObject.activeInHierarchy)
            {
                return null;
            }

            return root;
        }

        /// <summary>Dialog panel right inset matches SeneaL Dialog.PadX (28px) plus ~one character.</summary>
        const float PanelRightPad = 36f;

        static float PlaceBeside(RectTransform rebirth, RectTransform prestige)
        {
            RectTransform neighbor = prestige != null ? prestige : rebirth;
            RectTransform panel = rebirth.parent as RectTransform;
            _labelRoot.SetParent(rebirth.parent, false);
            _labelRoot.anchorMin = neighbor.anchorMin;
            _labelRoot.anchorMax = neighbor.anchorMax;
            _labelRoot.pivot = new Vector2(0f, neighbor.pivot.y);
            float leftX = neighbor.anchoredPosition.x + neighbor.sizeDelta.x + 10f;
            _labelRoot.anchoredPosition = new Vector2(leftX, neighbor.anchoredPosition.y);
            float maxWidth = 220f;
            if (panel != null)
            {
                maxWidth = Mathf.Max(80f, panel.sizeDelta.x - leftX - PanelRightPad);
            }

            if (_label != null)
            {
                _label.margin = new Vector4(0f, 0f, 4f, 0f);
            }

            _labelRoot.SetAsLastSibling();
            return maxWidth;
        }

        static void FitSingleLine(string text, float maxWidth, float height)
        {
            const float maxFont = 16f;
            const float minFont = 10f;

            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.enableAutoSizing = false;
            _label.text = text;

            float size = maxFont;
            _label.fontSize = size;
            float preferred = MeasurePreferredWidth(text);

            while (size > minFont && preferred > maxWidth)
            {
                size -= 0.5f;
                _label.fontSize = size;
                preferred = MeasurePreferredWidth(text);
            }

            float width = Mathf.Min(Mathf.Ceil(preferred + 2f), maxWidth);
            _labelRoot.sizeDelta = new Vector2(width, height);
        }

        static float MeasurePreferredWidth(string text)
        {
            _label.ForceMeshUpdate(true);
            return _label.GetPreferredValues(text, 10000f, 0f).x;
        }

        static void HideLabel()
        {
            if (_labelRoot != null)
            {
                _labelRoot.gameObject.SetActive(false);
            }
        }

        static void EnsureLabel(RectTransform rebirth, TMP_Text template)
        {
            if (_labelRoot != null && _label != null)
            {
                return;
            }

            Transform parent = rebirth.parent;
            Transform existing = parent == null ? null : parent.Find("HudLayout_RebirthCost");
            if (existing != null)
            {
                _labelRoot = existing as RectTransform;
                _label = existing.GetComponent<TextMeshProUGUI>();
                return;
            }

            GameObject go = new GameObject("HudLayout_RebirthCost");
            _labelRoot = go.AddComponent<RectTransform>();
            _labelRoot.SetParent(parent, false);

            _label = go.AddComponent<TextMeshProUGUI>();
            _label.raycastTarget = false;
            _label.fontSize = 16f;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.margin = new Vector4(0f, 0f, 4f, 0f);
            _label.color = new Color(0.92f, 0.78f, 0.42f, 1f);
            if (template != null)
            {
                _label.font = template.font;
                _label.fontSharedMaterial = template.fontSharedMaterial;
            }
        }

        static string BuildCostLine()
        {
            if (_rebirthCostEnabled == null)
            {
                return null;
            }

            if (!(_rebirthCostEnabled.GetValue(null, null) is bool enabled) || !enabled)
            {
                return null;
            }

            string modeName = _rebirthCostMode.GetValue(null, null)?.ToString() ?? string.Empty;
            bool useLevel = modeName == "LevelOnly" || modeName == "Both";
            bool useItem = modeName == "ItemOnly" || modeName == "Both";

            int levelLoss = _rebirthLevelLoss.GetValue(null, null) is int levels ? levels : 0;
            int itemAmount = _rebirthItemAmount.GetValue(null, null) is int amount ? amount : 0;
            string itemPrefab = _rebirthItemName.GetValue(null, null) as string;

            var parts = new StringBuilder(64);
            if (useLevel && levelLoss > 0)
            {
                parts.Append(levelLoss).Append(levelLoss == 1 ? " Level" : " Levels");
            }

            if (useItem && itemAmount > 0 && !string.IsNullOrEmpty(itemPrefab))
            {
                string itemName = ItemDisplayName(itemPrefab);
                if (parts.Length > 0)
                {
                    parts.Append(", ");
                }

                parts.Append(itemAmount).Append(' ').Append(itemName);
            }

            if (parts.Length == 0)
            {
                return null;
            }

            return "Cost: " + parts;
        }

        static string ItemDisplayName(string prefab)
        {
            if (_getItemDisplayName != null)
            {
                try
                {
                    object display = _getItemDisplayName.Invoke(null, new object[] { prefab });
                    if (display is string localized && !string.IsNullOrEmpty(localized))
                    {
                        return localized;
                    }
                }
                catch (Exception)
                {
                }
            }

            return prefab;
        }
    }
}