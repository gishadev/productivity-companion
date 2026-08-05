using System;
using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// The single place that reacts to <see cref="WindowSettings"/> changes; everything else just flips
    /// settings. The two entry point phases are not interchangeable: IInitializable runs as the
    /// container builds, where chrome must be stripped before a frame is drawn; IStartable runs on the
    /// first player loop, once <see cref="Camera.main"/> resolves, which is what transparency needs.
    /// </summary>
    public sealed class WindowController : IInitializable, IStartable, IDisposable
    {
        // Only 0 and 255 per channel: the project renders in Linear space, so the clear color round-trips
        // sRGB→linear→sRGB before reaching the window surface. Intermediate values come back off by a
        // unit and never match, and LWA_COLORKEY compares exactly.
        public static readonly Color32 DefaultColorKey = new Color32(255, 0, 255, 255);

        private readonly IPlatformWindow _window;
        private readonly WindowSettings _settings;
        private readonly RenderThrottle _renderThrottle;
        private readonly TopmostWatchdog _watchdog;

        private Camera _targetCamera;
        private CameraClearFlags _originalClearFlags;
        private Color _originalBackground;
        private bool _cameraStateCaptured;

        public WindowController(
            IPlatformWindow window,
            WindowSettings settings,
            RenderThrottle renderThrottle,
            TopmostWatchdog watchdog)
        {
            _window = window;
            _settings = settings;
            _renderThrottle = renderThrottle;
            _watchdog = watchdog;
        }

        public Color32 ColorKey { get; set; } = DefaultColorKey;

        /// <summary>
        /// Camera whose background becomes the transparent region. Resolved lazily so scene loads that
        /// swap cameras still work.
        /// </summary>
        public Camera TargetCamera
        {
            get
            {
                if (_targetCamera == null)
                {
                    _targetCamera = Camera.main;
                    _cameraStateCaptured = false;
                }

                return _targetCamera;
            }
            set
            {
                if (_targetCamera == value) return;
                _targetCamera = value;
                _cameraStateCaptured = false;
                ApplyTransparency();
            }
        }

        void IInitializable.Initialize()
        {
            // The widget must keep ticking while unfocused; that is the entire point of the app.
            Application.runInBackground = true;

            // Unity persists the last screen mode in the registry, so a previously fullscreen run would
            // survive the project default. A layered window has to be windowed.
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
                Screen.fullScreenMode = FullScreenMode.Windowed;

            _window.RemoveChrome();

            _settings.Changed += OnSettingChanged;
            Application.focusChanged += OnApplicationFocus;
        }

        void IStartable.Start() => ApplyAll();

        public void Dispose()
        {
            _settings.Changed -= OnSettingChanged;
            Application.focusChanged -= OnApplicationFocus;
        }

        public void ApplyAll()
        {
            ApplyFrameRate();
            ApplyDisplaySleep();
            ApplyTransparency();
            ApplyAlwaysOnTop();
            ApplyHideFromTaskbar();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _renderThrottle.SetFocused(hasFocus);

            // Regaining focus usually means the taskbar or another window just had it, which is
            // exactly when Windows will have dropped our topmost flag.
            if (hasFocus && _settings.AlwaysOnTop)
                _watchdog.CheckNow();
        }

        private void OnSettingChanged(WindowSetting setting)
        {
            switch (setting)
            {
                case WindowSetting.Transparency:
                    ApplyTransparency();
                    break;
                case WindowSetting.ClickThrough:
                    // ClickThroughController reads the setting directly each frame.
                    break;
                case WindowSetting.AlwaysOnTop:
                    ApplyAlwaysOnTop();
                    break;
                case WindowSetting.HideFromTaskbar:
                    ApplyHideFromTaskbar();
                    break;
                case WindowSetting.TargetFrameRate:
                    ApplyFrameRate();
                    break;
                case WindowSetting.PreventDisplaySleep:
                    ApplyDisplaySleep();
                    break;
            }
        }

        private void ApplyFrameRate()
        {
            Application.targetFrameRate = _settings.TargetFrameRate;
        }

        private void ApplyDisplaySleep()
        {
            Screen.sleepTimeout = _settings.PreventDisplaySleep
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;
        }

        private void ApplyTransparency()
        {
            var camera = TargetCamera;
            if (camera != null && !_cameraStateCaptured)
            {
                _originalClearFlags = camera.clearFlags;
                _originalBackground = camera.backgroundColor;
                _cameraStateCaptured = true;
            }

            switch (_settings.TransparencyMode)
            {
                case TransparencyMode.PerPixelAlpha:
                    SetCameraBackground(camera, new Color(0f, 0f, 0f, 0f));
                    _window.ApplyPerPixelAlpha();
                    break;

                case TransparencyMode.ColorKey:
                    SetCameraBackground(camera, ColorKey);
                    _window.ApplyColorKey(ColorKey);
                    break;

                default:
                    if (camera != null && _cameraStateCaptured)
                    {
                        camera.clearFlags = _originalClearFlags;
                        camera.backgroundColor = _originalBackground;
                    }

                    _window.ClearLayered();
                    break;
            }
        }

        private static void SetCameraBackground(Camera camera, Color color)
        {
            if (camera == null) return;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = color;
        }

        private void ApplyAlwaysOnTop()
        {
            var enabled = _settings.AlwaysOnTop;
            _window.SetTopmost(enabled);

            if (enabled)
                _watchdog.StartWatching();
            else
                _watchdog.StopWatching();
        }

        private void ApplyHideFromTaskbar()
        {
            _window.SetHiddenFromTaskbar(_settings.HideFromTaskbar);
        }
    }
}
