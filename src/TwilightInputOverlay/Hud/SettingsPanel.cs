using UnityEngine;

namespace TwilightInputOverlay
{
    public class SettingsPanel : MonoBehaviour
    {
        public SettingsPanel(System.IntPtr pointer) : base(pointer) { }

        private GUI.WindowFunction _drawWindow;
        private bool _drawFailed;

        private bool _visible;

        private Rect _rect = new Rect(60f, 60f, 640f, 600f);
        private Vector2 _scroll;

        private void Awake()
        {
            _drawWindow = new System.Action<int>(DrawWindow);
        }

        private void Update()
        {
            var cfg = ConfigService.Instance;
            if (cfg == null) return;
            if (Input.GetKeyDown(cfg.Settings.PanelKey))
                Toggle();
        }

        private void OnDestroy()
        {
            if (ConfigService.Instance != null)
                ConfigService.Instance.SaveSettings();
        }

        private void OnApplicationQuit()
        {
            if (ConfigService.Instance != null)
                ConfigService.Instance.SaveSettings();
        }

        private void Toggle()
        {
            _visible = !_visible;
            if (_visible)
            {
                _drawFailed = false;
                // Open on the root page: clear any open sub-page, language
                // dropdown and pending key rebind from a previous session.
                PanelContent.ResetTransientState();
            }
            else if (ConfigService.Instance != null)
            {
                ConfigService.Instance.SaveSettings();
            }
        }

        private void OnGUI()
        {
            if (!_visible || _drawFailed) return;
            try
            {
                PanelStyles.Ensure();
                _rect = GUI.Window(GetInstanceID(), _rect, _drawWindow, PluginInfo.PLUGIN_NAME);
            }
            catch (System.Exception error)
            {
                ReportDrawFailure(error);
            }
        }

        private void DrawWindow(int id)
        {
            if (_drawFailed) return;
            try
            {
                Draw();
            }
            catch (System.Exception error)
            {
                ReportDrawFailure(error);
            }
        }

        private void ReportDrawFailure(System.Exception error)
        {
            _drawFailed = true;
            Plugin.Logger.LogWarning($"Settings drawing stopped until reopened: {error}");
        }

        private void Draw()
        {
            var cfg = ConfigService.Instance;
            if (cfg == null) return;
            var loc = cfg.Localization;

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandWidth(true));
            PanelContent.Draw(false);

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(loc.Get("PANEL_SAVE"), PanelStyles.Button))
                cfg.SaveSettings();
            if (GUILayout.Button(loc.Get("PANEL_CLOSE"), PanelStyles.Button))
            {
                cfg.SaveSettings();
                _visible = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(loc.Get("PANEL_FOOTER"), PanelStyles.Small);
            GUILayout.Label(loc.Get("STORE_TEXT_INPUT_HINT"), PanelStyles.Small);
            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, _rect.width, 20));
        }
    }
}
