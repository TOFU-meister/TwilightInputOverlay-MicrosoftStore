using UnityEngine;

namespace TwilightInputOverlay
{
    /// <summary>
    /// Shared IMGUI styles used by both the standalone settings panel and the
    /// HSRTimer/TwilightTimer-integrated tab.
    /// </summary>
    internal static class PanelStyles
    {
        private static bool _ready;

        public static GUIStyle Label { get; private set; }
        public static GUIStyle Value { get; private set; }
        public static GUIStyle Section { get; private set; }
        public static GUIStyle Small { get; private set; }
        public static GUIStyle Toggle { get; private set; }
        public static GUIStyle Button { get; private set; }
        public static GUIStyle TextField { get; private set; }

        public static void Ensure()
        {
            if (_ready) return;
            Label = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false };
            Value = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            Section = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            Small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            Toggle = new GUIStyle(GUI.skin.toggle) { fontSize = 13, wordWrap = false };
            Button = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = false };
            TextField = new GUIStyle(GUI.skin.button) { fontSize = 13 };
            _ready = true;
        }
    }
}
