using UnityEngine;

namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// The only window-manipulation surface the rest of the app touches. Safe to call unconditionally:
    /// with no native window (editor, non-Windows) every member is a no-op and IsAvailable is false.
    /// </summary>
    public interface IPlatformWindow
    {
        bool IsAvailable { get; }

        bool IsMinimized { get; }

        bool IsTopmost { get; }

        /// <summary>True when the taskbar owns the foreground — when Windows drops our topmost flag.</summary>
        bool IsTaskbarForeground { get; }

        /// <summary>Strips the title bar and resize border.</summary>
        void RemoveChrome();

        /// <summary>Layered window with per-pixel alpha, backed by a DWM frame extension.</summary>
        void ApplyPerPixelAlpha();

        /// <summary>Layered window where every pixel matching <paramref name="key"/> is punched out.</summary>
        void ApplyColorKey(Color32 key);

        void ClearLayered();

        /// <summary>Toggles WS_EX_TRANSPARENT so mouse input falls through to what is behind us.</summary>
        void SetClickThrough(bool enabled);

        void SetTopmost(bool enabled);

        /// <summary>Hides from taskbar and Alt-Tab via WS_EX_TOOLWINDOW, keeping the window visible.</summary>
        void SetHiddenFromTaskbar(bool hidden);

        /// <summary>
        /// Cursor position in Unity screen coordinates, read straight from Win32 — a click-through
        /// window receives no mouse messages, so Unity's own cursor position freezes. False when the
        /// window is unavailable or the cursor is outside our client area.
        /// </summary>
        bool TryGetCursorPosition(out Vector2 unityScreenPosition);
    }
}
