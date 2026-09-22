using System;
using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    // Initialize strips chrome before the first frame; Start handles transparency once Camera.main resolves.
    public sealed class WindowController : IInitializable, IStartable, IDisposable
    {
        // Channels only 0 or 255: Linear color space round-trips the clear colour, and LWA_COLORKEY matches exactly.
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
            Application.runInBackground = true;

            // Unity persists the last screen mode; a layered window must be windowed.
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
                Screen.fullScreenMode = FullScreenMode.Windowed;

            _window.RemoveChrome();

            // Windowed mode inherits the fullscreen resolution plus a frame.
            _window.FitToMonitor();

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
            // Again: a fullscreen mode change applies at end of frame.
            _window.FitToMonitor();

            ApplyFrameRate();
            ApplyDisplaySleep();
            ApplyTransparency();
            ApplyAlwaysOnTop();
            ApplyHideFromTaskbar();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _renderThrottle.SetFocused(hasFocus);

            // Catches resolution/DPI changes.
            if (hasFocus) _window.FitToMonitor();

            // Windows drops topmost whenever something else had focus.
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
                    // ClickThroughController reads it every frame.
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
