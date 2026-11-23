using System;

namespace TwilightInputOverlay
{
    internal static class RainBarGeometry
    {
        internal static float RegionBottom(float rowTop, float gap)
            => rowTop - Math.Max(0f, gap);

        internal static float HalfWidth(float width) => width * 0.5f;

        internal static float Center(float keyLeft, float keyWidth, float barWidth, int half)
            => keyLeft + keyWidth * 0.5f + half * barWidth * 0.5f;

        internal static float Radius(float width, float height, float requested)
            => Math.Clamp(requested, 0f, Math.Min(width, height) * 0.5f);

        internal static byte Alpha(float x, float y, int width, int height, float radius)
        {
            float dx = radius - Math.Min(x, width - x);
            float dy = radius - Math.Min(y, height - y);
            float outsideX = Math.Max(0f, dx);
            float outsideY = Math.Max(0f, dy);
            float distance = MathF.Sqrt(outsideX * outsideX + outsideY * outsideY)
                + Math.Min(0f, Math.Max(dx, dy)) - radius;
            float coverage = Math.Clamp(0.5f - distance, 0f, 1f);
            return (byte)MathF.Round(coverage * 255f);
        }
    }
}
