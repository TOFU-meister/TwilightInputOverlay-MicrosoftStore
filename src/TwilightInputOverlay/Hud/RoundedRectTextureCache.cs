using System.Collections.Generic;
using UnityEngine;

namespace TwilightInputOverlay
{
    // Cache geometry only. The HUD tints each mask, including the two halves
    // of a mouse key, independently of press and fade state.
    internal static class RoundedRectTextureCache
    {
        private const int Capacity = 96;
        private const int MaxCachedPixels = 2 * 1024 * 1024;

        internal enum Mask { Full, Left, Right, Border }

        private readonly record struct Key(int Width, int Height, int Radius, Mask Part, int BorderWidth);
        private readonly record struct Entry(Key Key, Texture2D Texture, int Pixels);

        private static readonly Dictionary<Key, LinkedListNode<Entry>> Cache = new();
        private static readonly LinkedList<Entry> Order = new();
        private static int _pixels;

        public static int FillCalls { get; private set; }
        public static int FillBakes { get; private set; }
        internal static int FillCacheCount => Cache.Count;

        internal static Texture2D GetMask(int width, int height, int radius, Mask part = Mask.Full, int borderWidth = 0)
        {
            if (width < 1 || height < 1 || width > OverlayTexture.MaxDimension || height > OverlayTexture.MaxDimension)
                return null;

            radius = Mathf.Min(radius, Mathf.Min(width, height) / 2);
            if (radius <= 0) return null;

            bool border = part == Mask.Border;
            if (!border) FillCalls++;
            var key = new Key(width, height, radius, part, borderWidth);
            if (Cache.TryGetValue(key, out var node))
            {
                if (node.Value.Texture != null)
                {
                    Order.Remove(node);
                    Order.AddLast(node);
                    return node.Value.Texture;
                }
                Remove(node);
            }

            int pixelWidth = part == Mask.Left || part == Mask.Right ? (width + 1) / 2 : width;
            if (!OverlayTexture.Keys.CanAttempt) return null;
            var texture = border
                ? BuildBorderMask(width, height, radius, borderWidth)
                : BuildFillMask(width, height, radius, part, pixelWidth);
            if (texture == null) return null;
            if (!border) FillBakes++;

            int pixels = pixelWidth * height;
            while (Order.First is { } oldest && (Cache.Count >= Capacity || _pixels + pixels > MaxCachedPixels))
                Remove(oldest);

            Cache.Add(key, Order.AddLast(new Entry(key, texture, pixels)));
            _pixels += pixels;
            return texture;
        }

        internal static void ReleaseKeyMasks()
        {
            while (Order.First is { } oldest)
                Remove(oldest);
        }

        private static void Remove(LinkedListNode<Entry> node)
        {
            var entry = node.Value;
            Cache.Remove(entry.Key);
            Order.Remove(node);
            _pixels -= entry.Pixels;
            if (entry.Texture != null)
                UnityEngine.Object.Destroy(entry.Texture);
        }

        private static Texture2D BuildFillMask(int width, int height, int radius, Mask part, int pixelWidth)
        {
            // Sample both halves in the full key's coordinate space so they
            // meet exactly at the centre, including for odd pixel widths.
            float span = part == Mask.Full ? width : width * 0.5f;
            float origin = part == Mask.Right ? span : 0f;
            float step = span / pixelWidth;
            var pixels = new Color32[pixelWidth * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < pixelWidth; x++)
                {
                    ShapeCoverage(origin + (x + 0.5f) * step, y + 0.5f, width, height, radius, out float coverage);
                    pixels[y * pixelWidth + x] = OverlayTexture.Pixel(new Color(1f, 1f, 1f, coverage));
                }
            }

            return OverlayTexture.Upload(pixelWidth, height, pixels, OverlayTexture.Keys);
        }

        private static Texture2D BuildBorderMask(int width, int height, int radius, int borderWidth)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float alpha = 0f;

                    float coverage;
                    bool inside = ShapeCoverage(px, py, width, height, radius, out coverage);
                    if (inside && borderWidth > 0)
                    {
                        float edgeDist = EdgeDistance(px, py, width, height, radius);
                        if (edgeDist <= borderWidth)
                        {
                            alpha = radius <= 0f
                                ? 1f
                                : Mathf.Clamp01(borderWidth - edgeDist + 0.5f);
                        }
                    }
                    pixels[y * width + x] = OverlayTexture.Pixel(new Color(1f, 1f, 1f, alpha));
                }
            }

            return OverlayTexture.Upload(width, height, pixels, OverlayTexture.Keys);
        }

        private static bool ShapeCoverage(float px, float py, int width, int height, int radius, out float coverage)
        {
            if (radius <= 0)
            {
                // Pixel centers are always inside the rectangle.
                coverage = 1f;
                return true;
            }

            float r = radius;
            float nx = Mathf.Clamp(px, r, width - r);
            float ny = Mathf.Clamp(py, r, height - r);
            float dist = Vector2.Distance(new Vector2(px, py), new Vector2(nx, ny));
            if (dist > r)
            {
                coverage = 0f;
                return false;
            }

            coverage = Mathf.Clamp01((r - dist) + 0.5f);
            return true;
        }

        private static float EdgeDistance(float px, float py, int width, int height, int radius)
        {
            if (radius <= 0)
            {
                return Mathf.Min(Mathf.Min(px, width - px), Mathf.Min(py, height - py));
            }

            float r = radius;
            float nx = Mathf.Clamp(px, r, width - r);
            float ny = Mathf.Clamp(py, r, height - r);
            float dist = Vector2.Distance(new Vector2(px, py), new Vector2(nx, ny));
            return r - dist;
        }

    }
}
