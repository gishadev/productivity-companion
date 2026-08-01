using gishadev.companion.Window.Native;
using UnityEngine;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Applies <see cref="WindowSettings"/> to the actual window and to Unity's own player state.
    /// The single place that reacts to setting changes; everything else just flips settings.
    /// </summary>
    public sealed class WindowController : MonoBehaviour
    {
        /// <summary>
        /// Reserved key color for <see cref="TransparencyMode.ColorKey"/>: any pixel matching it
        /// exactly is punched out.
        /// </summary>
        /// <remarks>
        /// Only 0 and 255 are used per channel, and that is not cosmetic. The project renders in
        /// Linear color space, so the clear color makes a round trip through sRGB→linear→sRGB before
        /// it reaches the window surface. Intermediate values (254 was the first attempt here) come
        /// back off by a unit and never match the key, which LWA_COLORKEY compares exactly. 0 and 255
        /// are the only values guaranteed to survive that round trip.
        /// </remarks>
        public static readonly Color32 DefaultColorKey = new Color32(255, 0, 255, 255);

        private IPlatformWindow _window;
        private WindowSettings _settings;
        private RenderThrottle _renderThrottle;
        private TopmostWatchdog _watchdog;

        private Camera _targetCamera;
        private CameraClearFlags _originalClearFlags;
        private Color _originalBackground;
        private bool _cameraStateCaptured;

        /// <summary>Called by <see cref="WindowBootstrap"/> before this component is enabled.</summary>
        public void Initialize(IPlatformWindow window, WindowSettings settings, RenderThrottle renderThrottle)
        {
            _window = window;
            _settings = settings;
            _renderThrottle = renderThrottle;
        }

        /// <summary>Color punched out in <see cref="TransparencyMode.ColorKey"/> mode.</summary>
        public Color32 ColorKey { get; set; } = DefaultColorKey;

        /// <summary>
        /// Camera whose background becomes the transparent region. Defaults to <see cref="Camera.main"/>,
        /// resolved lazily so scene loads that swap cameras still work.
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

        private void Awake()
        {
            _watchdog = GetComponent<TopmostWatchdog>();

            // The widget must keep ticking while unfocused; that is the entire point of the app.
            Application.runInBackground = true;

            // Unity persists the last screen mode in the registry, so a previously fullscreen run
            // would survive the project default. A layered window has to be windowed.
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
                Screen.fullScreenMode = FullScreenMode.Windowed;

            _window.RemoveChrome();
        }

        private void OnEnable()
        {
            _settings.Changed += OnSettingChanged;
        }

        private void OnDisable()
        {
            _settings.Changed -= OnSettingChanged;
        }

        private void Start()
        {
            ApplyAll();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _renderThrottle.SetFocused(hasFocus);

            // Regaining focus usually means the taskbar or another window just had it, which is
            // exactly when Windows will have dropped our topmost flag.
            if (hasFocus && _settings.AlwaysOnTop)
                _watchdog?.CheckNow();
        }

        public void ApplyAll()
        {
            ApplyFrameRate();
            ApplyDisplaySleep();
            ApplyTransparency();
            ApplyAlwaysOnTop();
            ApplyHideFromTaskbar();
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

            if (_watchdog == null) return;

            // The watchdog exists purely to defend the topmost flag; don't run it when off.
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
