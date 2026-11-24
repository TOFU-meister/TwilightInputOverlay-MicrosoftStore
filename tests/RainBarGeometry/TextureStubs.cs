using System;

namespace UnityEngine
{
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Sqrt(float value) => MathF.Sqrt(value);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
        public static int RoundToInt(float value) => (int)MathF.Round(value);
    }

    public readonly record struct Color(float R, float G, float B, float A);
    public readonly record struct Vector2(float X, float Y)
    {
        public static float Distance(Vector2 a, Vector2 b)
            => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    public struct Color32
    {
        public byte r, g, b, a;
    }

    public sealed class Texture2D
    {
        public Color32[] Pixels { get; init; }
        public bool Destroyed { get; set; }
        // These Unity native properties failed on the Windows target.
        public int width => throw new NullReferenceException("Texture.get_width");
        public int height => throw new NullReferenceException("Texture.get_height");

        private static bool Missing(Texture2D texture) => texture is null || texture.Destroyed;
        public static bool operator ==(Texture2D left, Texture2D right)
            => Missing(left) && Missing(right) || ReferenceEquals(left, right);
        public static bool operator !=(Texture2D left, Texture2D right) => !(left == right);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => base.GetHashCode();
    }

    public static class Object
    {
        public static void Destroy(Texture2D texture)
        {
            if (texture.Destroyed) throw new InvalidOperationException("Texture already destroyed");
            texture.Destroyed = true;
        }
    }
}

namespace TwilightInputOverlay
{
    internal static class Plugin
    {
        internal static readonly TestLogger Logger = new();
        internal sealed class TestLogger { public void LogWarning(string message) { } }
    }

    internal static class OverlayTexture
    {
        public const int MaxDimension = 1024;
        public static bool FailUpload { get; set; }
        public static bool CanBake(int width, int height) => width > 0 && height > 0;
        public static readonly TextureRecovery Keys = new("Keys");
        public static readonly TextureRecovery Rain = new("Rain");
        public static UnityEngine.Color32 Pixel(UnityEngine.Color color) => new()
        {
            r = (byte)(color.R * 255f), g = (byte)(color.G * 255f),
            b = (byte)(color.B * 255f), a = (byte)(color.A * 255f),
        };

        public static UnityEngine.Texture2D Upload(int width, int height, UnityEngine.Color32[] pixels, TextureRecovery recovery)
            => FailUpload ? null : new UnityEngine.Texture2D { Pixels = pixels };
    }
}
