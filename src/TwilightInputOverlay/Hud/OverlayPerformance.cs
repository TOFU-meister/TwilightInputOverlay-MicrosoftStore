using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace TwilightInputOverlay
{
    internal static class OverlayPerformance
    {
        private const int MaxRows = 360;
        private const string FileName = "overlay-performance.csv";
        private const string Header = "utc,uptime_s,scene,key_visible,rain_enabled,mouse_visible,mouse_region_visible,frames,fps,"
            + "key_draws,key_total_ms,rain_draws,rain_total_ms,mouse_draws,mouse_total_ms,"
            + "key_count,key_pixels,key_created,key_destroy_requested,key_invalid_hits,key_rebuilt,"
            + "rain_count,rain_pixels,rain_created,rain_destroy_requested,rain_invalid_hits,rain_rebuilt,rain_mask_hits,rain_mask_misses,rain_texture_draws,rain_rounded_fallbacks,"
            + "mouse_count,mouse_pixels,mouse_created,mouse_destroy_requested,mouse_invalidations,mouse_rebuilt,collect_ms";
        private static Queue<string> _rows;
        private static readonly long[] Ticks = new long[3];
        private static readonly int[] Counts = new int[3];
        private static float _deadline;
        private static int _frames;
        private static double _seconds;
        private static string _status;
        internal static bool Enabled { get; private set; }
        internal static int RetainedSamples => _rows?.Count ?? 0;

        internal readonly struct Measurement : IDisposable
        {
            private readonly long _start;
            private readonly int _channel;
            internal Measurement(int channel)
            {
                _channel = channel;
                _start = Enabled ? Stopwatch.GetTimestamp() : 0;
            }

            public void Dispose()
            {
                if (_start == 0) return;
                Ticks[_channel] += Stopwatch.GetTimestamp() - _start;
                Counts[_channel]++;
            }
        }

        internal static void Draw()
        {
            if (GUILayout.Button(Enabled ? "Stop overlay performance capture" : "Start overlay performance capture", PanelStyles.Button))
            {
                if (Enabled)
                {
                    Enabled = false;
                    _status = "Overlay performance capture stopped.";
                }
                else
                {
                    _rows ??= new Queue<string>(MaxRows);
                    _rows.Clear();
                    ResetCounters();
                    _deadline = Time.realtimeSinceStartup + 5f;
                    Enabled = true;
                    _status = "Overlay capture active (5-second samples).";
                }
            }
            if (_rows?.Count > 0 && GUILayout.Button("Export overlay-performance.csv", PanelStyles.Button))
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(Header + "\n" + string.Join("\n", _rows) + "\n");
                    PersistenceService.WriteAtomically(PersistenceService.PathFor(FileName),
                        stream => stream.Write(bytes, 0, bytes.Length));
                    _status = FileName + " saved in the Overlay data folder.";
                }
                catch (Exception error) { _status = error.Message; }
            }
            if (_status != null) GUILayout.Label(_status, PanelStyles.Small);
        }

        internal static void Sample(SettingsModel settings)
        {
            if (!Enabled) return;
            float seconds = Time.unscaledDeltaTime;
            if (float.IsFinite(seconds) && seconds > 0)
            {
                _frames++;
                _seconds += seconds;
            }
            float now = Time.realtimeSinceStartup;
            if (now < _deadline) return;
            _deadline = now + 5f;
            long start = Stopwatch.GetTimestamp();
            string scene;
            try { scene = Application.loadedLevelName; }
            catch (Exception) { scene = ""; }
            var fields = new object[]
            {
                DateTime.UtcNow.ToString("O"), now, scene, settings.ShowHud, settings.RainingEnabled, settings.ShowCursor, settings.ShowCursorRegion,
                _frames, _seconds > 0 ? _frames / _seconds : 0,
                Counts[0], Ms(Ticks[0]), Counts[1], Ms(Ticks[1]), Counts[2], Ms(Ticks[2]),
                RoundedRectTextureCache.FillCacheCount, RoundedRectTextureCache.Pixels, RoundedRectTextureCache.Created,
                RoundedRectTextureCache.Destroyed, RoundedRectTextureCache.InvalidHits, RoundedRectTextureCache.Rebuilt,
                RainBarTextureCache.Count, RainBarTextureCache.Pixels, RainBarTextureCache.Bakes,
                RainBarTextureCache.Destroyed, RainBarTextureCache.InvalidHits, RainBarTextureCache.Rebuilt,
                RainBarTextureCache.Hits, RainBarTextureCache.Misses, RainingKeysOverlay.TextureDraws, RainingKeysOverlay.RoundedFallbacks,
                CursorShapeTextureCache.Count, CursorShapeTextureCache.Pixels, CursorShapeTextureCache.Bakes,
                CursorShapeTextureCache.Destroyed, CursorShapeTextureCache.Invalidated, CursorShapeTextureCache.Rebuilt,
            };
            var csv = Array.ConvertAll(fields, Format);
            if (_rows.Count == MaxRows) _rows.Dequeue();
            _rows.Enqueue(string.Join(",", csv) + "," + Format(Ms(Stopwatch.GetTimestamp() - start)));
            ResetCounters();
        }

        private static void ResetCounters()
        {
            _frames = 0;
            _seconds = 0;
            Array.Clear(Ticks, 0, Ticks.Length);
            Array.Clear(Counts, 0, Counts.Length);
        }

        private static double Ms(long ticks) => ticks * (1000d / Stopwatch.Frequency);
        private static string Format(object value) => value is string text
            ? "\"" + text.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\""
            : Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
