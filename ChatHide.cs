using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class ChatHide
    {
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
            if (SeneaLHudLayoutPlugin.HideChat == null || !SeneaLHudLayoutPlugin.HideChat.Value)
            {
                return;
            }

            Chat chat = Chat.instance;
            if (chat == null || chat.m_chatWindow == null)
            {
                return;
            }

            GameObject window = chat.m_chatWindow.gameObject;
            if (window.activeSelf)
            {
                window.SetActive(false);
            }
        }

        static bool SkipSkin()
        {
            return SeneaLHudLayoutPlugin.HideChat == null || !SeneaLHudLayoutPlugin.HideChat.Value;
        }
    }
}
