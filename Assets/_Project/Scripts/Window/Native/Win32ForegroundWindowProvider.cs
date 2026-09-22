#if UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Text;

namespace gishadev.companion.Window.Native
{
    public sealed class Win32ForegroundWindowProvider : IForegroundWindowProvider
    {
        private const int TitleBufferCapacity = 512;

        private static readonly uint CurrentProcessId = (uint)Process.GetCurrentProcess().Id;

        private readonly StringBuilder _titleBuffer = new StringBuilder(TitleBufferCapacity);

        // Process lookup is cached per handle; pid is part of the key because HWNDs get recycled.
        private IntPtr _cachedHwnd;
        private uint _cachedProcessId;
        private string _cachedProcessName;

        public bool IsAvailable => true;

        public ForegroundWindowInfo GetForegroundWindow()
        {
            var hwnd = Win32Interop.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return ForegroundWindowInfo.Invalid;

            Win32Interop.GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0) return ForegroundWindowInfo.Invalid;

            if (hwnd != _cachedHwnd || processId != _cachedProcessId)
            {
                _cachedHwnd = hwnd;
                _cachedProcessId = processId;
                _cachedProcessName = ResolveProcessName(processId);
            }

            if (string.IsNullOrEmpty(_cachedProcessName)) return ForegroundWindowInfo.Invalid;

            return new ForegroundWindowInfo(_cachedProcessName, ReadTitle(hwnd), processId == CurrentProcessId);
        }

        // Every UWP app reports "applicationframehost".
        private static string ResolveProcessName(uint processId)
        {
            try
            {
                using var process = Process.GetProcessById((int)processId);
                var name = process.ProcessName;
                return string.IsNullOrEmpty(name) ? null : name.ToLowerInvariant();
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        // Not cached: browsers retitle on every tab switch.
        private string ReadTitle(IntPtr hwnd)
        {
            if (Win32Interop.GetWindowTextLength(hwnd) <= 0) return string.Empty;

            var copied = Win32Interop.GetWindowText(hwnd, _titleBuffer, TitleBufferCapacity);
            return copied > 0 ? _titleBuffer.ToString() : string.Empty;
        }
    }
}
#endif
