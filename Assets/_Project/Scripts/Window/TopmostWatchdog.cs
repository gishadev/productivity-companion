using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Re-asserts WS_EX_TOPMOST, which Windows silently drops whenever the taskbar or Start menu takes
    /// the foreground. Low cadence and only when actually lost — calling SetWindowPos every frame
    /// fights the shell's own z-order handling.
    /// </summary>
    public sealed class TopmostWatchdog : ITickable
    {
        private const float CheckInterval = 0.5f;

        private readonly IPlatformWindow _window;

        private float _nextCheck;

        public TopmostWatchdog(IPlatformWindow window)
        {
            _window = window;
        }

        public bool IsRunning { get; private set; }

        public void StartWatching()
        {
            if (IsRunning || !_window.IsAvailable) return;

            IsRunning = true;
            _nextCheck = Time.unscaledTime;
        }

        public void StopWatching() => IsRunning = false;

        public void CheckNow()
        {
            if (!_window.IsAvailable) return;
            if (_window.IsTopmost && !_window.IsTaskbarForeground) return;
            _window.SetTopmost(true);
        }

        void ITickable.Tick()
        {
            if (!IsRunning) return;

            // Unscaled so a paused or slowed timescale cannot stall it.
            if (Time.unscaledTime < _nextCheck) return;

            _nextCheck = Time.unscaledTime + CheckInterval;
            CheckNow();
        }
    }
}
