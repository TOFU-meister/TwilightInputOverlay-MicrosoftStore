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

var first = RainBarTextureCache.Get(28f, 9.5f).Texture;
Check(ReferenceEquals(first, RainBarTextureCache.Get(28f, 9.5f).Texture), "reuse live mask");
UnityEngine.Object.Destroy(first);
OverlayTexture.FailUpload = true;
Check(RainBarTextureCache.Get(28f, 9.5f).Texture == null, "dead hit and failed upload fall back");
Check(RainBarTextureCache.Count == 0, "dead hit removed from cache");
OverlayTexture.FailUpload = false;
var rebuilt = RainBarTextureCache.Get(28f, 9.5f).Texture;
Check(rebuilt != null && !ReferenceEquals(first, rebuilt), "dead hit can be rebuilt");

RainBarTextureCache.Release();
Span<RainBarGeometry.Slice> slices = stackalloc RainBarGeometry.Slice[9];
int rainWarmup = RainBarTextureCache.Bakes;
foreach (float width in new[] { 28f, 56f })
    RainBarTextureCache.Get(width, 9.5f);
for (int cycle = 0; cycle < 12; cycle++)
{
    for (int step = 1; step <= 640; step++)
    {
        float height = (step <= 320 ? step : 641 - step) * 0.5f;
        foreach (float width in new[] { 28f, 56f })
        {
            var mask = RainBarTextureCache.Get(width, 9.5f);
            int count = RainBarGeometry.Slices(width, height, 9.5f, mask.Radius, mask.ShapeWidth, mask.ShapeHeight, slices);
            Check(count == (height < 19f ? 6 : height == 19f ? 2 : 3), "short and tall slice count");
            double area = 0;
            for (int i = 0; i < count; i++)
            {
                var slice = slices[i];
                area += slice.Width * slice.Height;
                Check(slice.Width > 0 && slice.Height > 0 && slice.UWidth > 0 && slice.VHeight > 0, "nonempty slice");
                Check(slice.X >= 0 && slice.Y >= 0 && slice.U >= 0 && slice.V >= 0
                    && slice.U + slice.UWidth <= 1.000001f && slice.V + slice.VHeight <= 1.000001f, "valid UVs");
                for (int j = 0; j < i; j++)
                {
                    var other = slices[j];
                    float overlapX = Math.Min(slice.X + slice.Width, other.X + other.Width) - Math.Max(slice.X, other.X);
                    float overlapY = Math.Min(slice.Y + slice.Height, other.Y + other.Height) - Math.Max(slice.Y, other.Y);
                    Check(overlapX < 0.00001f || overlapY < 0.00001f, "translucent slices do not overlap");
                }
            }
            Check(Math.Abs(area - width * height) < 0.01, "slices cover full bar without gaps");
        }
    }
}
Check(RainBarTextureCache.Bakes - rainWarmup == 2 && RainBarTextureCache.Count == 2, "no bakes after two-width warmup");
Console.WriteLine($"Rain replay: 15,360 lookups, 2 bakes, {RainBarTextureCache.Pixels} retained pixels.");

var extremeRain = RainBarTextureCache.Get(28f, 500f);
Check(RainBarGeometry.Slices(28f, 0.05f, 500f, extremeRain.Radius, extremeRain.ShapeWidth, extremeRain.ShapeHeight, slices) == 6
    && slices[0].Height == 0.025f && slices[2].UWidth > 0f, "width-clamped atlas handles subpixel short bars");
Check(RainBarTextureCache.Get(32f, 0f).Texture == null, "square bars bypass cache");
Check(RainBarGeometry.Slices(32f, 8f, 0f, 1f, 32, 3, slices) == 0, "no rounded parts for zero radius");

var oldest = RainBarTextureCache.Get(20f, 3f).Texture;
for (int width = 30; width < 140; width++)
    RainBarTextureCache.Get(width, 3f);
Check(oldest.Destroyed && RainBarTextureCache.Count == 96, "entry cap evicts old atlas");
RainBarTextureCache.Release();
var large = RainBarTextureCache.Get(1024f, 512f).Texture;
RainBarTextureCache.Get(1024f, 511f);
RainBarTextureCache.Get(1024f, 510f);
Check(large.Destroyed && RainBarTextureCache.Pixels <= 2 * 1024 * 1024, "pixel budget evicts old atlas");
RainBarTextureCache.Release();
Check(RainBarTextureCache.Count == 0 && RainBarTextureCache.Pixels == 0, "release populated cache");
Console.WriteLine("Rain cache checks passed (replay, slice geometry, invalid textures and both limits).");

Check(RoundedRectTextureCache.GetMask(57, 56, 0) == null, "square keys bypass rounded masks");
Check(RoundedRectTextureCache.GetMask(57, 56, 1).Pixels[0].a < 255, "small positive radius has corner coverage");
var full = RoundedRectTextureCache.GetMask(57, 56, 10);
var leftMask = RoundedRectTextureCache.GetMask(57, 56, 10, RoundedRectTextureCache.Mask.Left);
var rightMask = RoundedRectTextureCache.GetMask(57, 56, 10, RoundedRectTextureCache.Mask.Right);
Check(full.Pixels[0].a == 0 && leftMask.Pixels[0].a == 0 && rightMask.Pixels[28].a == 0,
    "full and split masks round the outside corners");
for (int y = 0; y < 56; y++)
{
    Check(leftMask.Pixels[y * 29 + 28].a == 255 && rightMask.Pixels[y * 29].a == 255,
        "odd-width split has no gap at its centre");
    for (int x = 0; x < 29; x++)
        Check(leftMask.Pixels[y * 29 + x].a == rightMask.Pixels[y * 29 + 28 - x].a,
            "left and right masks are mirrored");
}

UnityEngine.Object.Destroy(full);
OverlayTexture.FailUpload = true;
Check(RoundedRectTextureCache.GetMask(57, 56, 10) == null, "missing key texture and failed upload fall back");
OverlayTexture.FailUpload = false;
var recovered = RoundedRectTextureCache.GetMask(57, 56, 10);
Check(recovered != null && !ReferenceEquals(full, recovered), "missing key texture rebuilt");
int keyBakes = RoundedRectTextureCache.FillBakes;
for (int i = 0; i < 2000; i++)
{
    Check(ReferenceEquals(leftMask, RoundedRectTextureCache.GetMask(57, 56, 10, RoundedRectTextureCache.Mask.Left)), "left half reuses geometry");
    Check(ReferenceEquals(rightMask, RoundedRectTextureCache.GetMask(57, 56, 10, RoundedRectTextureCache.Mask.Right)), "right half reuses geometry");
}
Check(RoundedRectTextureCache.FillBakes == keyBakes, "same shapes incur no rebakes");

for (int width = 60; width < 220; width++)
{
    Check(RoundedRectTextureCache.GetMask(width, 56, 10) != null, "new geometry draws under eviction");
    Check(ReferenceEquals(leftMask, RoundedRectTextureCache.GetMask(57, 56, 10, RoundedRectTextureCache.Mask.Left)),
        "recently used half survives eviction");
}
Check(recovered.Destroyed && RoundedRectTextureCache.FillCacheCount == 96, "entry limit evicts old geometry");
Check(RoundedRectTextureCache.GetMask(57, 56, 10) != null, "evicted shape can be requested again");
RoundedRectTextureCache.ReleaseKeyMasks();
var largeKey = RoundedRectTextureCache.GetMask(1024, 1024, 12);
RoundedRectTextureCache.GetMask(1024, 1024, 13);
RoundedRectTextureCache.GetMask(1024, 1024, 14);
Check(largeKey.Destroyed && RoundedRectTextureCache.FillCacheCount == 2, "pixel budget evicts excess masks");
RoundedRectTextureCache.ReleaseKeyMasks();
RoundedRectTextureCache.ReleaseKeyMasks();
Check(RoundedRectTextureCache.FillCacheCount == 0 && leftMask.Destroyed, "teardown releases masks");
Console.WriteLine("Key mask checks passed (split geometry, live reuse, invalid textures and both eviction limits).");

long now = 0;
int warnings = 0;
var trailRecovery = new TextureRecovery("Trail", () => now, _ => warnings++);
var circleRecovery = new TextureRecovery("Circle", () => now, _ => warnings++);
var trail = CursorShapeTextureCache.Get(12, 34, trailRecovery);
var circle = CursorShapeTextureCache.Get(12, 12, circleRecovery);
Check(trail.Width == 50 && trail.Height == 28 && trail.CenterX == 36 && trail.CenterY == 14,
    "padded trail dimensions available without native size getters");
int yCenter = trail.CenterY * trail.Width;
Check(trail.Texture.Pixels[yCenter + 10].a == 255 && trail.Texture.Pixels[yCenter + trail.CenterX].a == 255
    && trail.Texture.Pixels[yCenter].a == 0, "single trail/circle union has a solid core and padding");
Check(circle.Width == circle.Height, "no extension gives a circle");

for (int i = 0; i < 300; i++)
{
    CursorShapeTextureCache.Get(12, 40 + i, trailRecovery);
    Check(ReferenceEquals(trail.Texture, CursorShapeTextureCache.Get(12, 34, trailRecovery).Texture),
        "recently used shapes survive churn");
}
Check(CursorShapeTextureCache.Count == CursorShapeTextureCache.MaxEntries && circle.Texture.Destroyed,
    "least-recent shapes evicted individually");
UnityEngine.Object.Destroy(trail.Texture);
OverlayTexture.FailUpload = true;
Check(CursorShapeTextureCache.Get(12, 34, trailRecovery).Texture == null,
    "destroyed texture removed even if upload fails");
OverlayTexture.FailUpload = false;
var rebuiltCursor = CursorShapeTextureCache.Get(12, 34, trailRecovery);
Check(rebuiltCursor.Texture != null && !ReferenceEquals(trail.Texture, rebuiltCursor.Texture), "destroyed cursor replaced");
CursorShapeTextureCache.Invalidate(12, 34);
Check(rebuiltCursor.Texture.Destroyed, "failed draw can discard its own shape");

trailRecovery.Fail(new NullReferenceException("simulated native draw"));
Check(!trailRecovery.CanAttempt && circleRecovery.CanAttempt && OverlayTexture.Rain.CanAttempt && OverlayTexture.Keys.CanAttempt,
    "trail failure does not affect circle, rain or keys");
Check(CursorShapeTextureCache.Get(12, 34, trailRecovery).Texture == null
    && CursorShapeTextureCache.Get(12, 12, circleRecovery).Texture != null, "circle remains available on cooldown");
now += 4999;
Check(!trailRecovery.CanAttempt, "cooldown does not retry immediately");
now++;
Check(trailRecovery.CanAttempt && CursorShapeTextureCache.Get(12, 34, trailRecovery).Texture != null,
    "transient failure retries after five seconds");
trailRecovery.Fail(new MissingMethodException("simulated stripped upload"));
now += 10000;
Check(!trailRecovery.CanAttempt && trailRecovery.Unsupported && trailRecovery.Failures == 2 && warnings == 1,
    "permanent failure stops retry and does not spam warnings");

for (int i = 0; i < 6; i++)
{
    var extreme = CursorShapeTextureCache.Get(int.MaxValue - i - 16, int.MaxValue, circleRecovery);
    Check(extreme.Width == OverlayTexture.MaxDimension && extreme.Height == OverlayTexture.MaxDimension,
        "oversized inputs clamp before allocation");
    CursorShapeTextureCache.Get(CursorShapeTextureCache.MaxRadius - i - 1, int.MaxValue, circleRecovery);
    Check(CursorShapeTextureCache.Pixels <= CursorShapeTextureCache.MaxPixels, "cursor pixel limit");
}
CursorShapeTextureCache.Release();
CursorShapeTextureCache.Release();
Check(CursorShapeTextureCache.Count == 0 && CursorShapeTextureCache.Pixels == 0, "repeated teardown is safe");
Console.WriteLine("Cursor mask checks passed (geometry, reuse, teardown, both budgets, failure recovery and isolation).");

Check(RoundedRectTextureCache.Created > 0 && RoundedRectTextureCache.Destroyed > 0
    && RoundedRectTextureCache.InvalidHits > 0 && RoundedRectTextureCache.Pixels == 0, "key counters and released pixels");
Check(RainBarTextureCache.Bakes > 0 && RainBarTextureCache.Destroyed > 0
    && RainBarTextureCache.InvalidHits > 0 && RainBarTextureCache.Pixels == 0, "rain counters and released pixels");
Check(CursorShapeTextureCache.Bakes > 0 && CursorShapeTextureCache.Destroyed > 0, "mouse counters");
