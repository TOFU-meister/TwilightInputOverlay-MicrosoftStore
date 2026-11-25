using System;
using TwilightInputOverlay;

ConfigService.DebugModeEnabled = true;

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

// Reproduce the reported fractional settings, both automatic and fixed width,
// with dimensions small enough to clamp and tall enough to retain the radius.
foreach (float width in new[] { 56f * 1.33f, 28f * 1.33f, 23.45f * 1.33f })
foreach (float height in new[] { 0.35f, 2.7f, 21.93f, 160f * 1.33f })
{
    string stage = null;
    var texture = RainBarTextureCache.Get(width, height, 8.24f * 1.33f, ref stage, out var info);
    Check(texture != null && texture.Pixels.Length == info.Width * info.Height, "fractional mask is usable");
    Check(info.Radius > 0 && info.Radius <= Math.Min(info.Width, info.Height) * 0.5f, "rounded radius limited by mask dimensions");
    Check(ReferenceEquals(texture, RainBarTextureCache.Get(width, height, 8.24f * 1.33f)), "fractional masks reused");
}
RainBarTextureCache.Release();

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
string rainWarning = null;
var rainRecovery = new TextureRecovery("test Rain", () => 0, warning => rainWarning = warning);
try { throw new InvalidOperationException("simulated draw"); }
catch (Exception error) { rainRecovery.Fail(error, "stage=draw; uv=(0, 0, 1, 1)"); }
Check(rainWarning.Contains("stage=draw") && rainWarning.Contains("simulated draw") && rainWarning.Contains(" at "),
    "first Rain warning carries stage and stack trace");
var firstRainWarning = rainWarning;
rainRecovery.Fail(new Exception("second"), "second warning");
Check(!rainRecovery.WarningPending && rainWarning == firstRainWarning, "repeated failures do not warn again");

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

ConfigService.DebugModeEnabled = false;
var counters = (RoundedRectTextureCache.Created, RoundedRectTextureCache.Destroyed,
    RainBarTextureCache.Bakes, RainBarTextureCache.Destroyed, RainBarTextureCache.Hits,
    CursorShapeTextureCache.Bakes, CursorShapeTextureCache.Destroyed);
var quietKey = RoundedRectTextureCache.GetMask(20, 20, 3);
var quietRain = RainBarTextureCache.Get(20, 20, 3);
var quietCursor = CursorShapeTextureCache.Get(20, 3, circleRecovery);
Check(quietKey != null && quietRain != null && quietCursor.Texture != null, "all caches create textures with debug off");
Check(RainBarTextureCache.Get(20, 20, 3) == quietRain, "rain texture reused with debug off");
RoundedRectTextureCache.ReleaseKeyMasks();
RainBarTextureCache.Release();
CursorShapeTextureCache.Release();
Check(quietKey.Destroyed && quietRain.Destroyed && quietCursor.Texture.Destroyed, "all caches release textures with debug off");
Check(counters == (RoundedRectTextureCache.Created, RoundedRectTextureCache.Destroyed,
    RainBarTextureCache.Bakes, RainBarTextureCache.Destroyed, RainBarTextureCache.Hits,
    CursorShapeTextureCache.Bakes, CursorShapeTextureCache.Destroyed), "diagnostic counters stay unchanged with debug off");
