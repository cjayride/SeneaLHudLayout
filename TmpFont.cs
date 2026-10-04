using TMPro;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class TmpFont
    {
        static TMP_FontAsset _font;
        static Material _material;

        internal static void Apply(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            Ensure();
            if (_font == null)
            {
                return;
            }

            text.font = _font;
            if (_material != null)
            {
                text.fontSharedMaterial = _material;
            }
        }

        internal static void Ensure()
        {
            if (_font != null && _font)
            {
                BindDefault();
                return;
            }

            StealFrom(Hud.instance != null ? Hud.instance.m_gpName : null);
            if (_font == null)
            {
                TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
                for (int i = 0; i < texts.Length && _font == null; i++)
                {
                    StealFrom(texts[i]);
                }
            }

            if (_font == null)
            {
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                for (int i = 0; i < fonts.Length; i++)
                {
                    TMP_FontAsset font = fonts[i];
                    if (font != null && font && !IsLiberation(font.name))
                    {
                        _font = font;
                        _material = font.material;
                        break;
                    }
                }
            }

            BindDefault();
        }

        static void StealFrom(TMP_Text sample)
        {
            if (sample == null || sample.font == null || !sample.font || IsLiberation(sample.font.name))
            {
                return;
            }

            _font = sample.font;
            _material = sample.fontSharedMaterial != null ? sample.fontSharedMaterial : sample.font.material;
        }

        static void BindDefault()
        {
            if (_font != null && _font && TMP_Settings.instance != null)
            {
                TMP_Settings.defaultFontAsset = _font;
            }
        }

        static bool IsLiberation(string name)
        {
            return !string.IsNullOrEmpty(name)
                && name.IndexOf("Liberation", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
