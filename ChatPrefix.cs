using System;
using System.Reflection;
using HarmonyLib;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Chat prefix defaults.
    ///
    /// Chat.InputText() is Valheim's only submit path for the chat box: the vanilla box, SeneaL UI's
    /// skinned box, and Chatter's box all end up there. Plain text becomes "say &lt;text&gt;" (close
    /// range); a leading '/' is stripped and the rest runs as a console command, where /s is Valheim's
    /// shout (the long-range line above your character).
    ///
    /// With [Chat] DefaultToShout on, the chat box opens with "/s " already typed in it, so the first
    /// Enter shouts and nobody types the prefix. Delete the "/s " and the message is a normal
    /// close-range say; the box stops prefilled and stays that way until /s is typed again, which
    /// brings the prefill back. The channel is kept in the same player prefs file the game uses for
    /// its chat key binds, so it survives a restart. [Chat] RememberLastUsed off always prefills /s.
    ///
    /// Nothing is injected: the prefilled "/s " runs Valheim's own shout, plain text runs Valheim's
    /// own say. Prefill() and Submit() are the whole decision with nothing Unity in them, so a
    /// standalone test (tools\RouteTest.exe) can prove every case.
    /// </summary>
    static class ChatPrefix
    {
        const string LastChannelKey = "cjayride.SeneaLHudLayout.ChatLastChannel";
        const string ShoutPrefix = "/s ";
        const string ShoutValue = "s";
        const string SayValue = "say";

        static string _lastChannel;
        static bool _lastChannelRead;
        static bool _open;
        static string _prefilled;
        static bool _selectOff;
        static bool _justSent;

        public static void Apply(Harmony harmony)
        {
            MethodInfo inputText = AccessTools.Method(typeof(Chat), "InputText");
            if (inputText == null)
            {
                return;
            }

            harmony.Patch(inputText, prefix: new HarmonyMethod(typeof(ChatPrefix), nameof(InputTextPrefix)));
        }

        /// <summary>
        /// Once a frame: prefill "/s " when the chat box opens, keep Chatter's placeholder in step,
        /// and stop TMP selecting all text on focus (that would make the first keystroke replace
        /// the prefix).
        /// </summary>
        public static void Tick()
        {
            Chat chat = Chat.instance;
            if (chat == null || chat.m_input == null)
            {
                _open = false;
                return;
            }

            if (!Enabled())
            {
                // Setting off: the box is Valheim's. Nothing is read, written, or changed.
                _open = false;
                _prefilled = null;
                _justSent = false;
                return;
            }

            // TMP selects all text when the box is focused, which would make the first keystroke
            // replace the prefilled prefix.
            if (!_selectOff)
            {
                _selectOff = true;
                try
                {
                    chat.m_input.onFocusSelectAll = false;
                }
                catch (Exception)
                {
                }
            }

            bool open;
            try
            {
                open = chat.m_input.gameObject != null && chat.m_input.gameObject.activeInHierarchy;
            }
            catch (Exception)
            {
                open = false;
            }

            // Prefill when the box opens, and again right after a shout, when Valheim has cleared it.
            if ((open && !_open) || (open && _justSent))
            {
                _justSent = false;
                _prefilled = Prefill(true, UsesShout(), SafeText(chat));
                if (_prefilled != null)
                {
                    try
                    {
                        chat.m_input.text = _prefilled;
                        chat.m_input.caretPosition = _prefilled.Length;
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            if (!open)
            {
                _prefilled = null;
                _justSent = false;
            }

            _open = open;
            SyncChatterPlaceholder();
        }

        /// <summary>
        /// Returns false when this patch handled the text, so the game's own InputText is skipped.
        /// </summary>
        static bool InputTextPrefix(Chat __instance)
        {
            if (__instance == null || __instance.m_input == null)
            {
                return true;
            }

            string text = SafeText(__instance);
            RouteResult route = Submit(text, Enabled(), ChatterPrefix(), _prefilled);

            if (route.Remember != null)
            {
                Remember(route.Remember.Value);
            }

            if (route.Remember == true)
            {
                _justSent = true;
            }

            // Swallow: the prefilled "/s " with nothing after it. Valheim's shout needs three
            // characters, so running it would only print a failed-command line.
            return route.Action != RouteAction.Swallow;
        }

        public enum RouteAction
        {
            /// <summary>Let Valheim's own InputText run the text.</summary>
            Vanilla,

            /// <summary>Nothing to send; swallow the text so no failed-command line is printed.</summary>
            Swallow
        }

        public struct RouteResult
        {
            public RouteAction Action;

            /// <summary>True for shout, false for say, null when the channel is not recorded.</summary>
            public bool? Remember;

            public override string ToString()
            {
                string channel = Remember == null ? "-" : (Remember.Value ? "shout" : "say");
                return Action + " remember=" + channel;
            }
        }

        /// <summary>
        /// What to type into the box when it opens: "/s " while the channel in use is shout, nothing
        /// while it is say (plain text is then Valheim's close-range say).
        /// </summary>
        public static string Prefill(bool enabled, bool usesShout, string currentText)
        {
            if (!enabled || !usesShout || !string.IsNullOrEmpty(currentText))
            {
                return null;
            }

            return ShoutPrefix;
        }

        /// <summary>
        /// The whole submit decision. text is what the box holds; chatterPrefix is Chatter's prefix
        /// for plain text when Chatter is installed ("s " when its panel is set to shout, "say "
        /// normally, null when Chatter is absent); prefill is what this mod typed in on open.
        /// </summary>
        public static RouteResult Submit(string text, bool enabled, string chatterPrefix, string prefill)
        {
            var plain = new RouteResult { Action = RouteAction.Vanilla, Remember = null };
            if (!enabled || string.IsNullOrEmpty(text))
            {
                return plain;
            }

            if (text == prefill)
            {
                // Untouched "/s ": nothing to shout.
                return new RouteResult { Action = RouteAction.Swallow, Remember = true };
            }

            if (text[0] != '/')
            {
                // Plain text: Valheim turns it into say. With Chatter, Chatter's own prefix decides
                // (its panel can default to shout), so remember the channel the game will use.
                bool shout = chatterPrefix == ShoutValue + " ";
                return new RouteResult { Action = RouteAction.Vanilla, Remember = shout };
            }

            string rest = text.Substring(1);
            string token = FirstToken(rest);
            string lower = token.ToLowerInvariant();
            string tail = rest.Substring(token.Length);

            if (lower == ShoutValue)
            {
                if (tail.Trim().Length == 0)
                {
                    return new RouteResult { Action = RouteAction.Swallow, Remember = true };
                }

                return new RouteResult { Action = RouteAction.Vanilla, Remember = true };
            }

            if (lower == SayValue)
            {
                return new RouteResult { Action = RouteAction.Vanilla, Remember = false };
            }

            if (lower == "shout")
            {
                return new RouteResult { Action = RouteAction.Vanilla, Remember = true };
            }

            return plain;
        }

        static string FirstToken(string s)
        {
            int i = s.IndexOf(' ');
            return i < 0 ? s : s.Substring(0, i);
        }

        static string SafeText(Chat chat)
        {
            try
            {
                return chat.m_input.text;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        static bool Enabled()
        {
            return SeneaLHudLayoutPlugin.ChatSettingsOn()
                && SeneaLHudLayoutPlugin.DefaultToShout != null
                && SeneaLHudLayoutPlugin.DefaultToShout.Value;
        }

        static bool RememberLastUsedOn()
        {
            return SeneaLHudLayoutPlugin.ChatSettingsOn()
                && SeneaLHudLayoutPlugin.RememberLastUsed != null
                && SeneaLHudLayoutPlugin.RememberLastUsed.Value;
        }

        static bool UsesShout()
        {
            return !RememberLastUsedOn() || LastChannel() != SayValue;
        }

        static string LastChannel()
        {
            if (_lastChannelRead)
            {
                return _lastChannel;
            }

            _lastChannelRead = true;
            try
            {
                string saved = PlatformPrefs.GetString(LastChannelKey, string.Empty);
                if (saved == ShoutValue || saved == SayValue)
                {
                    _lastChannel = saved;
                }
            }
            catch (Exception)
            {
            }

            return _lastChannel;
        }

        static void Remember(bool shout)
        {
            string value = shout ? ShoutValue : SayValue;
            bool changed = !string.Equals(LastChannel(), value, StringComparison.Ordinal);

            if (changed)
            {
                _lastChannel = value;
                _lastChannelRead = true;
            }

            if (RememberLastUsedOn() && changed)
            {
                try
                {
                    PlatformPrefs.SetString(LastChannelKey, value);
                    PlatformPrefs.Save();
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Chatter's own prefix for plain text, "say " / "s " / "w ". Null when Chatter is not
        /// installed or its panel is not built yet.
        /// </summary>
        static string ChatterPrefix()
        {
            Type utils = ModTypes.Find("Chatter", "Chatter.ChatTextInputUtils");
            PropertyInfo prefix = utils == null ? null : AccessTools.Property(utils, "ChatTextInputPrefix");
            if (prefix == null)
            {
                return null;
            }

            try
            {
                return prefix.GetValue(null, null) as string;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Keeps Chatter's box placeholder ("/shout" / "/say ") in step with the channel in use.
        /// Nothing in Chatter is patched; this calls Chatter's own setter.
        /// </summary>
        static void SyncChatterPlaceholder()
        {
            Type controller = ModTypes.Find("Chatter", "Chatter.ChatPanelController");
            PropertyInfo panelProp = controller == null ? null : AccessTools.Property(controller, "ChatPanel");
            object panel;
            try
            {
                panel = panelProp == null ? null : panelProp.GetValue(null, null);
            }
            catch (Exception)
            {
                return;
            }

            if (panel == null)
            {
                return;
            }

            Type utils = ModTypes.Find("Chatter", "Chatter.ChatTextInputUtils");
            MethodInfo setter = utils == null ? null : AccessTools.Method(utils, "SetChatTextInputPrefix",
                new Type[] { panel.GetType(), typeof(Talker.Type) });
            if (setter == null)
            {
                return;
            }

            try
            {
                Talker.Type channel = UsesShout() ? Talker.Type.Shout : Talker.Type.Normal;
                setter.Invoke(null, new object[] { panel, channel });
            }
            catch (Exception)
            {
            }
        }
    }
}
