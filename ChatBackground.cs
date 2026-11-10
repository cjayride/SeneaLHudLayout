using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SeneaLHudLayout
{
    /// <summary>
    /// [Chat] ChatBackgroundOpacity: one slider for the opacity of the chat window's dark
    /// background (the glass panel SeneaL UI draws behind the message log and the input row).
    ///
    /// SeneaL builds that background as a single stretched "bg" Image child of the chat panel
    /// (SeneaLUI.Components.Ui.Panel, sprite Skin.Sliced(48, Styles.Glass78)). The sprite carries
    /// the dark fill; the Image's own color is white, so its alpha multiplies the sprite and is the
    /// whole opacity knob. The slider writes that alpha. 1 is the panel as SeneaL ships it; 0.5 - the
    /// default - is half as opaque; 0 hides the fill (the text stays).
    ///
    /// The panel is destroyed and rebuilt (ChatSkin.Detach/Attach, HUD editor, layout changes), so
    /// the Image is found lazily by name under the panel and re-found if it goes away.
    ///
    /// SeneaL's ChatSkin also writes this alpha itself while the chat fades in/out, and that fade
    /// lands AFTER our first write - so the value is REASSERTED every frame it drifts, not applied
    /// once (the 1.5.37 startup bug: the fade-in stamped the panel back to full alpha and a
    /// one-shot guard never wrote 0.25 again). The per-frame cost is one cached reference plus a
    /// float compare; the colour is only written when it differs from the target.
    /// </summary>
    static class ChatBackground
    {
        const float Epsilon = 0.01f;

        static Image _bg;

        public static void Tick()
        {
            if (SeneaLHudLayoutPlugin.ChatBackgroundOpacity == null)
            {
                return;
            }

            float alpha = Mathf.Clamp01(SeneaLHudLayoutPlugin.ChatBackgroundOpacity.Value);
            Image bg = Find();
            if (bg == null)
            {
                return;
            }

            Color color = bg.color;
            if (Mathf.Abs(color.a - alpha) > Epsilon)
            {
                color.a = alpha;
                bg.color = color;
            }
        }

        static Image Find()
        {
            RectTransform panel = ChatWindowLayout.GetPanel();
            if (panel == null)
            {
                _bg = null;
                return null;
            }

            if (_bg != null && _bg.transform.parent == panel)
            {
                return _bg;
            }

            Transform child = panel.Find("bg");
            Image image = child != null ? child.GetComponent<Image>() : null;
            if (image == null)
            {
                return null;
            }

            _bg = image;
            return image;
        }
    }
}
