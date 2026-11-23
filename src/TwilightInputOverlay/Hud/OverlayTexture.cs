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

        internal static bool CanBake(int width, int height)
            => !_uploadFailed && width > 0 && width <= MaxDimension
                && height > 0 && height <= MaxDimension;

        internal static Color32 Pixel(Color color) => new Color32
        {
            r = ToByte(color.r),
            g = ToByte(color.g),
            b = ToByte(color.b),
            a = ToByte(color.a),
        };

        private static byte ToByte(float value) => (byte)(Math.Clamp(value, 0f, 1f) * 255f);

        internal static Texture2D Upload(int width, int height, Color32[] pixels)
        {
            Texture2D texture = null;
            try
            {
                texture = CreateTexture(width, height);
                UploadPixels(texture, pixels);
                return texture;
            }
            catch (Exception error)
            {
                _uploadFailed = true;
                if (texture != null) UnityEngine.Object.Destroy(texture);
                Plugin.Logger.LogWarning($"TwilightInputOverlay: texture upload unavailable; using rectangular rendering: {error}");
                return null;
            }
        }

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
