using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwilightInputOverlay
{
    internal static class RainBarTextureCache
    {
        // Keep recently used heights for the next press and share identical
        // masks across slots. Tint and screen location do not affect the mask.
        private const int Capacity = 96;
        private const int MaxCachedPixels = 2 * 1024 * 1024;
        private const int RadiusStepsPerPixel = 8;
        private readonly record struct CacheKey(int Width, int Height, int Radius);
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

        internal readonly record struct MaskInfo(int Width, int Height, float Radius);

        internal static Texture2D Get(float width, float height, float requestedRadius)
        {
            string stage = null;
            return Get(width, height, requestedRadius, ref stage, out _);
        }

        internal static Texture2D Get(float width, float height, float requestedRadius, ref string stage, out MaskInfo info)
        {
            info = default;
            stage = "cache geometry";
            if (!float.IsFinite(width) || !float.IsFinite(height) || !float.IsFinite(requestedRadius)
                || width <= 0f || height <= 0f) return null;
            float radius = RainBarGeometry.Radius(width, height, requestedRadius);
            if (radius <= 0f) return null;

            int shapeWidth = Math.Max(1, Mathf.RoundToInt(width));
            int shapeHeight = Math.Max(1, Mathf.RoundToInt(height));
            int pixelWidth = Math.Min(shapeWidth, OverlayTexture.MaxDimension);
            int pixelHeight = Math.Min(shapeHeight, OverlayTexture.MaxDimension);
            if (!OverlayTexture.Rain.CanAttempt || !OverlayTexture.CanBake(pixelWidth, pixelHeight)) return null;

            int radiusSteps = Mathf.RoundToInt(radius * RadiusStepsPerPixel);
            if (radiusSteps == 0) return null;
            float maskRadius = RainBarGeometry.Radius(shapeWidth, shapeHeight,
                radiusSteps / (float)RadiusStepsPerPixel);
            info = new MaskInfo(pixelWidth, pixelHeight, maskRadius);
            stage = "cache lookup / Unity validity";
            var key = new CacheKey(shapeWidth, shapeHeight, radiusSteps);
            bool replacing = Cache.TryGetValue(key, out var existing);
            if (replacing)
            {
                if (existing.Value.Texture != null)
                {
                    if (ConfigService.DebugModeEnabled) Hits++;
                    Order.Remove(existing);
                    Order.AddLast(existing);
                    return existing.Value.Texture;
                }

                if (ConfigService.DebugModeEnabled) InvalidHits++;
                Remove(existing);
            }

            if (ConfigService.DebugModeEnabled) Misses++;
            stage = "mask pixel generation";
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

            stage = "Texture2D creation / protection / upload";
            var texture = OverlayTexture.Upload(pixelWidth, pixelHeight, pixels, OverlayTexture.Rain, propagateFailure: true);
            if (texture == null) return null;
            if (ConfigService.DebugModeEnabled && replacing) Rebuilt++;
            if (ConfigService.DebugModeEnabled) Bakes++;

            stage = "cache eviction / insertion";
            while (Order.First is { } oldest
                && (Cache.Count >= Capacity || _cachedPixels + pixels.Length > MaxCachedPixels))
            {
                Remove(oldest);
            }

            var node = Order.AddLast(new CacheEntry(key, texture, pixels.Length));
            Cache.Add(key, node);
            _cachedPixels += pixels.Length;
            return texture;
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
                if (ConfigService.DebugModeEnabled) Destroyed++;
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
