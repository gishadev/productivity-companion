using System.Collections;
using gishadev.companion.Window.Native;
using UnityEngine;

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
    /// </remarks>
    public sealed class TopmostWatchdog : MonoBehaviour
    {
        private const float CheckInterval = 0.5f;

        private IPlatformWindow _window;

        private Coroutine _routine;

        /// <summary>Called by <see cref="WindowBootstrap"/> before this component is enabled.</summary>
        public void Initialize(IPlatformWindow window) => _window = window;

        public bool IsRunning => _routine != null;

        public void StartWatching()
        {
            if (_routine != null || _window == null || !_window.IsAvailable) return;
            _routine = StartCoroutine(WatchRoutine());
        }

        public void StopWatching()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
        }

        /// <summary>Immediate out-of-band check, e.g. right after the app regains focus.</summary>
        public void CheckNow()
        {
            if (_window == null || !_window.IsAvailable) return;
            if (_window.IsTopmost && !_window.IsTaskbarForeground) return;
            _window.SetTopmost(true);
        }

        private void OnDisable() => StopWatching();

        private IEnumerator WatchRoutine()
        {
            var wait = new WaitForSecondsRealtime(CheckInterval);
            while (true)
            {
                CheckNow();
                yield return wait;
            }
        }
    }
}
