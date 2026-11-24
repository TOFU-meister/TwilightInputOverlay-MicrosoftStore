using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace TwilightInputOverlay
{
    internal static class OverlayTexture
    {
        internal const int MaxDimension = 1024;
        internal static readonly TextureRecovery Keys = new("key masks");
        internal static readonly TextureRecovery Rain = new("Raining Keys masks");
        internal static readonly TextureRecovery CursorTrail = new("mouse trail masks");
        internal static readonly TextureRecovery CursorCircle = new("mouse circle masks");
        internal static readonly TextureRecovery CursorRectangle = new("mouse rectangles");

        internal static bool CanBake(int width, int height)
            => width > 0 && width <= MaxDimension && height > 0 && height <= MaxDimension;

        internal static Color32 Pixel(Color color) => new Color32
        {
            r = ToByte(color.r),
            g = ToByte(color.g),
            b = ToByte(color.b),
            a = ToByte(color.a),
        };

        private static byte ToByte(float value) => (byte)(Math.Clamp(value, 0f, 1f) * 255f);

        internal static Texture2D Upload(int width, int height, Color32[] pixels, TextureRecovery recovery, bool propagateFailure = false)
        {
            Texture2D texture = null;
            try
            {
                texture = CreateTexture(width, height);
                KeepMaskAlive(texture);
                UploadPixels(texture, pixels);
                return texture;
            }
            catch (Exception error)
            {
                if (propagateFailure)
                {
                    try { if (texture != null) UnityEngine.Object.Destroy(texture); }
                    catch (Exception cleanupError) { throw new AggregateException(error, cleanupError); }
                    throw;
                }
                if (texture != null) UnityEngine.Object.Destroy(texture);
                recovery.Fail(error);
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void KeepMaskAlive(Texture2D texture)
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
