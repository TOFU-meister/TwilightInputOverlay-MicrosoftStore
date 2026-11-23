using System;
using TwilightInputOverlay;

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

const float keyLeft = 112f;
const float keyWidth = 56f;
float[] radii = { 0f, 1f, 3f, 5f, 6.52f, 9f, 9.5f, 10f, 14.76f, 15f, 20f };
int[] heights = { 1, 2, 5, 19, 20, 21, 160 };

foreach (float gap in new[] { 0f, 20f })
{
    float bottom = RainBarGeometry.RegionBottom(700f, gap);
    Check(bottom == 700f - gap, $"Gap {gap} moves the region vertically");

    foreach (float fullWidth in new[] { keyWidth, 20f })
    {
        float middle = RainBarGeometry.Center(keyLeft, keyWidth, fullWidth, 0);
        float halfWidth = RainBarGeometry.HalfWidth(fullWidth);
        float left = RainBarGeometry.Center(keyLeft, keyWidth, halfWidth, -1);
        float right = RainBarGeometry.Center(keyLeft, keyWidth, halfWidth, 1);
        Check(middle == keyLeft + keyWidth / 2, "Full bar centered on key");
        Check(left + halfWidth / 2 == middle && right - halfWidth / 2 == middle,
            "Split bars meet at key center");
        Check(right + halfWidth / 2 - (left - halfWidth / 2) == fullWidth,
            "Split span equals configured full width");

        foreach (int width in new[] { (int)fullWidth, (int)halfWidth })
        foreach (int height in heights)
        {
            int previousCorner = 255;
            foreach (float requested in radii)
            {
                string context = $"width={width}, height={height}, radius={requested}, gap={gap}";
                float radius = RainBarGeometry.Radius(width, height, requested);
                Check(radius == Math.Min(requested, Math.Min(width, height) / 2f), context + " clamp");

                int corner = RainBarGeometry.Alpha(0.5f, 0.5f, width, height, radius);
                Check(corner <= previousCorner, context + " corner opacity monotonic");
                if (requested == 0f)
                    Check(corner == 255, context + " opaque rectangular mask");
                if (requested == 1f && height >= 2)
                    Check(corner > 0 && corner < 255, context + " subpixel corner coverage");
                previousCorner = corner;

                for (int y = 0; y < height; y++)
                {
                    Check(RainBarGeometry.Alpha(width / 2f, y + 0.5f, width, height, radius) == 255,
                        context + " solid center column");
                    for (int x = 0; x < width / 2; x++)
                    {
                        byte alpha = RainBarGeometry.Alpha(x + 0.5f, y + 0.5f, width, height, radius);
                        Check(alpha == RainBarGeometry.Alpha(width - x - 0.5f, y + 0.5f, width, height, radius)
                            && alpha == RainBarGeometry.Alpha(x + 0.5f, height - y - 0.5f, width, height, radius),
                            context + " horizontal and vertical UV symmetry");
                        if (height >= 2 && y > 0 && y < height / 2)
                            Check(alpha >= RainBarGeometry.Alpha(x + 0.5f, y - 0.5f, width, height, radius),
                                context + " no interior alpha dip toward center");
                    }
                }
            }
        }
    }
}

foreach (int width in new[] { 56, 28 })
{
    float a = RainBarGeometry.Radius(width, 160f, 9f);
    float b = RainBarGeometry.Radius(width, 160f, 9.5f);
    float c = RainBarGeometry.Radius(width, 160f, 10f);
    Check(a < b && b < c, "Fractional radius retained across the reported threshold");
    Check(RainBarGeometry.Radius(width, 19.5f, 20f) == 9.75f, "Fractional short-bar clamp");
}

Console.WriteLine("Passed 616 rain mask cases for full and split bars, including both gaps.");

RainBarTextureCache.Release();
RainBarTextureCache.Release();
Check(RainBarTextureCache.Count == 0, "release empty cache");

var first = RainBarTextureCache.Get(28f, 100f, 9.5f);
Check(ReferenceEquals(first, RainBarTextureCache.Get(28f, 100f, 9.5f)), "reuse live mask");
UnityEngine.Object.Destroy(first);
OverlayTexture.FailUpload = true;
Check(RainBarTextureCache.Get(28f, 100f, 9.5f) == null, "dead hit and failed upload fall back");
Check(RainBarTextureCache.Count == 0, "dead hit removed from cache");
OverlayTexture.FailUpload = false;
var rebuilt = RainBarTextureCache.Get(28f, 100f, 9.5f);
Check(rebuilt != null && !ReferenceEquals(first, rebuilt), "dead hit can be rebuilt");

for (int round = 0; round < 20; round++)
{
    for (int height = 2; height <= 180; height++)
    {
        var texture = RainBarTextureCache.Get(28f + round, height, 9.5f);
        Check(texture != null, "repeat fill and eviction");
        Check(ReferenceEquals(texture, RainBarTextureCache.Get(28f + round, height, 9.5f)),
            "repeat reuse");
    }
    Check(RainBarTextureCache.Count <= 96, "entry limit");
}
Check(rebuilt.Destroyed, "old mask evicted without native size getters");
RainBarTextureCache.Release();

var large = RainBarTextureCache.Get(1024f, 1024f, 12f);
RainBarTextureCache.Get(1024f, 1024f, 13f);
UnityEngine.Object.Destroy(large);
RainBarTextureCache.Get(1024f, 1024f, 14f);
Check(RainBarTextureCache.Count == 2, "pixel limit evicts already-destroyed texture");
RainBarTextureCache.Release();
Check(RainBarTextureCache.Count == 0, "release populated cache");
Check(RainBarTextureCache.Get(32f, 64f, 0f) == null, "square bars bypass cache");
Console.WriteLine("Rain cache checks passed (3,580 insert/reuse pairs, invalid textures, empty release and both eviction limits).");
