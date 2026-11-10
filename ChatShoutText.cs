using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Valheim shouts a chat message in ALL CAPS. Verified in the game's IL; both places are
    /// display-only formatting of the shout case:
    ///   Chat::AddInworldText   shout -&gt; text = text.ToUpper()   (the line above the character)
    ///   Terminal::AddString    shout -&gt; text = text.ToUpper()   (the chat log)
    /// Whispers are forced lower case in those same two places, and are left alone.
    ///
    /// [Chat] ShoutKeepsCapitalization, read straight: on = the shout keeps Valheim's shout
    /// capitalization (ALL CAPS, what the game does by itself). Off (the default) = no shout
    /// capitalization, the shout shows exactly the letters the player typed. To get that, each
    /// shout's ToUpper call is replaced with a call that hands the text back unchanged, so the
    /// setting works live without re-patching. One instruction swapped for one instruction of the
    /// same shape; nothing else moves. This is the same technique Chatter uses for its own copy
    /// of the in-world text.
    ///
    /// A transpiler that finds no ToUpper leaves the method untouched, and every patch is in a
    /// try/catch, so a game update that moves the call leaves the old caps behaviour in place.
    /// </summary>
    static class ChatShoutText
    {
        static bool _applied;

        public static void Apply(Harmony harmony)
        {
            _applied = false;

            foreach (MethodInfo target in ShoutFormatters())
            {
                if (PatchToUpper(harmony, target))
                {
                    _applied = true;
                }
            }

            MethodInfo inworld = AccessTools.Method(typeof(Chat), "AddInworldText");
            if (inworld != null)
            {
                try
                {
                    harmony.Patch(inworld,
                        prefix: new HarmonyMethod(typeof(ChatShoutText), nameof(HideOverHead)),
                        transpiler: new HarmonyMethod(typeof(ChatShoutText), nameof(ReplaceToUpper)));
                }
                catch (Exception)
                {
                }
            }
        }

        public static bool Applied
        {
            get { return _applied; }
        }

        /// <summary>
        /// The methods that format a shout's text and colour: Chat.AddInworldText (6 args, patched
        /// with its hide prefix) and every Terminal.AddString overload (4 args). Found by name and
        /// argument count, so a signature tweak in a game update does not stop the search.
        /// </summary>
        static IEnumerable<MethodInfo> ShoutFormatters()
        {
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(Terminal)))
            {
                if (m.Name == "AddString" && m.GetParameters().Length == 4)
                {
                    yield return m;
                }
            }
        }

        static bool PatchToUpper(Harmony harmony, MethodInfo target)
        {
            if (target == null)
            {
                return false;
            }

            try
            {
                harmony.Patch(target, transpiler: new HarmonyMethod(typeof(ChatShoutText), nameof(ReplaceToUpper)));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static IEnumerable<CodeInstruction> ReplaceToUpper(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo toUpper = AccessTools.Method(typeof(string), "ToUpper", Type.EmptyTypes);
            MethodInfo identity = AccessTools.Method(typeof(ChatShoutText), nameof(KeepTypedCase));
            MethodInfo shoutColor = AccessTools.Method(typeof(ChatShoutText), nameof(ShoutColor));
            MethodInfo sayColor = AccessTools.Method(typeof(ChatShoutText), nameof(SayColor));

            // Unity's Color.white / Color.yellow are static properties, so the game's IL calls the
            // getters (call get_white), it does not load a field.
            MethodInfo getWhite = AccessTools.PropertyGetter(typeof(Color), "white");
            MethodInfo getYellow = AccessTools.PropertyGetter(typeof(Color), "yellow");

            List<CodeInstruction> body = instructions.ToList();
            for (int i = 0; i < body.Count; i++)
            {
                CodeInstruction instruction = body[i];

                // Only the shout's own string: a string on the stack, callvirt ToUpper(), no arguments,
                // and the result stored straight back (verified shape of both call sites).
                if (instruction.opcode == OpCodes.Callvirt
                    && ReferenceEquals(instruction.operand, toUpper)
                    && StoresResult(body, i))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = identity;
                    continue;
                }

                // The chat colour picks: the shout line's Color.yellow getter and the say / proximity
                // line's Color.white getter. Each pushes one Color struct, exactly what the
                // replacement call pushes, so nothing else moves.
                if (getYellow != null
                    && (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                    && ReferenceEquals(instruction.operand, getYellow))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = shoutColor;
                }
                else if (getWhite != null
                    && (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                    && ReferenceEquals(instruction.operand, getWhite))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = sayColor;
                }
            }

            return body;
        }

        /// <summary>
        /// Shout text colour: the [Chat] ShoutTextColor hex (white FFFFFF by default) while
        /// ShoutWhiteText is on; Valheim's yellow with it off or the hex blank/unreadable. The
        /// settings are read per line, so they work live with no re-patch.
        /// </summary>
        public static Color ShoutColor()
        {
            if (SeneaLHudLayoutPlugin.ShoutWhite != null && SeneaLHudLayoutPlugin.ShoutWhite.Value)
            {
                Color hex;
                if (TryHexColor(SeneaLHudLayoutPlugin.ShoutColorHex, ref _shoutHexSource, ref _shoutHex, out hex))
                {
                    return hex;
                }

                return Color.white;
            }

            return Color.yellow;
        }

        /// <summary>
        /// Say / proximity text colour: the [Chat] SayTextColor hex (light grey C7C7C7 by default)
        /// while SayLightGreyText is on; Valheim's white with it off or the hex blank/unreadable.
        /// Read per line, like the shout.
        /// </summary>
        public static Color SayColor()
        {
            if (SeneaLHudLayoutPlugin.ChatSettingsOn()
                && SeneaLHudLayoutPlugin.SayLightGrey != null
                && SeneaLHudLayoutPlugin.SayLightGrey.Value)
            {
                Color hex;
                if (TryHexColor(SeneaLHudLayoutPlugin.SayColorHex, ref _sayHexSource, ref _sayHex, out hex))
                {
                    return hex;
                }

                return LightGrey;
            }

            return Color.white;
        }

        static readonly Color LightGrey = new Color(0.78f, 0.78f, 0.78f, 1f);

        // Parsed hex caches: the config string is re-parsed only when it changes.
        static string _shoutHexSource;
        static Color _shoutHex;
        static string _sayHexSource;
        static Color _sayHex;

        /// <summary>
        /// "#RRGGBB" / "RRGGBB" / "#RRGGBBAA" / "RGB" to a Unity colour, cached per source string.
        /// False for blank, malformed, or all-parsing-failed text, so the caller falls back.
        /// </summary>
        static bool TryHexColor(ConfigEntry<string> entry, ref string source, ref Color cached, out Color color)
        {
            color = default(Color);
            string text = entry == null ? null : entry.Value;
            if (string.IsNullOrWhiteSpace(text))
            {
                source = null;
                return false;
            }

            if (text == source)
            {
                color = cached;
                return true;
            }

            if (!TryParseHex(text, out color))
            {
                // Unreadable value: fall back every line, but remember the string so we do not
                // re-parse it constantly, and keep the last good colour untouched.
                source = text;
                return false;
            }

            source = text;
            cached = color;
            return true;
        }

        static bool TryParseHex(string text, out Color color)
        {
            color = default(Color);
            string s = text.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal))
            {
                s = s.Substring(1);
            }

            if (s.Length == 3)
            {
                s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
            }

            if (s.Length != 6 && s.Length != 8)
            {
                return false;
            }

            uint value;
            try
            {
                value = Convert.ToUInt32(s, 16);
            }
            catch (Exception)
            {
                return false;
            }

            if (s.Length == 6)
            {
                color = new Color(
                    ((value >> 16) & 0xFF) / 255f,
                    ((value >> 8) & 0xFF) / 255f,
                    (value & 0xFF) / 255f,
                    1f);
            }
            else
            {
                color = new Color(
                    ((value >> 24) & 0xFF) / 255f,
                    ((value >> 16) & 0xFF) / 255f,
                    ((value >> 8) & 0xFF) / 255f,
                    (value & 0xFF) / 255f);
            }

            return true;
        }

        static bool StoresResult(List<CodeInstruction> instructions, int index)
        {
            if (index + 1 >= instructions.Count)
            {
                return false;
            }

            OpCode next = instructions[index + 1].opcode;
            return next == OpCodes.Starg || next == OpCodes.Starg_S
                || next == OpCodes.Stloc || next == OpCodes.Stloc_S
                || next == OpCodes.Stfld;
        }

        /// <summary>
        /// The shout's text. With [Chat] ShoutKeepsCapitalization on the shout keeps Valheim's
        /// shout capitalization (ALL CAPS); with it off - the default - the text is handed back
        /// exactly as the player typed it. The transpiler is always installed, so the setting
        /// works live without re-patching.
        /// </summary>
        static bool HideOverHead(Talker.Type type)
        {
            if (type != Talker.Type.Shout)
            {
                return true;
            }

            return SeneaLHudLayoutPlugin.HideShoutOverHead == null
                || !SeneaLHudLayoutPlugin.HideShoutOverHead.Value;
        }

        public static string KeepTypedCase(string text)
        {
            if (text == null)
            {
                return null;
            }

            if (!SeneaLHudLayoutPlugin.ChatSettingsOn())
            {
                return text.ToUpper();
            }

            bool keep = SeneaLHudLayoutPlugin.ShoutKeepsCapitalization != null
                && SeneaLHudLayoutPlugin.ShoutKeepsCapitalization.Value;
            return keep ? text.ToUpper() : text;
        }
    }
}
