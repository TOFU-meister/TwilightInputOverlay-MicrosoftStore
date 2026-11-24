using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace TwilightInputOverlay
{
    /// <summary>
    /// "Raining Keys": when a key of the first HUD row (<see cref="SettingsModel.Rows"/>[0],
    /// the visually top row) is pressed, a bar (rectangle) appears above that key. While
    /// the key is held, the bar's top edge extends upward at the flow speed; after release
    /// the whole bar keeps translating upward at the same speed until its bottom passes the
    /// top edge of the region, then it disappears. Pressing again during the release
    /// animation spawns another bar that animates independently, so several bars can be
    /// visible at once.
    /// <para>
    /// Bars are clipped to a rectangular region whose width is the first row's total width,
    /// whose height is <see cref="SettingsModel.RainingHeight"/> and whose bottom edge sits
    /// <see cref="SettingsModel.RainingGap"/> above the key tops; the region itself is not
    /// drawn. A held bar's bottom edge is pinned at the region's bottom edge (so the gap
    /// never clips the bottom rounded end cap away — with gap 0 it coincides with the key
    /// tops); the clip only ever straight-cuts a bar that extends past the region's top.
    /// A width-1 dual key spawns one bar per half (each listening to its own keybind),
    /// like the per-half pressed style of the HUD keys; a configured fixed width is split in
    /// two, so each half bar is half the width and the two halves stay adjacent with their
    /// seam at the key's centre. A single bar's width defaults to the width of the key it
    /// rises from; a fixed width can be configured instead.
    /// Bars are plain rectangles by default, or rounded rectangles when
    /// <see cref="SettingsModel.RainingRadius"/> &gt; 0. All Raining Keys values are
    /// HUD-space and scale with <see cref="SettingsModel.Scale"/>.
    /// </para>
    /// <para>
    /// The row geometry (cell size, spacing, row origin, key width) is computed with the
    /// same formulas as <see cref="InputHud"/>; keep the two in sync if the layout math
    /// ever changes.
    /// </para>
    /// </summary>
    public class RainingKeysOverlay : MonoBehaviour
    {
        public RainingKeysOverlay(System.IntPtr pointer) : base(pointer) { }

        private void OnDestroy() => RainBarTextureCache.Release();

        private bool _barDrawingFailed;
        internal static long TextureDraws { get; private set; }
        internal static long RoundedFallbacks { get; private set; }

        /// <summary>
        /// One active bar. Screen/GUI Y grows downward, so "up" is smaller Y.
        /// While <see cref="Held"/> the bottom edge is pinned at the region's bottom edge
        /// (<see cref="SettingsModel.RainingGap"/> above the key tops) and only the top
        /// extends; once the key is released the whole bar translates upward.
        /// </summary>
        private sealed class Bar
        {
            public float TopY;    // Y relative to the region bottom
            public float BottomY; // Y relative to the region bottom
            public bool Held;     // true while the key is held (top extends)
        }

        /// <summary>
        /// Per-input-slot state. The slot scheme mirrors <see cref="InputHud"/>'s fade
        /// slots: a single key uses id*2, a dual key uses id*2 (left half) and id*2+1
        /// (right half), so layout edits drop stale entries naturally.
        /// </summary>
        private sealed class Rain
        {
            public bool WasPressed;
            public readonly List<Bar> Bars = new List<Bar>();
        }

        /// <summary>Defensive cap on concurrent bars per slot (a release animation is
        /// finite, so this only guards pathological states).</summary>
        private const int MaxBarsPerSlot = 32;

        private Dictionary<int, Rain> _rains = new Dictionary<int, Rain>();
        private Dictionary<int, Rain> _nextRains = new Dictionary<int, Rain>();

        // ── Debug logging (SettingsModel.DebugInfo) ───────────────────────
        // Log bar counts and the rate of HUD and rain mask creation. A held
        // bar at full height reuses its existing mask.
        private float _debugTimer;
        private int _debugMaxBars;
        private float _debugMaxBarHeight;
        private int _debugBakeStart;
        private int _debugRainBakeStart;
        private long _debugLastMem;
        private bool _wasDebug;

        private static int SlotOf(KeyEntry e, int half)
        {
            // half -1/0 = single or left half, 1 = right half of a dual key.
            return half <= 0 ? e.Id * 2 : e.Id * 2 + 1;
        }

        private void Update()
        {
            var cfg = ConfigService.Instance;
            if (cfg == null) return;
            var s = cfg.Settings;

            if (!s.DebugInfo)
            {
                _debugTimer = 0f;
                _debugMaxBars = 0;
                _debugMaxBarHeight = 0f;
                _debugBakeStart = RoundedRectTextureCache.FillBakes;
                _debugRainBakeStart = RainBarTextureCache.Bakes;
                _wasDebug = false;
            }
            else if (!_wasDebug)
            {
                _wasDebug = true;
                _debugLastMem = System.GC.GetTotalMemory(false);
                Plugin.Logger.LogInfo(string.Format(
                    "[RainingKeys] debug logging enabled: enabled={0} height={1} gap={2} speed={3} width={4} radius={5} scale={6}",
                    s.RainingEnabled, s.RainingHeight, s.RainingGap, s.RainingSpeed, s.RainingWidth, s.RainingRadius, s.Scale));
            }

            float speed = Mathf.Max(0f, s.RainingSpeed * s.Scale);
            if (!s.RainingEnabled || !s.ShowHud || s.Rows == null || s.Rows.Count == 0 || speed <= 0f)
            {
                _rains.Clear();
                _nextRains.Clear();
                return;
            }

            // Keep the simulation relative to the bottom of the rain region so
            // changes in HUD position or screen height move existing bars with it.
            float regionHeightPx = Mathf.Max(1f, s.RainingHeight * s.Scale);
            float gapPx = Mathf.Max(0f, s.RainingGap * s.Scale);
            float regionTop = -regionHeightPx;
            float dt = Time.deltaTime;
            if (dt <= 0f) dt = Time.unscaledDeltaTime;
            if (dt <= 0f) dt = 1f / 60f;

            var row = s.Rows[0];
            var next = _nextRains;
            next.Clear();
            for (int i = 0; i < row.Keys.Count; i++)
            {
                var e = row.Keys[i];
                if (e.Blank) continue;

                if (e.Width == 1 && e.Dual)
                {
                    Advance(next, SlotOf(e, 0), InputState.IsHeld(e.Key1), regionTop, speed, dt);
                    Advance(next, SlotOf(e, 1), InputState.IsHeld(e.Key2), regionTop, speed, dt);
                }
                else
                {
                    Advance(next, SlotOf(e, -1), InputState.IsHeld(e.Key1), regionTop, speed, dt);
                }
            }
            _nextRains = _rains;
            _rains = next;
            _nextRains.Clear();

            if (s.DebugInfo)
                LogDebugStats(s, regionHeightPx, gapPx, dt);
        }

        [HideFromIl2Cpp]
        private void LogDebugStats(SettingsModel s, float regionHeightPx, float gapPx, float dt)
        {
            int bars = 0;
            float maxHeight = 0f;
            foreach (var kv in _rains)
            {
                bars += kv.Value.Bars.Count;
                var list = kv.Value.Bars;
                for (int i = 0; i < list.Count; i++)
                {
                    float h = list[i].BottomY - list[i].TopY;
                    if (h > maxHeight) maxHeight = h;
                }
            }
            _debugMaxBars = Mathf.Max(_debugMaxBars, bars);
            _debugMaxBarHeight = Mathf.Max(_debugMaxBarHeight, maxHeight);

            _debugTimer += dt;
            if (_debugTimer < 1f)
                return;

            // A held bar's height is bounded by the region height (bottom pinned at
            // the region's bottom edge, top clamped to the region top); warn if a bar
            // ever exceeds that, which would mean the clamp stopped working.
            float bound = regionHeightPx + gapPx + 1f;
            if (_debugMaxBarHeight > bound)
                Plugin.Logger.LogWarning(string.Format(
                    "[RainingKeys] bar height exceeded bound: max={0:0.##}px bound={1:0.##}px (height={2} gap={3} scale={4})",
                    _debugMaxBarHeight, bound, s.RainingHeight, s.RainingGap, s.Scale));

            int bakes = RoundedRectTextureCache.FillBakes - _debugBakeStart;
            int rainBakes = RainBarTextureCache.Bakes - _debugRainBakeStart;
            long mem = System.GC.GetTotalMemory(false);
            long delta = mem - _debugLastMem;
            Plugin.Logger.LogInfo(string.Format(
                "[RainingKeys] 1s: bars(max)={0} barH(max)={1:0.##}px regionH={2:0.##}px fillTexCalls={3} bakes={4}/s rainBakes={5}/s cache={6} gcMem={7:0.0}MB delta={8:+#0.0;-#0.0}MB/s",
                _debugMaxBars, _debugMaxBarHeight, regionHeightPx,
                RoundedRectTextureCache.FillCalls, bakes, rainBakes,
                RoundedRectTextureCache.FillCacheCount + RainBarTextureCache.Count,
                mem / (1024f * 1024f), delta / (1024f * 1024f)));

            _debugTimer = 0f;
            _debugMaxBars = 0;
            _debugMaxBarHeight = 0f;
            _debugBakeStart = RoundedRectTextureCache.FillBakes;
            _debugRainBakeStart = RainBarTextureCache.Bakes;
            _debugLastMem = mem;
        }

        [HideFromIl2Cpp]
        private void Advance(Dictionary<int, Rain> next, int slot, bool pressed,
            float regionTop, float speed, float dt)
        {
            Rain rain;
            if (!_rains.TryGetValue(slot, out rain))
            {
                if (!pressed) return;
                rain = new Rain();
            }

            // Rising edge: a new bar appears with height 0 at the region's bottom edge
            // (RainingGap above the key tops). Its bottom stays pinned there while held,
            // so the bottom rounded end cap is never clipped away by the region.
            if (pressed && !rain.WasPressed)
            {
                if (rain.Bars.Count >= MaxBarsPerSlot)
                    rain.Bars.RemoveAt(0);
                rain.Bars.Add(new Bar { Held = true });
            }
            rain.WasPressed = pressed;

            int kept = 0;
            for (int i = 0; i < rain.Bars.Count; i++)
            {
                var bar = rain.Bars[i];
                if (bar.Held && pressed)
                {
                    // While held the top extends upward, but never past the
                    // region's top edge: the region clip hides anything beyond
                    // it anyway, and stopping there keeps the bar's height
                    // bounded (a held bar is never removed, so an unclamped
                    // top would grow without limit and re-bake a larger and
                    // larger rounded-rect texture every frame, eventually
                    // crashing the game).
                    bar.TopY = Mathf.Max(regionTop, bar.TopY - speed * dt);
                }
                if (bar.Held && !pressed)
                    bar.Held = false;                    // just released: start translating
                if (!bar.Held)
                {
                    // Released: the whole bar keeps moving up at the same speed.
                    bar.TopY -= speed * dt;
                    bar.BottomY -= speed * dt;
                }
                if (bar.BottomY <= regionTop)
                    continue;                            // fully above the region: gone
                rain.Bars[kept++] = bar;
            }
            if (kept < rain.Bars.Count) rain.Bars.RemoveRange(kept, rain.Bars.Count - kept);

            if (rain.Bars.Count > 0 || pressed)
                next[slot] = rain;
        }

        private void OnGUI()
        {
            if (_barDrawingFailed || Event.current.type != EventType.Repaint) return;

            var cfg = ConfigService.Instance;
            if (cfg == null) return;
            var s = cfg.Settings;
            if (!s.RainingEnabled || !s.ShowHud || s.Rows == null || s.Rows.Count == 0)
                return;

            using var measurement = new OverlayPerformance.Measurement(1);
            // Same layout math as InputHud.OnGUI: keep in sync.
            float cell = Mathf.Max(8f, 56f * s.Scale);
            float spacing = Mathf.Max(0f, s.Spacing * s.Scale);
            float blockHeight = s.Rows.Count * cell + (s.Rows.Count - 1) * spacing;
            float rowY = Screen.height - s.OffsetY - blockHeight;

            var row = s.Rows[0];
            float rowWidth = RowWidth(row, cell, spacing);
            float regionHeight = Mathf.Max(1f, s.RainingHeight * s.Scale);
            float regionBottom = RainBarGeometry.RegionBottom(rowY, s.RainingGap * s.Scale);
            var region = new Rect(s.OffsetX, regionBottom - regionHeight, rowWidth, regionHeight);
            if (region.width <= 0f || region.height <= 0f)
                return;

            float fixedWidth = s.RainingWidth > 0f ? Mathf.Max(1f, s.RainingWidth * s.Scale) : 0f;
            float radius = s.RainingRadius * s.Scale;

            Color prev = GUI.color;
            GUI.BeginGroup(region);
            try
            {
                GUI.color = s.RainingColor;
                float regionTop = -regionHeight;
                float cx = 0f; // key x relative to the region (region.x == rowX)
                for (int i = 0; i < row.Keys.Count; i++)
                {
                    var e = row.Keys[i];
                    float w = KeyWidth(e, cell, spacing);
                    if (!e.Blank)
                    {
                        if (e.Width == 1 && e.Dual)
                        {
                            Rain left, right;
                            // A dual key splits the configured bar width in two: each
                            // half bar is half the width and the two halves stay adjacent,
                            // their seam at the key's centre (auto width = key half,
                            // which gives the same result as before).
                            float halfW = RainBarGeometry.HalfWidth(fixedWidth > 0f ? fixedWidth : w);
                            if (_rains.TryGetValue(SlotOf(e, 0), out left))
                                DrawBars(left, RainBarGeometry.Center(cx, w, halfW, -1), halfW, radius, regionTop);
                            if (_rains.TryGetValue(SlotOf(e, 1), out right))
                                DrawBars(right, RainBarGeometry.Center(cx, w, halfW, 1), halfW, radius, regionTop);
                        }
                        else
                        {
                            Rain rain;
                            if (_rains.TryGetValue(SlotOf(e, -1), out rain))
                                DrawBars(rain, RainBarGeometry.Center(cx, w, w, 0), fixedWidth > 0f ? fixedWidth : w, radius, regionTop);
                        }
                    }
                    cx += w;
                    if (i < row.Keys.Count - 1)
                        cx += spacing;
                }
            }
            finally
            {
                GUI.color = prev;
                GUI.EndGroup();
            }
        }

        [HideFromIl2Cpp]
        private void DrawBars(Rain rain, float centerX, float width, float radius, float regionTop)
        {
            foreach (var bar in rain.Bars)
            {
                float height = bar.BottomY - bar.TopY;
                if (height <= 0f) continue;
                var rect = new Rect(centerX - width * 0.5f, bar.TopY - regionTop, width, height);

                // BeginGroup clips the combined shape without adding an end cap.
                DrawBar(rect, radius);
                if (_barDrawingFailed) return;
            }
        }

        private void DrawBar(Rect rect, float radius)
        {
            if (_barDrawingFailed) return;

            if (OverlayTexture.Rain.CanAttempt && radius > 0f)
            {
                int submitted = 0;
                try
                {
                    var mask = RainBarTextureCache.Get(rect.width, radius);
                    if (mask.Texture != null)
                    {
                        Span<RainBarGeometry.Slice> slices = stackalloc RainBarGeometry.Slice[9];
                        int count = RainBarGeometry.Slices(rect.width, rect.height, radius, mask.Radius,
                            mask.ShapeWidth, mask.ShapeHeight, slices);
                        for (; submitted < count; submitted++)
                        {
                            var slice = slices[submitted];
                            GUI.DrawTextureWithTexCoords(
                                new Rect(rect.x + slice.X, rect.y + slice.Y, slice.Width, slice.Height),
                                mask.Texture, new Rect(slice.U, slice.V, slice.UWidth, slice.VHeight), true);
                            TextureDraws++;
                        }
                        if (submitted > 0) return;
                    }
                }
                catch (Exception error)
                {
                    OverlayTexture.Rain.Fail(error);
                    RainBarTextureCache.Release();
                    // The following bars can fall back immediately. Do not
                    // paint over already submitted translucent slices.
                    if (submitted > 0) return;
                }
            }

            if (radius > 0f) RoundedFallbacks++;

            try
            {
                var white = Texture2D.whiteTexture;
                if (white != null)
                {
                    GUI.DrawTexture(rect, white);
                    TextureDraws++;
                }
            }
            catch (Exception error)
            {
                _barDrawingFailed = true;
                Plugin.Logger.LogWarning($"[RainingKeys] Rectangle rendering unavailable; pausing bar rendering: {error}");
            }
        }

        /// <summary>Total width of one row: key widths plus the inter-key spacings.</summary>
        private static float RowWidth(KeyRow row, float cell, float spacing)
        {
            float w = 0f;
            for (int i = 0; i < row.Keys.Count; i++)
            {
                w += KeyWidth(row.Keys[i], cell, spacing);
                if (i < row.Keys.Count - 1)
                    w += spacing;
            }
            return w;
        }

        /// <summary>Width of one key unit: 1 cell, or n cells + (n-1) internal gaps
        /// (mirrors InputHud.KeyWidth).</summary>
        private static float KeyWidth(KeyEntry e, float cell, float spacing)
        {
            if (e.Width <= 1) return cell;
            return cell * e.Width + spacing * (e.Width - 1);
        }
    }
}
