using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwilightInputOverlay
{
    internal static class RainBarTextureCache
    {
        // The middle of the mask stretches independently of its end caps.
        // Only the configured width and width-limited radius affect the asset.
        private const int Capacity = 96;
        private const int MaxCachedPixels = 2 * 1024 * 1024;
        private const int RadiusStepsPerPixel = 8;
        private readonly record struct CacheKey(int Width, int Radius);
        private readonly record struct CacheEntry(CacheKey Key, Texture2D Texture, int PixelCount);

        private static readonly Dictionary<CacheKey, LinkedListNode<CacheEntry>> Cache = new();
        private static readonly LinkedList<CacheEntry> Order = new();

        private static int _cachedPixels;

        internal static int Bakes { get; private set; }
        internal static int Count => Cache.Count;

        internal static int Pixels => _cachedPixels;
        internal static long Rebuilt { get; private set; }
        internal static long Destroyed { get; private set; }
        internal static long InvalidHits { get; private set; }
        internal static long Hits { get; private set; }
        internal static long Misses { get; private set; }

        internal readonly record struct Mask(Texture2D Texture, int ShapeWidth, int ShapeHeight, float Radius);

        internal static Mask Get(float width, float requestedRadius)
        {
            if (!float.IsFinite(width) || !float.IsFinite(requestedRadius) || width <= 0f || requestedRadius <= 0f
                || !OverlayTexture.Rain.CanAttempt) return default;

            int shapeWidth = Math.Max(1, Mathf.RoundToInt(Math.Min(width, 16f * OverlayTexture.MaxDimension)));
            int radiusSteps = Mathf.RoundToInt(Math.Min(requestedRadius, shapeWidth * 0.5f) * RadiusStepsPerPixel);
            if (radiusSteps == 0) return default;
            float maskRadius = Math.Min(shapeWidth * 0.5f, radiusSteps / (float)RadiusStepsPerPixel);
            // Include an opaque centre row separated from both curved caps.
            int shapeHeight = 2 * (int)MathF.Ceiling(maskRadius) + 1;
            int pixelHeight = Math.Min(OverlayTexture.MaxDimension, shapeHeight);
            int pixelWidth = Math.Min(shapeWidth, OverlayTexture.MaxDimension);
            if (!OverlayTexture.CanBake(pixelWidth, pixelHeight)) return default;

            var key = new CacheKey(shapeWidth, radiusSteps);
            bool replacing = Cache.TryGetValue(key, out var existing);
            if (replacing)
            {
                if (existing.Value.Texture != null)
                {
                    Hits++;
                    Order.Remove(existing);
                    Order.AddLast(existing);
                    return new Mask(existing.Value.Texture, shapeWidth, shapeHeight, maskRadius);
                }

                InvalidHits++;
                Remove(existing);
            }

            Misses++;
            float sampleWidth = (float)shapeWidth / pixelWidth;
            float sampleHeight = (float)shapeHeight / pixelHeight;
            var pixels = new Color32[pixelWidth * pixelHeight];
            for (int y = 0; y < pixelHeight; y++)
            {
                for (int x = 0; x < pixelWidth; x++)
                {
                    pixels[y * pixelWidth + x] = new Color32
                    {
                        r = 255,
                        g = 255,
                        b = 255,
                        a = RainBarGeometry.Alpha((x + 0.5f) * sampleWidth, (y + 0.5f) * sampleHeight,
                            shapeWidth, shapeHeight, maskRadius),
                    };
                }
            }

            var texture = OverlayTexture.Upload(pixelWidth, pixelHeight, pixels, OverlayTexture.Rain);
            if (texture == null) return default;
            if (replacing) Rebuilt++;
            Bakes++;

            while (Order.First is { } oldest
                && (Cache.Count >= Capacity || _cachedPixels + pixels.Length > MaxCachedPixels))
            {
                Remove(oldest);
            }

            var node = Order.AddLast(new CacheEntry(key, texture, pixels.Length));
            Cache.Add(key, node);
            _cachedPixels += pixels.Length;
            return new Mask(texture, shapeWidth, shapeHeight, maskRadius);
        }

        private static void Remove(LinkedListNode<CacheEntry> node)
        {
            var entry = node.Value;
            // Update bookkeeping before releasing the Unity object. The native
            // texture can already be gone, and its size getters are not needed.
            Cache.Remove(entry.Key);
            Order.Remove(node);
            _cachedPixels -= entry.PixelCount;
            if (entry.Texture != null)
            {
                Destroyed++;
                UnityEngine.Object.Destroy(entry.Texture);
            }
        }

        internal static void Release()
        {
            while (Order.First is { } oldest)
                Remove(oldest);
        }
    }
}
