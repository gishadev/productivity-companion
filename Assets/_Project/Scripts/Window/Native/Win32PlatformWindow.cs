#if UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace gishadev.companion.Window.Native
{
    public sealed class Win32PlatformWindow : IPlatformWindow
    {
        private const string UnityWindowClassName = "UnityWndClass";

        private static readonly uint CurrentProcessId = (uint)Process.GetCurrentProcess().Id;

        private readonly IntPtr _hwnd;

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

        public bool IsTopmost => IsAvailable && HasTopmostFlag(_hwnd);

        // Topmost windows always precede non-topmost ones in z-order, so a visible non-topmost window above us
        // means the flag and the real position disagree. Windows 10 leaves this state behind after shell UI.
        public bool IsCoveredByNonTopmostWindow
        {
            get
            {
                if (!IsAvailable) return false;

                const int maxSteps = 1024;
                var above = Win32Interop.GetWindow(_hwnd, Win32Interop.GW_HWNDPREV);
                for (var i = 0; above != IntPtr.Zero && i < maxSteps; i++)
                {
                    if (Win32Interop.IsWindowVisible(above) && !HasTopmostFlag(above))
                        return true;
                    above = Win32Interop.GetWindow(above, Win32Interop.GW_HWNDPREV);
                }

                return false;
            }
        }

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
            // SWP_FRAMECHANGED forces the cached non-client area to be recomputed.
            Win32Interop.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE |
                Win32Interop.SWP_FRAMECHANGED);
        }

        public void FitToMonitor()
        {
            if (!IsAvailable) return;

            var monitor = Win32Interop.MonitorFromWindow(_hwnd, Win32Interop.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return;

            var info = new Win32Interop.MONITORINFO { cbSize = Marshal.SizeOf<Win32Interop.MONITORINFO>() };
            if (!Win32Interop.GetMonitorInfo(monitor, ref info)) return;

            // rcMonitor, not rcWork: covering the taskbar is harmless while click-through.
            var bounds = info.rcMonitor;
            Win32Interop.SetWindowPos(_hwnd, IntPtr.Zero,
                bounds.Left, bounds.Top,
                bounds.Right - bounds.Left, bounds.Bottom - bounds.Top,
                Win32Interop.SWP_NOZORDER | Win32Interop.SWP_NOACTIVATE);
        }

        public void ApplyPerPixelAlpha()
        {
            if (!IsAvailable) return;

            if (Win32Interop.DwmIsCompositionEnabled(out var composited) == 0 && !composited)
                UnityEngine.Debug.LogWarning(
                    "[Window] DWM composition is disabled; per-pixel alpha will not work. Use the ColorKey mode instead.");

            // Required: WS_EX_TRANSPARENT only passes clicks reliably on a layered window, and a layered window
            // without SetLayeredWindowAttributes may not paint.
            AddExStyle(Win32Interop.WS_EX_LAYERED);
            Win32Interop.SetLayeredWindowAttributes(_hwnd, 0, 255, Win32Interop.LWA_ALPHA);

            var margins = new Win32Interop.MARGINS
            {
                cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1
            };
            Win32Interop.DwmExtendFrameIntoClientArea(_hwnd, ref margins);
        }

        public void ApplyColorKey(Color32 key)
        {
            if (!IsAvailable) return;

            // Collapse any glass frame left from PerPixelAlpha, or it keeps punching holes.
            var margins = new Win32Interop.MARGINS();
            Win32Interop.DwmExtendFrameIntoClientArea(_hwnd, ref margins);

            AddExStyle(Win32Interop.WS_EX_LAYERED);
            // COLORREF is 0x00BBGGRR.
            var colorRef = (uint)(key.r | (key.g << 8) | (key.b << 16));
            Win32Interop.SetLayeredWindowAttributes(_hwnd, colorRef, 0, Win32Interop.LWA_COLORKEY);
        }

        public void ClearLayered()
        {
            if (!IsAvailable) return;

            // Collapse the DWM frame before dropping WS_EX_LAYERED, or a transparent border lingers.
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

            SetZOrder(enabled ? Win32Interop.HWND_TOPMOST : Win32Interop.HWND_NOTOPMOST);

            // Windows 10 can ignore HWND_TOPMOST on a window it already considers topmost; cycling the band
            // forces it to recompute our position.
            if (enabled && IsCoveredByNonTopmostWindow)
            {
                SetZOrder(Win32Interop.HWND_NOTOPMOST);
                SetZOrder(Win32Interop.HWND_TOPMOST);
            }
        }

        private void SetZOrder(IntPtr insertAfter)
        {
            Win32Interop.SetWindowPos(_hwnd, insertAfter, 0, 0, 0, 0,
                Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE);
        }

        private static bool HasTopmostFlag(IntPtr hWnd) =>
            (Win32Interop.GetWindowLong(hWnd, Win32Interop.GWL_EXSTYLE) & Win32Interop.WS_EX_TOPMOST) != 0;

        public void SetHiddenFromTaskbar(bool hidden)
        {
            if (!IsAvailable || _hiddenFromTaskbar == hidden) return;

            _hiddenFromTaskbar = hidden;

            // The taskbar only re-reads WS_EX_TOOLWINDOW on re-show.
            Win32Interop.ShowWindow(_hwnd, Win32Interop.SW_HIDE);

            var exStyle = Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE);
            if (hidden)
                exStyle = (exStyle | Win32Interop.WS_EX_TOOLWINDOW) & ~Win32Interop.WS_EX_APPWINDOW;
            else
                exStyle = (exStyle & ~Win32Interop.WS_EX_TOOLWINDOW) | Win32Interop.WS_EX_APPWINDOW;
            Win32Interop.SetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE, exStyle);

            // SW_SHOWNA: don't steal focus.
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

        // GetActiveWindow fails if we aren't foreground at startup.
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
