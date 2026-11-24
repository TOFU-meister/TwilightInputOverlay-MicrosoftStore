using System;

namespace TwilightInputOverlay
{
    internal sealed class TextureRecovery
    {
        private const long RetryDelayMs = 5000;
        private readonly string _label;
        private readonly Func<long> _clock;
        private readonly Action<string> _warning;
        private long _retryAfter;
        private bool _warned;

        internal TextureRecovery(string label)
            : this(label, () => Environment.TickCount64, message => Plugin.Logger.LogWarning(message)) { }

        internal TextureRecovery(string label, Func<long> clock, Action<string> warning)
        {
            _label = label;
            _clock = clock;
            _warning = warning;
        }

        internal bool Unsupported { get; private set; }
        internal bool CanAttempt => !Unsupported && _clock() >= _retryAfter;
        internal int Failures { get; private set; }
        internal bool WarningPending => !_warned;

        internal void Fail(Exception error, string context = null)
        {
            Failures++;
            Unsupported = error is MissingMemberException or EntryPointNotFoundException or NotSupportedException;
            _retryAfter = _clock() + RetryDelayMs;
            if (_warned) return;
            _warned = true;
            _warning(context == null
                ? $"TwilightInputOverlay: {_label} unavailable. {error.GetType().Name}: {error.Message}"
                : $"TwilightInputOverlay: {_label} unavailable. {context}\n{error}");
        }
    }
}
