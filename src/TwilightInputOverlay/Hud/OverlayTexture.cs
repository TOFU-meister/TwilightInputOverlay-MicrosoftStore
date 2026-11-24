using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace TwilightInputOverlay
{
    internal static class OverlayTexture
    {
        internal const int MaxDimension = 1024;
        private static bool _uploadFailed;
        private static long _keyMaskRetryAfter;
        private static bool _keyMaskFailureLogged;

        internal static bool CanBake(int width, int height)
            => !_uploadFailed && width > 0 && width <= MaxDimension
                && height > 0 && height <= MaxDimension;

        internal static bool CanBakeKeyMask => Environment.TickCount64 >= _keyMaskRetryAfter;

        internal static Color32 Pixel(Color color) => new Color32
        {
            r = ToByte(color.r),
            g = ToByte(color.g),
            b = ToByte(color.b),
            a = ToByte(color.a),
        };

        private static byte ToByte(float value) => (byte)(Math.Clamp(value, 0f, 1f) * 255f);

        internal static Texture2D Upload(int width, int height, Color32[] pixels, bool keyMask = false)
        {
            Texture2D texture = null;
            try
            {
                texture = CreateTexture(width, height);
                if (keyMask) KeepKeyMaskAlive(texture);
                UploadPixels(texture, pixels);
                return texture;
            }
            catch (Exception error)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                if (keyMask)
                    DeferKeyMasks(error);
                else
                {
                    _uploadFailed = true;
                    Plugin.Logger.LogWarning($"TwilightInputOverlay: texture upload unavailable; using rectangular rendering: {error}");
                }
                return null;
            }
        }

        internal static void DeferKeyMasks(Exception error)
        {
            _keyMaskRetryAfter = Environment.TickCount64 + 5000;
            if (_keyMaskFailureLogged) return;
            _keyMaskFailureLogged = true;
            Plugin.Logger.LogWarning($"TwilightInputOverlay: key mask unavailable; temporarily using rectangular keys: {error}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void KeepKeyMaskAlive(Texture2D texture)
            => texture.hideFlags = HideFlags.DontUnloadUnusedAsset;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Texture2D CreateTexture(int width, int height)
            => new Texture2D(width, height, TextureFormat.ARGB32, false);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UploadPixels(Texture2D texture, Color32[] pixels)
        {
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(new Il2CppStructArray<Color32>(pixels), 0);
            texture.Apply(false, true);
        }
    }
}
