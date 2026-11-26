using System;
using System.Collections;
using System.Reflection;
using System.Text;
using BepInEx.Bootstrap;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Skills Reworked sends rebirth and prestige results through the HUD.
    /// The skills window is still open, so that popup sits behind the panel and fades out.
    /// Failure is written back onto the confirm dialog. Success is written into chat.
    /// Rebirth confirm text is expanded with reset scope and configured costs when the dialog opens.
    /// </summary>
    static class RebirthNotice
    {
        const string SkillsReworkedGuid = "M2Valheim.SkillsReworked";
        const string TalentTreeGuid = "M2Valheim.TalentTree";

        static string _text;
        static bool _ok;

        public static void Apply(Harmony harmony)
        {
            Type flow = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Rebirth.RebirthFlow");
            Type ui = ModTypes.Find("SkillsReworked", "SkillsReworked.UI.SkillPanel.SkillUiController");
            MethodInfo execute = flow == null ? null : AccessTools.Method(flow, "Execute");
            MethodInfo confirm = ui == null ? null : AccessTools.Method(ui, "OnRebirthConfirm");
            MethodInfo requested = ui == null ? null : AccessTools.Method(ui, "OnRebirthRequested");
            if (execute == null || confirm == null)
            {
                return;
            }

            harmony.Patch(execute, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(Remember)));
            harmony.Patch(confirm, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(AfterRebirth)));
            if (requested != null)
            {
                harmony.Patch(requested, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(AfterRebirthRequested)));
            }

            Type prestige = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Prestige.PrestigeFlow");
            MethodInfo prestigeExecute = prestige == null ? null : AccessTools.Method(prestige, "Execute");
            MethodInfo prestigeConfirm = AccessTools.Method(ui, "OnPrestigeConfirm");
            if (prestigeExecute != null && prestigeConfirm != null)
            {
                harmony.Patch(prestigeExecute, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(Remember)));
                harmony.Patch(prestigeConfirm, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(AfterPrestige)));
            }
        }

        static void AfterRebirthRequested()
        {
            string text = BuildRebirthConfirmText();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            SetRebirthDialogText(text);
        }

        static string BuildRebirthConfirmText()
        {
            if (Localization.instance == null)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(Localization.instance.Localize("$m2_sr_rebirth_confirm_text"));

            builder.Append('\n');
            builder.Append("Resets SkillsReworked skills and attributes, and refunds spent attribute points.");
            if (TalentTreeLoaded())
            {
                builder.Append('\n');
                builder.Append("Also clears your Talent Tree.");
            }

            foreach (string costLine in RebirthCostLines())
            {
                builder.Append('\n');
                builder.Append(costLine);
            }

            return builder.ToString();
        }

        static bool TalentTreeLoaded()
        {
            return Chainloader.PluginInfos != null &&
                   Chainloader.PluginInfos.ContainsKey(TalentTreeGuid);
        }

        static IEnumerable RebirthCostLines()
        {
            if (!SkillsReworkedLoaded())
            {
                yield break;
            }

            Type config = ModTypes.Find("SkillsReworked", "SkillsReworked.Bootstrap.ModConfig");
            if (config == null)
            {
                yield break;
            }

            if (!GetStaticBool(config, "RebirthCostEnabled", true))
            {
                yield break;
            }

            object mode = GetStatic(config, "RebirthCostModeSetting");
            bool levelCost = CostModeIncludesLevel(mode);
            bool itemCost = CostModeIncludesItem(mode);

            if (levelCost)
            {
                int levelLoss = GetStaticInt(config, "RebirthLevelLoss", 0);
                if (levelLoss > 0)
                {
                    string format = Localization.instance.Localize("$m2_sr_rebirth_need_levels");
                    yield return string.Format(format, levelLoss);
                }
            }

            if (itemCost)
            {
                int amount = GetStaticInt(config, "RebirthItemAmount", 0);
                string prefab = GetStaticString(config, "RebirthItemName");
                if (amount > 0 && !string.IsNullOrEmpty(prefab))
                {
                    string display = ItemDisplayName(prefab);
                    string format = Localization.instance.Localize("$m2_sr_rebirth_need_items");
                    yield return string.Format(format, amount, display);
                }
            }
        }

        static bool SkillsReworkedLoaded()
        {
            return Chainloader.PluginInfos != null &&
                   Chainloader.PluginInfos.ContainsKey(SkillsReworkedGuid);
        }

        static bool CostModeIncludesLevel(object mode)
        {
            if (mode == null)
            {
                return true;
            }

            string name = mode.ToString();
            if (name == "None")
            {
                return false;
            }

            return name == "LevelOnly" || name == "Both";
        }

        static bool CostModeIncludesItem(object mode)
        {
            if (mode == null)
            {
                return true;
            }

            string name = mode.ToString();
            if (name == "None")
            {
                return false;
            }

            return name == "ItemOnly" || name == "Both";
        }

        static string ItemDisplayName(string prefab)
        {
            Type itemCosts = ModTypes.Find("SkillsReworked", "M2Core.Content.ItemCosts");
            MethodInfo display = itemCosts == null ? null : ModTypes.Method(itemCosts, "DisplayName", new[] { typeof(string) });
            if (display == null)
            {
                return prefab;
            }

            try
            {
                return display.Invoke(null, new object[] { prefab }) as string ?? prefab;
            }
            catch (Exception)
            {
                return prefab;
            }
        }

        static object GetStatic(Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            return property == null ? null : property.GetValue(null, null);
        }

        static bool GetStaticBool(Type type, string name, bool fallback)
        {
            object value = GetStatic(type, name);
            return value is bool b ? b : fallback;
        }

        static int GetStaticInt(Type type, string name, int fallback)
        {
            object value = GetStatic(type, name);
            return value is int i ? i : fallback;
        }

        static string GetStaticString(Type type, string name)
        {
            object value = GetStatic(type, name);
            return value as string;
        }

        static void Remember(object __result)
        {
            _ok = false;
            _text = null;
            if (__result == null)
            {
                return;
            }

            PropertyInfo success = __result.GetType().GetProperty("Success");
            _ok = success != null && success.GetValue(__result, null) is bool ok && ok;

            PropertyInfo notes = __result.GetType().GetProperty("Notifications");
            IEnumerable list = notes == null ? null : notes.GetValue(__result, null) as IEnumerable;
            if (list == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (object note in list)
            {
                string message = note.GetType().GetProperty("Message")?.GetValue(note, null) as string;
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(message);
            }

            _text = builder.Length == 0 ? null : builder.ToString();
        }

        static void AfterRebirth()
        {
            Deliver("_rebirthDialogRoot");
        }

        static void AfterPrestige()
        {
            Deliver("_prestigeDialogRoot");
        }

        static void SetRebirthDialogText(string text)
        {
            Type ui = ModTypes.Find("SkillsReworked", "SkillsReworked.UI.SkillPanel.SkillUiController");
            FieldInfo field = ui == null ? null : AccessTools.Field(ui, "_rebirthDialogRoot");
            GameObject root = field == null ? null : field.GetValue(null) as GameObject;
            if (root == null)
            {
                return;
            }

            SetDialogLabel(root, text);
        }

        static void Deliver(string rootField)
        {
            if (string.IsNullOrEmpty(_text))
            {
                return;
            }

            if (_ok)
            {
                ToChat(_text);
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, _text, 0, null, true);
                }

                return;
            }

            Type ui = ModTypes.Find("SkillsReworked", "SkillsReworked.UI.SkillPanel.SkillUiController");
            FieldInfo field = ui == null ? null : AccessTools.Field(ui, rootField);
            GameObject root = field == null ? null : field.GetValue(null) as GameObject;
            if (root == null)
            {
                ToChat(_text);
                return;
            }

            SetDialogLabel(root, _text);
            root.SetActive(true);
            ToChat(_text);
        }

        static void SetDialogLabel(GameObject root, string text)
        {
            Text label = root.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = text;
                return;
            }

            TMP_Text tmp = root.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = text;
            }
        }

        static void ToChat(string text)
        {
            Chat chat = Chat.instance;
            if (chat == null)
            {
                return;
            }

            FieldInfo hide = AccessTools.Field(typeof(Chat), "m_hideTimer");
            if (hide != null)
            {
                hide.SetValue(chat, 0f);
            }

            chat.AddString(text);
        }
    }
}