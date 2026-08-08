#if UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Text;

namespace gishadev.companion.Window.Native
{
    /// <summary>
    /// Reads the foreground window through user32. Unlike <see cref="Win32PlatformWindow"/> this only
    /// observes, never mutates, so <see cref="PlatformWindowFactory"/> also creates it in the editor.
    /// </summary>
    public sealed class Win32ForegroundWindowProvider : IForegroundWindowProvider
    {
        private const int TitleBufferCapacity = 512;

        private static readonly uint CurrentProcessId = (uint)Process.GetCurrentProcess().Id;

        private readonly StringBuilder _titleBuffer = new StringBuilder(TitleBufferCapacity);

        // Process lookup is the expensive part of a poll and the answer cannot change for a live
        // handle, so it is cached. The pid is part of the key because Windows recycles HWNDs.
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

        /// <summary>
        /// Note that every UWP app reports "applicationframehost", so they all collapse into a single
        /// entry as far as classification is concerned.
        /// </summary>
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
                // Exited between the handle read and this call.
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        // Re-read every poll rather than cached with the handle: a browser changes its title on every
        // tab switch without the window ever changing.
        private string ReadTitle(IntPtr hwnd)
        {
            if (Win32Interop.GetWindowTextLength(hwnd) <= 0) return string.Empty;

            var copied = Win32Interop.GetWindowText(hwnd, _titleBuffer, TitleBufferCapacity);
            return copied > 0 ? _titleBuffer.ToString() : string.Empty;
        }
    }
}
#endif
