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
        private static readonly Dictionary<(int Width, int Height, int Radius), Texture2D> Cache = new();
        private static readonly Queue<(int Width, int Height, int Radius)> Order = new();

        private static int _cachedPixels;

        internal static int Bakes { get; private set; }
        internal static int Count => Cache.Count;

        internal static Texture2D Get(float width, float height, float requestedRadius)
        {
            float radius = RainBarGeometry.Radius(width, height, requestedRadius);
            if (radius <= 0f) return null;

            int shapeWidth = Math.Max(1, Mathf.RoundToInt(width));
            int shapeHeight = Math.Max(1, Mathf.RoundToInt(height));
            int pixelWidth = Math.Min(shapeWidth, OverlayTexture.MaxDimension);
            int pixelHeight = Math.Min(shapeHeight, OverlayTexture.MaxDimension);
            if (!OverlayTexture.CanBake(pixelWidth, pixelHeight)) return null;

            int radiusSteps = Mathf.RoundToInt(radius * RadiusStepsPerPixel);
            if (radiusSteps == 0) return null;
            var key = (shapeWidth, shapeHeight, radiusSteps);
            if (Cache.TryGetValue(key, out var texture)) return texture;

            float maskRadius = RainBarGeometry.Radius(shapeWidth, shapeHeight,
                radiusSteps / (float)RadiusStepsPerPixel);
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

            texture = OverlayTexture.Upload(pixelWidth, pixelHeight, pixels);
            if (texture == null) return null;
            Bakes++;

            while (Cache.Count == Capacity || _cachedPixels + pixels.Length > MaxCachedPixels)
            {
                var oldest = Order.Dequeue();
                var evicted = Cache[oldest];
                _cachedPixels -= evicted.width * evicted.height;
                UnityEngine.Object.Destroy(evicted);
                Cache.Remove(oldest);
            }
            Cache.Add(key, texture);
            Order.Enqueue(key);
            _cachedPixels += pixels.Length;
            return texture;
        }

        internal static void Release()
        {
            foreach (var texture in Cache.Values)
                UnityEngine.Object.Destroy(texture);
            Cache.Clear();
            Order.Clear();
            _cachedPixels = 0;
        }
    }
}
