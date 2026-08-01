#if UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// Win32 implementation. Instantiated only by <see cref="PlatformWindowFactory"/>, which
    /// refuses to create it in the editor.
    /// </summary>
    public sealed class Win32PlatformWindow : IPlatformWindow
    {
        private const string UnityWindowClassName = "UnityWndClass";

        private static readonly uint CurrentProcessId = (uint)Process.GetCurrentProcess().Id;

        private readonly IntPtr _hwnd;

        // Cached so the per-frame click-through path never pays for a redundant SetWindowLong.
        private bool _clickThrough;
        private bool _hiddenFromTaskbar;

        public Win32PlatformWindow()
        {
            _hwnd = ResolveWindowHandle();
            if (_hwnd == IntPtr.Zero)
                UnityEngine.Debug.LogWarning(
                    "[Window] Could not resolve the player HWND; native window features are disabled.");
        }

        public bool IsAvailable => _hwnd != IntPtr.Zero;

        public bool IsMinimized => IsAvailable && Win32Interop.IsIconic(_hwnd);

        public bool IsTopmost =>
            IsAvailable && (Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE) & Win32Interop.WS_EX_TOPMOST) != 0;

        public bool IsTaskbarForeground
        {
            get
            {
                if (!IsAvailable) return false;
                var taskbar = Win32Interop.FindWindow("Shell_TrayWnd", null);
                return taskbar != IntPtr.Zero && Win32Interop.GetForegroundWindow() == taskbar;
            }
        }

        public void RemoveChrome()
        {
            if (!IsAvailable) return;

            var style = Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_STYLE);
            var stripped = style & ~(Win32Interop.WS_CAPTION | Win32Interop.WS_THICKFRAME |
                                     Win32Interop.WS_MINIMIZEBOX | Win32Interop.WS_MAXIMIZEBOX |
                                     Win32Interop.WS_SYSMENU);
            stripped |= Win32Interop.WS_POPUP;

            if (stripped == style) return;

            Win32Interop.SetWindowLong(_hwnd, Win32Interop.GWL_STYLE, stripped);
            // The non-client area is cached by the OS; SWP_FRAMECHANGED forces it to be recomputed.
            Win32Interop.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE |
                Win32Interop.SWP_FRAMECHANGED);
        }

        public void ApplyPerPixelAlpha()
        {
            if (!IsAvailable) return;

            if (Win32Interop.DwmIsCompositionEnabled(out var composited) == 0 && !composited)
                UnityEngine.Debug.LogWarning(
                    "[Window] DWM composition is disabled; per-pixel alpha will not work. Use the ColorKey mode instead.");

            // Deliberately NOT layered here. The DWM glass sheet below already honours the
            // framebuffer's per-pixel alpha; adding WS_EX_LAYERED + LWA_ALPHA on top applies a second
            // translucency pass over the composited result, which washes out even fully opaque
            // content. The two mechanisms are alternatives, not partners.
            RemoveExStyle(Win32Interop.WS_EX_LAYERED);

            // -1 on every side extends the glass frame across the whole client area.
            var margins = new Win32Interop.MARGINS
            {
                cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1
            };
            Win32Interop.DwmExtendFrameIntoClientArea(_hwnd, ref margins);
        }

        public void ApplyColorKey(Color32 key)
        {
            if (!IsAvailable) return;

            // Color keying works on the composited window surface, so any glass frame left over from
            // a previous PerPixelAlpha run has to be collapsed or it keeps punching its own holes.
            var margins = new Win32Interop.MARGINS();
            Win32Interop.DwmExtendFrameIntoClientArea(_hwnd, ref margins);

            AddExStyle(Win32Interop.WS_EX_LAYERED);
            // COLORREF packs as 0x00BBGGRR, which is byte-reversed from the usual RGB order.
            var colorRef = (uint)(key.r | (key.g << 8) | (key.b << 16));
            Win32Interop.SetLayeredWindowAttributes(_hwnd, colorRef, 0, Win32Interop.LWA_COLORKEY);
        }

        public void ClearLayered()
        {
            if (!IsAvailable) return;

            // Collapse the DWM frame extension before dropping the layered style, otherwise the
            // glass margins linger and the window renders with a transparent border.
            var margins = new Win32Interop.MARGINS();
            Win32Interop.DwmExtendFrameIntoClientArea(_hwnd, ref margins);

            RemoveExStyle(Win32Interop.WS_EX_LAYERED);
        }

        public void SetClickThrough(bool enabled)
        {
            if (!IsAvailable || _clickThrough == enabled) return;

            _clickThrough = enabled;
            if (enabled)
                AddExStyle(Win32Interop.WS_EX_TRANSPARENT);
            else
                RemoveExStyle(Win32Interop.WS_EX_TRANSPARENT);
        }

        public void SetTopmost(bool enabled)
        {
            if (!IsAvailable) return;

            Win32Interop.SetWindowPos(
                _hwnd,
                enabled ? Win32Interop.HWND_TOPMOST : Win32Interop.HWND_NOTOPMOST,
                0, 0, 0, 0,
                Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE);
        }

        public void SetHiddenFromTaskbar(bool hidden)
        {
            if (!IsAvailable || _hiddenFromTaskbar == hidden) return;

            _hiddenFromTaskbar = hidden;

            // The taskbar only re-reads WS_EX_TOOLWINDOW when the window is re-shown, so the
            // hide/show cycle around the style change is required, not defensive.
            Win32Interop.ShowWindow(_hwnd, Win32Interop.SW_HIDE);

            var exStyle = Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE);
            if (hidden)
                exStyle = (exStyle | Win32Interop.WS_EX_TOOLWINDOW) & ~Win32Interop.WS_EX_APPWINDOW;
            else
                exStyle = (exStyle & ~Win32Interop.WS_EX_TOOLWINDOW) | Win32Interop.WS_EX_APPWINDOW;
            Win32Interop.SetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE, exStyle);

            // SW_SHOWNA keeps us from stealing focus on the way back up.
            Win32Interop.ShowWindow(_hwnd, Win32Interop.SW_SHOWNA);
        }

        public bool TryGetCursorPosition(out Vector2 unityScreenPosition)
        {
            unityScreenPosition = default;
            if (!IsAvailable) return false;

            if (!Win32Interop.GetCursorPos(out var point)) return false;
            if (!Win32Interop.ScreenToClient(_hwnd, ref point)) return false;
            if (!Win32Interop.GetClientRect(_hwnd, out var client)) return false;

            var width = client.Right - client.Left;
            var height = client.Bottom - client.Top;
            if (width <= 0 || height <= 0) return false;

            if (point.X < 0 || point.Y < 0 || point.X >= width || point.Y >= height)
                return false;

            // Win32 client space is top-left origin; Unity screen space is bottom-left.
            unityScreenPosition = new Vector2(point.X, height - point.Y);
            return true;
        }

        private void AddExStyle(uint flag)
        {
            var exStyle = Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE);
            if ((exStyle & flag) != 0) return;
            Win32Interop.SetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE, exStyle | flag);
        }

        private void RemoveExStyle(uint flag)
        {
            var exStyle = Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE);
            if ((exStyle & flag) == 0) return;
            Win32Interop.SetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE, exStyle & ~flag);
        }

        /// <summary>
        /// GetActiveWindow only works when the player already owns the foreground, which is not
        /// guaranteed at startup, so fall back to scanning this process's own top-level windows.
        /// </summary>
        private static IntPtr ResolveWindowHandle()
        {
            var active = Win32Interop.GetActiveWindow();
            if (active != IntPtr.Zero && IsOwnUnityWindow(active))
                return active;

            var found = IntPtr.Zero;
            Win32Interop.EnumWindows((hWnd, _) =>
            {
                if (!IsOwnUnityWindow(hWnd)) return true;
                found = hWnd;
                return false; // stop enumerating
            }, IntPtr.Zero);

            return found;
        }

        private static bool IsOwnUnityWindow(IntPtr hWnd)
        {
            if (!Win32Interop.IsWindowVisible(hWnd)) return false;

            Win32Interop.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId != CurrentProcessId) return false;

            var buffer = new StringBuilder(256);
            Win32Interop.GetClassName(hWnd, buffer, buffer.Capacity);
            return buffer.ToString() == UnityWindowClassName;
        }
    }
}
#endif
