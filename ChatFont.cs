using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Puts a chosen TMP font asset and font size on the chat log text (Terminal::m_output).
    /// The names are the game's own font-asset names (Valheim-AveriaSansLibre, NotoSansJP-Regular SDF, ...).
    /// A name is resolved at runtime by scanning the loaded TMP_FontAsset objects, the same way
    /// ComfyLib's UIFonts::GetFontAssetByName does (Resources.FindObjectsOfTypeAll + name match).
    /// Chatter is not touched: with Chatter installed its own ChatMessage.Text.Font keys drive its
    /// panel, and these keys drive the vanilla / SeneaL-skinned chat text.
    /// SeneaL's ChatSkin::Build applies its font once (ChatSkin::Tx) and never re-applies it, so
    /// writing the font here from LateUpdate sticks.
    /// </summary>
    static class ChatFont
    {
        // The list the game ships (ZNetInstance-style acceptable values). Players can only pick these.
        public static readonly string[] FontNames =
        {
            "AveriaSansLibre-Bold SDF",
            "Fallback-NotoSansNormal",
            "Fallback-NotoSansThin",
            "Fallback-NotoSerifNormal",
            "LiberationSans SDF",
            "LiberationSans SDF - Fallback",
            "NotoEmoji-Light SDF",
            "NotoEmoji-Regular SDF",
            "NotoSansArabic-Light SDF",
            "NotoSansArabic-Regular SDF",
            "NotoSansArmenian-ExtraLight SDF",
            "NotoSansArmenian-Regular SDF",
            "NotoSansBengali-ExtraLight SDF",
            "NotoSansBengali-Regular SDF",
            "NotoSansDevanagari-ExtraLight SDF",
            "NotoSansDevanagari-Regular SDF",
            "NotoSansGeorgian-ExtraLight SDF",
            "NotoSansGeorgian-Regular SDF",
            "NotoSansHebrew-Light SDF",
            "NotoSansHebrew-Regular SDF",
            "NotoSansJP-Regular SDF",
            "NotoSansJP-Thin SDF",
            "NotoSansKR-Regular SDF",
            "NotoSansKR-Thin SDF",
            "NotoSansMalayalam-ExtraLight SDF",
            "NotoSansMalayalam-Regular SDF",
            "NotoSansSC-Regular SDF",
            "NotoSansSC-Thin SDF",
            "NotoSansThai-ExtraLight SDF",
            "NotoSansThai-Regular SDF",
            "NotoSerifArmenian-Regular SDF",
            "NotoSerifBengali-Regular SDF",
            "NotoSerifDevanagari-Regular SDF",
            "NotoSerifGeorgian-Regular SDF",
            "NotoSerifJP-Regular SDF",
            "NotoSerifKR-Regular SDF",
            "NotoSerifMalayalam-Regular SDF",
            "NotoSerifSC-Regular SDF",
            "NotoSerifThai-Regular SDF",
            "Opensans - Fallback",
            "Valheim-AveriaSansLibre",
            "Valheim-AveriaSerifLibre",
            "Valheim-Norse",
            "Valheim-Norsebold",
            "Valheim-Prstartk",
        };

        public const float MinFontSize = 6f;
        public const float MaxFontSize = 64f;

        public static float ClampFontSize(float size)
        {
            if (size < MinFontSize)
            {
                return MinFontSize;
            }

            if (size > MaxFontSize)
            {
                return MaxFontSize;
            }

            return size;
        }

        static TMP_FontAsset _asset;
        static string _appliedName;
        static float _appliedSize;
        static TMP_Text _loggedMissingFor;
        static double _nextScan;
        static bool _missingWarned;
        static bool _foundNamesLogged;

        /// <summary>
        /// Resolve a font-asset name to the loaded asset. Case-insensitive; an empty name means
        /// "leave the game's own font alone". Rescans at most once every two seconds while the
        /// requested asset is missing, so fonts loaded by other mods are picked up.
        ///
        /// The scan (Resources.FindObjectsOfTypeAll, the pattern ComfyLib.UIFonts uses) first
        /// wants an exact name; it then accepts a loaded name that starts with the requested
        /// name, because the shipped objects are not always named exactly like the config value
        /// (the game's Valheim-AveriaSansLibre asset, for instance, is reached by Chatter through
        /// an existing text's font, not by a plain name scan).
        /// </summary>
        public static TMP_FontAsset Resolve(string name, double now)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (_asset != null && string.Equals(_asset.name, name, StringComparison.OrdinalIgnoreCase))
            {
                return _asset;
            }

            if (now < _nextScan)
            {
                return null;
            }

            _nextScan = now + 2.0;
            TMP_FontAsset[] all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (all == null)
            {
                return null;
            }

            TMP_FontAsset prefixMatch = null;
            for (int i = 0; i < all.Length; i++)
            {
                TMP_FontAsset candidate = all[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    _asset = candidate;
                    _missingWarned = false;
                    _foundNamesLogged = false;
                    return candidate;
                }

                if (prefixMatch == null
                    && candidate.name != null
                    && candidate.name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    prefixMatch = candidate;
                }
            }

            if (prefixMatch != null)
            {
                _asset = prefixMatch;
                _missingWarned = false;
                return prefixMatch;
            }

            _asset = null;
            return null;
        }

        /// <summary>
        /// Once per missing name: log the font assets that are actually loaded, so the value can
        /// be fixed from the log. Throttled by the caller's warn-once.
        /// </summary>
        public static string LoadedAssetNames()
        {
            if (_foundNamesLogged)
            {
                return string.Empty;
            }

            _foundNamesLogged = true;
            TMP_FontAsset[] all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (all == null || all.Length == 0)
            {
                return "none loaded";
            }

            var names = new System.Collections.Generic.List<string>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && !string.IsNullOrEmpty(all[i].name))
                {
                    names.Add(all[i].name);
                }
            }

            return string.Join(", ", names);
        }

        public static void Tick()
        {
            if (SeneaLHudLayoutPlugin.ChatFontSettingOn == null || !SeneaLHudLayoutPlugin.ChatFontSettingOn.Value)
            {
                return;
            }

            Chat chat = Chat.instance;
            if (chat == null || chat.m_output == null)
            {
                return;
            }

            string name = SeneaLHudLayoutPlugin.ChatFontAsset == null ? string.Empty : SeneaLHudLayoutPlugin.ChatFontAsset.Value;
            float size = ClampFontSize(SeneaLHudLayoutPlugin.ChatFontSize == null ? 16f : SeneaLHudLayoutPlugin.ChatFontSize.Value);

            bool wantName = !string.IsNullOrEmpty(name);
            bool sameName = string.Equals(_appliedName ?? string.Empty, name, StringComparison.OrdinalIgnoreCase);
            bool sameSize = _appliedSize == size;
            if (sameName && sameSize && _loggedMissingFor == chat.m_output)
            {
                return;
            }

            TMP_FontAsset asset = wantName ? Resolve(name, Time.unscaledTimeAsDouble) : null;
            if (wantName && asset == null)
            {
                if (!_missingWarned)
                {
                    _missingWarned = true;
                    string loaded = LoadedAssetNames();
                    Debug.LogWarning("[SeneaLHudLayout] Chat font asset not loaded: " + name
                        + "  (loaded: " + loaded + ")");
                }

                // The requested font is not there; the chat keeps its own face, but the
                // font SIZE setting still applies.
                Apply(chat.m_output, null, false, size);
                _appliedName = name;
                _appliedSize = size;
                _loggedMissingFor = chat.m_output;
                return;
            }

            Apply(chat.m_output, asset, wantName, size);
            _appliedName = name;
            _appliedSize = size;
            _loggedMissingFor = chat.m_output;
        }

        static void Apply(TMP_Text text, TMP_FontAsset asset, bool wantName, float size)
        {
            if (text == null)
            {
                return;
            }

            try
            {
                if (wantName && asset != null && !ReferenceEquals(text.font, asset))
                {
                    text.font = asset;
                }

                if (text.fontSize != size)
                {
                    text.fontSize = size;
                }
            }
            catch (Exception)
            {
                if (_loggedMissingFor != text)
                {
                    _loggedMissingFor = text;
                    Debug.LogWarning("[SeneaLHudLayout] Could not set the chat font on the chat text.");
                }
            }
        }
    }
}
