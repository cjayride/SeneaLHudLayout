using System;
using System.Collections;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Skills Reworked rebirth/prestige result text: failed confirms on the dialog, success in chat
    /// (the skills window covers the HUD popup).
    /// </summary>
    static class RebirthNotice
    {
        static string _text;
        static bool _ok;

        public static void Apply(Harmony harmony)
        {
            Type flow = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Rebirth.RebirthFlow");
            Type ui = ModTypes.Find("SkillsReworked", "SkillsReworked.UI.SkillPanel.SkillUiController");
            MethodInfo execute = flow == null ? null : AccessTools.Method(flow, "Execute");
            MethodInfo confirm = ui == null ? null : AccessTools.Method(ui, "OnRebirthConfirm");
            if (execute == null || confirm == null)
            {
                return;
            }

            harmony.Patch(execute, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(Remember)));
            harmony.Patch(confirm, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(AfterRebirth)));

            Type prestige = ModTypes.Find("SkillsReworked", "SkillsReworked.Systems.Prestige.PrestigeFlow");
            MethodInfo prestigeExecute = prestige == null ? null : AccessTools.Method(prestige, "Execute");
            MethodInfo prestigeConfirm = AccessTools.Method(ui, "OnPrestigeConfirm");
            if (prestigeExecute != null && prestigeConfirm != null)
            {
                harmony.Patch(prestigeExecute, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(Remember)));
                harmony.Patch(prestigeConfirm, postfix: new HarmonyMethod(typeof(RebirthNotice), nameof(AfterPrestige)));
            }
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