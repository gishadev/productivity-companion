using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Keeps the widget on top after Windows silently drops WS_EX_TOPMOST — which it does whenever
    /// the taskbar or Start menu takes the foreground.
    /// </summary>
    /// <remarks>
    /// Runs at a low fixed cadence and only re-asserts when the flag was <em>actually</em> lost.
    /// Blindly calling SetWindowPos every frame is pure waste and can fight the shell's own
    /// z-order handling.
    ///
    /// A plain <see cref="ITickable"/> rather than a MonoBehaviour coroutine: nothing here needs a
    /// transform, and staying out of the scene means the dependency arrives by constructor instead of
    /// an Initialize call. Unscaled time is used so a paused or slowed timescale cannot stall it.
    /// </remarks>
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

            // Due immediately, matching the coroutine this replaced: it checked before its first wait.
            _nextCheck = Time.unscaledTime;
        }

        public void StopWatching() => IsRunning = false;

        /// <summary>Immediate out-of-band check, e.g. right after the app regains focus.</summary>
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
