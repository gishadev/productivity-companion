using UnityEngine;

namespace gishadev.companion.Window.Native
{
    // No-op with IsAvailable false in the editor and on non-Windows.
    public interface IPlatformWindow
    {
        bool IsAvailable { get; }

        bool IsMinimized { get; }

        bool IsTopmost { get; }

        // Windows drops our topmost flag while the taskbar is foreground.
        bool IsTaskbarForeground { get; }

        void RemoveChrome();

        // Call after RemoveChrome.
        void FitToMonitor();

        void ApplyPerPixelAlpha();

        void ApplyColorKey(Color32 key);

        void ClearLayered();

        void SetClickThrough(bool enabled);

        void SetTopmost(bool enabled);

        void SetHiddenFromTaskbar(bool hidden);

        // Read from Win32: a click-through window gets no mouse messages. False outside the client area.
        bool TryGetCursorPosition(out Vector2 unityScreenPosition);
    }
}
