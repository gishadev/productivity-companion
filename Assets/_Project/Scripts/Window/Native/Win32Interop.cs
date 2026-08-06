#if UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;

namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// Raw Win32 surface. Deliberately free of Unity types so it stays trivially auditable.
    /// Nothing outside <see cref="Native"/> should reference this type.
    /// </summary>
    internal static class Win32Interop
    {
        // --- Window style indices -------------------------------------------------
        internal const int GWL_STYLE = -16;
        internal const int GWL_EXSTYLE = -20;

        // --- Extended styles ------------------------------------------------------
        internal const uint WS_EX_TOPMOST = 0x00000008;
        internal const uint WS_EX_TRANSPARENT = 0x00000020;
        internal const uint WS_EX_TOOLWINDOW = 0x00000080;
        internal const uint WS_EX_APPWINDOW = 0x00040000;
        internal const uint WS_EX_LAYERED = 0x00080000;

        // --- Window styles --------------------------------------------------------
        internal const uint WS_CAPTION = 0x00C00000;
        internal const uint WS_THICKFRAME = 0x00040000;
        internal const uint WS_MINIMIZEBOX = 0x00020000;
        internal const uint WS_MAXIMIZEBOX = 0x00010000;
        internal const uint WS_SYSMENU = 0x00080000;
        internal const uint WS_POPUP = 0x80000000;

        // --- SetLayeredWindowAttributes flags -------------------------------------
        internal const uint LWA_COLORKEY = 0x00000001;
        internal const uint LWA_ALPHA = 0x00000002;

        // --- SetWindowPos ---------------------------------------------------------
        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_FRAMECHANGED = 0x0020;

        // --- MonitorFromWindow ----------------------------------------------------
        internal const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        // --- ShowWindow -----------------------------------------------------------
        internal const int SW_HIDE = 0;
        internal const int SW_SHOW = 5;
        internal const int SW_SHOWNA = 8;

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // --- user32 ---------------------------------------------------------------
        [DllImport("user32.dll")]
        internal static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        internal static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

        [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        // GetWindowLongPtr/SetWindowLongPtr are only exported on 64-bit user32. The 32-bit
        // entry points are resolved lazily on first call, so dispatching on IntPtr.Size is safe.
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        internal static uint GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8
                ? (uint)GetWindowLongPtr64(hWnd, nIndex).ToInt64()
                : unchecked((uint)GetWindowLong32(hWnd, nIndex));
        }

        internal static void SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong)
        {
            if (IntPtr.Size == 8)
                // Widen through long, not int: WS_POPUP (0x80000000) is negative as an int and would
                // sign-extend into the upper 32 bits of the style word.
                SetWindowLongPtr64(hWnd, nIndex, new IntPtr((long)dwNewLong));
            else
                SetWindowLong32(hWnd, nIndex, unchecked((int)dwNewLong));
        }

        // --- dwmapi ---------------------------------------------------------------
        [DllImport("dwmapi.dll")]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmIsCompositionEnabled(out bool enabled);
    }
}
#endif
