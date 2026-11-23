using UnityEngine;

namespace TwilightInputOverlay
{
    internal static class PanelTextInput
    {
        private static string _activeField;
        private static string _draft;
        private static bool _replaceOnType;

        internal static bool IsEditing => _activeField != null;

        internal static void Cancel(string key)
        {
            if (_activeField == key) Cancel();
        }

        internal static void Cancel()
        {
            _activeField = null;
            _draft = null;
            _replaceOnType = false;
        }

        internal static string Draw(string key, string value, GUIStyle style, params GUILayoutOption[] options)
        {
            value = value ?? "";
            // Use scalar IMGUI controls and keyboard events. GUILayout.TextField
            // enters Unity's stripped TextEditor.set_text on the Store player.
            bool editing = _activeField == key;
            if (GUILayout.Button(editing ? _draft + "|" : value.Length == 0 ? "..." : value, style, options) && !editing)
            {
                _activeField = key;
                _draft = value;
                _replaceOnType = true;
            }

            Event input = Event.current;
            if (!editing || input.type != EventType.KeyDown || !GUI.enabled)
                return value;

            switch (input.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    input.Use();
                    string result = _draft;
                    Cancel();
                    return result;
                case KeyCode.Escape:
                    input.Use();
                    Cancel();
                    return value;
                case KeyCode.Backspace:
                case KeyCode.Delete:
                    if (_replaceOnType)
                        _draft = "";
                    else if (_draft.Length > 0)
                    {
                        int length = _draft.Length - 1;
                        if (length > 0 && char.IsSurrogatePair(_draft, length - 1))
                            length--;
                        _draft = _draft.Substring(0, length);
                    }
                    _replaceOnType = false;
                    break;
                default:
                    if (input.control || input.command)
                    {
                        if (input.keyCode == KeyCode.A)
                            _replaceOnType = true;
                        else if (input.keyCode == KeyCode.V)
                        {
                            string pasted = GUIUtility.systemCopyBuffer;
                            if (!string.IsNullOrEmpty(pasted))
                                Append(pasted.Replace("\r", "").Replace("\n", "").Replace("\t", ""));
                        }
                    }
                    else if (!char.IsControl(input.character))
                        Append(input.character.ToString());
                    break;
            }
            input.Use();
            return value;
        }

        private static void Append(string text)
        {
            _draft = _replaceOnType ? text : _draft + text;
            _replaceOnType = false;
        }
    }
}
