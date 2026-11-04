using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class ChatHide
    {
        static FieldInfo _hideDelayField;

        public static void Apply(Harmony harmony)
        {
            MethodInfo tick = AccessTools.Method("SeneaLUI.Hud.ChatSkin:Tick");
            if (tick == null)
            {
                return;
            }

            harmony.Patch(tick, prefix: new HarmonyMethod(typeof(ChatHide), nameof(SkipSkin)));
        }

        public static void Tick()
        {
            Chat chat = Chat.instance;
            if (chat == null)
            {
                return;
            }

            ApplyHideDelay(chat);

            if (SeneaLHudLayoutPlugin.HideChat == null || !SeneaLHudLayoutPlugin.HideChat.Value)
            {
                return;
            }

            if (chat.m_chatWindow == null)
            {
                return;
            }

            GameObject window = chat.m_chatWindow.gameObject;
            if (window.activeSelf)
            {
                window.SetActive(false);
            }
        }

        static void ApplyHideDelay(Chat chat)
        {
            if (SeneaLHudLayoutPlugin.HideChatPanelDelay == null)
            {
                return;
            }

            float value = Mathf.Clamp(SeneaLHudLayoutPlugin.HideChatPanelDelay.Value, 1f, 600f);
            try
            {
                FieldInfo delay = _hideDelayField ??= AccessTools.Field(typeof(Chat), "m_hideDelay");
                if (delay == null)
                {
                    return;
                }

                object live = delay.GetValue(chat);
                if (!(live is float current) || !Mathf.Approximately(current, value))
                {
                    delay.SetValue(chat, value);
                }
            }
            catch (Exception)
            {
            }
        }

        static bool SkipSkin()
        {
            return SeneaLHudLayoutPlugin.HideChat == null || !SeneaLHudLayoutPlugin.HideChat.Value;
        }
    }
}
