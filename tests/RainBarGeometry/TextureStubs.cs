using System;

namespace UnityEngine
{
    public static class Mathf
    {
        public static int RoundToInt(float value) => (int)MathF.Round(value);
    }

    public struct Color32
    {
        public byte r, g, b, a;
    }

    public sealed class Texture2D
    {
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
    internal static class OverlayTexture
    {
        public const int MaxDimension = 1024;
        public static bool FailUpload { get; set; }
        public static bool CanBake(int width, int height) => width > 0 && height > 0;
        public static UnityEngine.Texture2D Upload(int width, int height, UnityEngine.Color32[] pixels)
            => FailUpload ? null : new UnityEngine.Texture2D();
    }
}
