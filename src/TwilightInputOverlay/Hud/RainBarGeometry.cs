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

        internal readonly record struct Slice(float X, float Y, float Width, float Height,
            float U, float V, float UWidth, float VHeight);

        // GUI positions run downwards; Unity texture coordinates run upwards.
        // Reuse the centre of the mask vertically and, for short bars, stretch
        // its centre horizontally so the contracted corners stay circular.
        internal static int Slices(float width, float height, float radius, float sourceRadius,
            int sourceWidth, int sourceHeight, Span<Slice> slices)
        {
            float r = Radius(width, height, radius);
            if (r <= 0f) return 0;
            float cap = sourceRadius / sourceHeight;
            float middle = Math.Min(0.25f / sourceHeight, (0.5f - cap) * 0.5f);
            bool shortBar = r < Math.Min(radius, width * 0.5f);
            int count = 0;
            if (shortBar)
            {
                float u = sourceRadius / sourceWidth;
                AddColumn(0f, r, 0f, u, height, r, cap, middle, slices, ref count);
                // A width-clamped cap may leave no flat centre in the atlas.
                // Sample a narrow strip in the middle for that case.
                float uCentre = u < 0.5f ? u : 0.5f - 0.25f / sourceWidth;
                AddColumn(r, width - 2f * r, uCentre, 1f - 2f * uCentre, height, r, cap, middle, slices, ref count);
                AddColumn(width - r, r, 1f - u, u, height, r, cap, middle, slices, ref count);
            }
            else
                AddColumn(0f, width, 0f, 1f, height, r, cap, middle, slices, ref count);
            return count;
        }

        private static void AddColumn(float x, float width, float u, float uWidth, float height, float radius,
            float cap, float middle, Span<Slice> slices, ref int count)
        {
            if (width <= 0f) return;
            slices[count++] = new Slice(x, 0f, width, radius, u, 1f - cap, uWidth, cap);
            float centreHeight = height - 2f * radius;
            if (centreHeight > 0f)
                slices[count++] = new Slice(x, radius, width, centreHeight, u, 0.5f - middle, uWidth, 2f * middle);
            slices[count++] = new Slice(x, height - radius, width, radius, u, 0f, uWidth, cap);
        }

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
