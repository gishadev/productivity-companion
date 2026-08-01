using UnityEngine;

namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// Used in the editor and on every non-Windows target. Every call is a no-op, so callers
    /// never need their own platform guards.
    /// </summary>
    public sealed class NullPlatformWindow : IPlatformWindow
    {
        public bool IsAvailable => false;
        public bool IsMinimized => false;
        public bool IsTopmost => false;
        public bool IsTaskbarForeground => false;

        public void RemoveChrome() { }
        public void ApplyPerPixelAlpha() { }
        public void ApplyColorKey(Color32 key) { }
        public void ClearLayered() { }
        public void SetClickThrough(bool enabled) { }
        public void SetTopmost(bool enabled) { }
        public void SetHiddenFromTaskbar(bool hidden) { }

        public bool TryGetCursorPosition(out Vector2 unityScreenPosition)
        {
            unityScreenPosition = default;
            return false;
        }
    }
}
