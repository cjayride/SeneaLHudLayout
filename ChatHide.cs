using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class ChatHide
    {
        const float AlwaysOnDelay = 1e6f;

        static FieldInfo _hideDelayField;
        static FieldInfo _hideTimerField;
        static FieldInfo _group;
        static FieldInfo _vis;
        static bool _bound;

        public static void Apply(Harmony harmony)
        {
            MethodInfo tick = AccessTools.Method("SeneaLUI.Hud.ChatSkin:Tick");
            if (tick == null)
            {
                return;
            }

            _bound = false;
            Type skin = AccessTools.TypeByName("SeneaLUI.Hud.ChatSkin");
            if (skin != null)
            {
                _group = AccessTools.Field(skin, "_group");
                _vis = AccessTools.Field(skin, "_vis");
                _bound = _group != null;
            }

            harmony.Patch(tick,
                prefix: new HarmonyMethod(typeof(ChatHide), nameof(SkipSkin)),
                postfix: _bound ? new HarmonyMethod(typeof(ChatHide), nameof(KeepVisible)) : null);
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

        /// <summary>
        /// [Chat] ChatAlwaysVisible on: the chat window never fades out. The game closes it when its
        /// hide timer runs out, and SeneaL fades the panel with the HUD, so both are undone.
        /// HideChat wins when both are on.
        /// </summary>
        public static bool AlwaysVisible()
        {
            if (SeneaLHudLayoutPlugin.HideChat != null && SeneaLHudLayoutPlugin.HideChat.Value)
            {
                return false;
            }

            return SeneaLHudLayoutPlugin.ChatAlwaysVisible != null
                && SeneaLHudLayoutPlugin.ChatAlwaysVisible.Value;
        }

        /// <summary>
        /// Runs after SeneaL's own chat panel frame: pin the panel at full alpha (no HUD fade-out)
        /// while ChatAlwaysVisible is on.
        /// </summary>
        static void KeepVisible()
        {
            if (!AlwaysVisible())
            {
                return;
            }

            Chat chat = Chat.instance;
            if (chat == null || chat.m_chatWindow == null)
            {
                return;
            }

            try
            {
                GameObject window = chat.m_chatWindow.gameObject;
                if (!window.activeSelf)
                {
                    window.SetActive(true);
                }

                CanvasGroup group = _group.GetValue(null) as CanvasGroup;
                if (group == null)
                {
                    return;
                }

                if (!Mathf.Approximately(group.alpha, 1f))
                {
                    group.alpha = 1f;
                }

                if (!group.blocksRaycasts)
                {
                    group.blocksRaycasts = true;
                }

                if (_vis != null && _vis.GetValue(null) is float vis && vis < 1f)
                {
                    _vis.SetValue(null, 1f);
                }
            }
            catch (Exception)
            {
            }
        }

        static void ApplyHideDelay(Chat chat)
        {
            if (AlwaysVisible())
            {
                // Nothing may close the panel, so the game's own timer is parked far below the delay.
                try
                {
                    FieldInfo delay = _hideDelayField ??= AccessTools.Field(typeof(Chat), "m_hideDelay");
                    FieldInfo timer = _hideTimerField ??= AccessTools.Field(typeof(Chat), "m_hideTimer");
                    if (delay != null && timer != null)
                    {
                        object live = delay.GetValue(chat);
                        if (!(live is float current) || !Mathf.Approximately(current, AlwaysOnDelay))
                        {
                            delay.SetValue(chat, AlwaysOnDelay);
                        }

                        timer.SetValue(chat, 0f);
                    }
                }
                catch (Exception)
                {
                }

                return;
            }

            if (!SeneaLHudLayoutPlugin.ChatSettingsOn() || SeneaLHudLayoutPlugin.HideChatPanelDelay == null)
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
