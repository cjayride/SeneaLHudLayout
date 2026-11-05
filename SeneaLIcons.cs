using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class SeneaLIcons
    {
        static Assembly _ui;
        static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public static Sprite Get(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                return null;
            }

            if (_sprites.TryGetValue(iconName, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Assembly ui = UiAssembly();
            if (ui == null)
            {
                return null;
            }

            string resource = "SeneaLUI.Icons." + iconName + ".png";
            Stream stream = ui.GetManifestResourceStream(resource);
            if (stream == null)
            {
                return null;
            }

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                bytes = ms.ToArray();
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!LoadPng(tex, bytes))
            {
                return null;
            }

            tex.filterMode = FilterMode.Bilinear;
            float ppu = tex.width > 0 ? tex.width / 14f : 100f;
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            _sprites[iconName] = sprite;
            return sprite;
        }

        static bool LoadPng(Texture2D tex, byte[] bytes)
        {
            Type conversion = AccessTools.TypeByName("UnityEngine.ImageConversion");
            if (conversion == null)
            {
                return false;
            }

            MethodInfo load = AccessTools.Method(conversion, "LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
            if (load == null)
            {
                return false;
            }

            try
            {
                return (bool)load.Invoke(null, new object[] { tex, bytes });
            }
            catch (Exception)
            {
                return false;
            }
        }

        static Assembly UiAssembly()
        {
            if (_ui != null)
            {
                return _ui;
            }

            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < loaded.Length; i++)
            {
                Assembly assembly = loaded[i];
                try
                {
                    if (string.Equals(assembly.GetName().Name, "SeneaL-UI", StringComparison.OrdinalIgnoreCase))
                    {
                        _ui = assembly;
                        return _ui;
                    }
                }
                catch (Exception)
                {
                }
            }

            return null;
        }
    }
}
