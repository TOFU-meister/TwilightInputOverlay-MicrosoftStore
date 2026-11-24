using System.Collections.Generic;
using UnityEngine;

namespace TwilightInputOverlay
{
    /// <summary>
    /// Builds the cursor + trail silhouette as a single white mask texture, so
    /// the two parts are one shape with one shared alpha. The cursor is a circle
    /// of <c>radius</c>; the trail is the circle's rear half stretched into a
    /// half-ellipse whose rear extent is <c>rear</c> (in pixels). When
    /// <c>rear == radius</c> the ellipse coincides with the rear half of the
    /// circle and the whole silhouette is exactly a circle.
    /// <para>
    /// Because the coverage is the union of both shapes baked into one texture,
    /// overlapping areas never blend twice: a semi-transparent cursor looks like
    /// one body instead of showing a seam where the trail overlaps the circle.
    /// Textures are cached by (<c>radius</c>, <c>rear</c>) and tinted at draw
    /// time via <see cref="GUI.color"/>, so only geometry is baked.
    /// </para>
    /// </summary>
    internal static class CursorShapeTextureCache
    {
        /// <summary>Transparent padding around the shape, in pixels, for anti-aliasing.</summary>
        public const int Pad = 2;

        internal const int MaxRadius = (OverlayTexture.MaxDimension - Pad * 2) / 2;
        internal const int MaxEntries = 32;
        internal const int MaxPixels = 2 * 1024 * 1024;

        private readonly record struct ShapeKey(int Radius, int Rear);
        internal readonly record struct Shape(Texture2D Texture, int Width, int Height, int CenterX, int CenterY);
        private readonly record struct Entry(ShapeKey Key, Shape Shape);
        private static readonly Dictionary<ShapeKey, LinkedListNode<Entry>> Cache = new();
        private static readonly LinkedList<Entry> Order = new();

        internal static int Count => Cache.Count;
        internal static int Pixels { get; private set; }
        internal static int Bakes { get; private set; }
        internal static int Evictions { get; private set; }
        internal static int Invalidated { get; private set; }

        internal static int LimitRear(int radius, int rear)
            => System.Math.Clamp(rear, radius, OverlayTexture.MaxDimension - radius - Pad * 2);

        internal static Shape Get(int radius, int rear, TextureRecovery recovery)
        {
            if (!recovery.CanAttempt) return default;
            radius = System.Math.Clamp(radius, 1, MaxRadius);
            rear = LimitRear(radius, rear);
            var key = new ShapeKey(radius, rear);
            if (Cache.TryGetValue(key, out var node))
            {
                if (node.Value.Shape.Texture != null)
                {
                    Order.Remove(node);
                    Order.AddLast(node);
                    return node.Value.Shape;
                }
                Invalidated++;
                Remove(node);
            }

            int width = rear + radius + Pad * 2;
            int height = radius * 2 + Pad * 2;
            var tex = Build(radius, rear, width, height, recovery);
            if (tex == null) return default;

            while (Order.First is { } oldest && (Count >= MaxEntries || Pixels + width * height > MaxPixels))
            {
                Remove(oldest);
                Evictions++;
            }
            var shape = new Shape(tex, width, height, Pad + rear, Pad + radius);
            Cache.Add(key, Order.AddLast(new Entry(key, shape)));
            Pixels += width * height;
            Bakes++;
            return shape;
        }

        internal static void Invalidate(int radius, int rear)
        {
            if (Cache.TryGetValue(new ShapeKey(radius, rear), out var node))
            {
                Invalidated++;
                Remove(node);
            }
        }

        internal static void Release()
        {
            while (Order.First is { } oldest) Remove(oldest);
        }

        private static void Remove(LinkedListNode<Entry> node)
        {
            var entry = node.Value;
            Cache.Remove(entry.Key);
            Order.Remove(node);
            Pixels -= entry.Shape.Width * entry.Shape.Height;
            if (entry.Shape.Texture != null) Object.Destroy(entry.Shape.Texture);
        }

        private static Texture2D Build(int radius, int rear, int width, int height, TextureRecovery recovery)
        {
            // The circle centre sits `rear` from the left edge so the rear
            // half-ellipse (which reaches dx = -rear) always fits in the texture.
            float cx = Pad + rear;
            float cy = Pad + radius;
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;

                    // Front: the full circle.
                    float circle = CircleCoverage(dx, dy, radius);

                    // Rear: the half-ellipse, only behind the circle centre.
                    float trail = 0f;
                    if (dx <= 0f)
                        trail = EllipseCoverage(dx, dy, rear, radius);

                    // Union coverage, so the two parts share a single alpha.
                    float coverage = Mathf.Clamp01(Mathf.Max(circle, trail));
                    pixels[y * width + x] = OverlayTexture.Pixel(new Color(1f, 1f, 1f, coverage));
                }
            }

            return OverlayTexture.Upload(width, height, pixels, recovery);
        }

        private static float CircleCoverage(float dx, float dy, float radius)
        {
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - dist + 0.5f);
        }

        private static float EllipseCoverage(float dx, float dy, float semiX, float semiY)
        {
            float nx = dx / semiX;
            float ny = dy / semiY;
            float t = Mathf.Sqrt(nx * nx + ny * ny);
            // Approximate signed-distance anti-aliasing along the smaller axis.
            return Mathf.Clamp01((1f - t) * Mathf.Min(semiX, semiY) + 0.5f);
        }
    }
}
