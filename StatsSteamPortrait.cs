using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeneaLHudLayout
{
    static class StatsSteamPortrait
    {
        static bool _looked;
        static Type _steamIdType;
        static MethodInfo _getSteamId;
        static MethodInfo _getAvatar;
        static MethodInfo _getImageSize;
        static MethodInfo _getImageRgba;

        public static bool TryGetLocal(out Texture2D texture)
        {
            texture = null;
            if (!Lookup())
            {
                return false;
            }

            try
            {
                object steamId = _getSteamId.Invoke(null, null);
                if (steamId == null)
                {
                    return false;
                }

                int image = (int)_getAvatar.Invoke(null, new[] { steamId });
                if (image <= 0)
                {
                    return false;
                }

                object[] sizeArgs = { image, 0u, 0u };
                if (!(bool)_getImageSize.Invoke(null, sizeArgs))
                {
                    return false;
                }

                uint w = (uint)sizeArgs[1];
                uint h = (uint)sizeArgs[2];
                if (w < 1 || h < 1 || w > 2048 || h > 2048)
                {
                    return false;
                }

                int bytes = (int)(w * h * 4);
                byte[] rgba = new byte[bytes];
                if (!(bool)_getImageRgba.Invoke(null, new object[] { image, rgba, bytes }))
                {
                    return false;
                }

                FlipVertical(rgba, (int)w, (int)h);
                var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
                tex.LoadRawTextureData(rgba);
                tex.Apply(false, true);
                texture = tex;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void FlipVertical(byte[] rgba, int width, int height)
        {
            int row = width * 4;
            byte[] temp = new byte[row];
            for (int y = 0; y < height / 2; y++)
            {
                int top = y * row;
                int bottom = (height - 1 - y) * row;
                Buffer.BlockCopy(rgba, top, temp, 0, row);
                Buffer.BlockCopy(rgba, bottom, rgba, top, row);
                Buffer.BlockCopy(temp, 0, rgba, bottom, row);
            }
        }

        static bool Lookup()
        {
            if (_looked)
            {
                return _getSteamId != null;
            }

            _looked = true;
            Type friends = AccessTools.TypeByName("Steamworks.SteamFriends");
            Type user = AccessTools.TypeByName("Steamworks.SteamUser");
            Type utils = AccessTools.TypeByName("Steamworks.SteamUtils");
            _steamIdType = AccessTools.TypeByName("Steamworks.CSteamID");
            _getSteamId = user != null ? AccessTools.Method(user, "GetSteamID") : null;
            _getAvatar = friends != null && _steamIdType != null
                ? AccessTools.Method(friends, "GetMediumFriendAvatar", new[] { _steamIdType })
                : null;
            _getImageSize = utils != null ? AccessTools.Method(utils, "GetImageSize") : null;
            _getImageRgba = utils != null ? AccessTools.Method(utils, "GetImageRGBA") : null;
            return _getSteamId != null && _getAvatar != null && _getImageSize != null && _getImageRgba != null;
        }
    }
}
