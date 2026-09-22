using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    // Windows drops WS_EX_TOPMOST when the taskbar or Start menu takes focus. Re-assert only when lost:
    // SetWindowPos every frame fights the shell.
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

            if (Time.unscaledTime < _nextCheck) return;

            _nextCheck = Time.unscaledTime + CheckInterval;
            CheckNow();
        }
    }
}
