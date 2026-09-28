using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class CenterMessageRoute
    {
        static MethodInfo _tryMessage;

        public static void Apply(Harmony harmony)
        {
            MethodInfo show = AccessTools.Method(typeof(MessageHud), "ShowMessage");
            if (show == null)
            {
                return;
            }

            harmony.Patch(show, prefix: new HarmonyMethod(typeof(CenterMessageRoute), nameof(Prefix)));
            _tryMessage = AccessTools.Method("SeneaLUI.Hud.Notices:TryMessage");
        }

        static bool Prefix(MessageHud __instance, MessageHud.MessageType type, string text, int amount, Sprite icon)
        {
            if (SeneaLHudLayoutPlugin.CenterMessageToNotices == null || !SeneaLHudLayoutPlugin.CenterMessageToNotices.Value)
            {
                return true;
            }

            if (type != MessageHud.MessageType.Center || string.IsNullOrEmpty(text))
            {
                return true;
            }

            if (_tryMessage != null && _tryMessage.Invoke(null, new object[] { __instance, type, text, amount, icon }) is bool taken && taken)
            {
                return false;
            }

            __instance.ShowMessage(MessageHud.MessageType.TopLeft, text, amount, icon);
            return false;
        }
    }
}
