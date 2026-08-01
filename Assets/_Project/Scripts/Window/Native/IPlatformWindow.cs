using UnityEngine;

namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// The only window-manipulation surface the rest of the app is allowed to touch.
    /// Implementations must be safe to call unconditionally: when the platform has no
    /// native window (editor, non-Windows), every member is a no-op and
    /// <see cref="IsAvailable"/> is false.
    /// </summary>
    public interface IPlatformWindow
    {
        /// <summary>True when a real native window handle was resolved and calls will have an effect.</summary>
        bool IsAvailable { get; }

        bool IsMinimized { get; }

        /// <summary>Whether the OS currently has the topmost flag set on our window.</summary>
        bool IsTopmost { get; }

        /// <summary>True when the shell taskbar owns the foreground, which is when Windows drops our topmost flag.</summary>
        bool IsTaskbarForeground { get; }

        /// <summary>Strips the title bar and resize border so the widget is borderless.</summary>
        void RemoveChrome();

        /// <summary>Layered window with per-pixel alpha, backed by a DWM frame extension.</summary>
        void ApplyPerPixelAlpha();

        /// <summary>Layered window where every pixel matching <paramref name="key"/> is punched out.</summary>
        void ApplyColorKey(Color32 key);

        /// <summary>Drops the layered style entirely, returning to an ordinary opaque window.</summary>
        void ClearLayered();

        /// <summary>Toggles WS_EX_TRANSPARENT so mouse input falls through to whatever is behind us.</summary>
        void SetClickThrough(bool enabled);

        void SetTopmost(bool enabled);

        /// <summary>Hides from the taskbar and Alt-Tab via WS_EX_TOOLWINDOW while keeping the window visible.</summary>
        void SetHiddenFromTaskbar(bool hidden);

        /// <summary>
        /// Cursor position in Unity screen coordinates, read straight from Win32.
        /// Returns false when the window is unavailable or the cursor is outside our client area.
        /// </summary>
        /// <remarks>
        /// A click-through window receives no mouse messages at all, so Unity's own cursor position
        /// freezes the instant click-through engages. Reading the cursor from the OS is what lets the
        /// hit test recover and turn click-through back off.
        /// </remarks>
        bool TryGetCursorPosition(out Vector2 unityScreenPosition);
    }
}
