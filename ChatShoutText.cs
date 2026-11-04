using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

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
        }

        public static bool Applied
        {
            get { return _applied; }
        }

        /// <summary>
        /// The methods that format a shout's text: Chat.AddInworldText (6 args) and every
        /// Terminal.AddString overload (4 args). Found by name and argument count, so a signature
        /// tweak in a game update does not stop the search.
        /// </summary>
        static IEnumerable<MethodInfo> ShoutFormatters()
        {
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(Chat)))
            {
                if (m.Name == "AddInworldText" && m.GetParameters().Length == 6)
                {
                    yield return m;
                }
            }

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
                }
            }

            return body;
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
        public static string KeepTypedCase(string text)
        {
            if (text == null)
            {
                return null;
            }

            bool keep = SeneaLHudLayoutPlugin.ShoutKeepsCapitalization != null
                && SeneaLHudLayoutPlugin.ShoutKeepsCapitalization.Value;
            return keep ? text.ToUpper() : text;
        }
    }
}
