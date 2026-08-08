#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using gishadev.companion.Focus;
using gishadev.companion.Window.Native;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Development-only toggles and state readout, for exercising window behaviors in a standalone
    /// build before a real settings UI exists. Stripped from release builds. Keyboard input requires
    /// focus, and while the overlay is open it forces the window to stay clickable — otherwise
    /// enabling click-through would make the overlay unreachable with no way to switch it back off.
    /// </summary>
    public sealed class WindowDebugHotkeys : MonoBehaviour
    {
        private WindowSettings _settings;
        private RenderThrottle _renderThrottle;
        private IPlatformWindow _window;
        private ClickThroughController _clickThrough;
        private FocusController _focus;

        /// <summary>Overlay bounds in GUI space (top-left origin). Shared by the draw and the hit test.</summary>
        private static readonly Rect OverlayRect = new Rect(10, 10, 340, 270);

        private bool _overlayVisible = true;
        private IDisposable _overlayLease;
        private IDisposable _overlayBlock;

        private Texture2D _opaqueBackground;

        // Fully opaque so the panel is never color-keyed away or tinted.
        private Texture2D OpaqueBackground
        {
            get
            {
                if (_opaqueBackground != null) return _opaqueBackground;

                _opaqueBackground = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                _opaqueBackground.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.10f, 1f));
                _opaqueBackground.Apply();
                return _opaqueBackground;
            }
        }

        [Inject]
        public void Construct(
            WindowSettings settings,
            RenderThrottle renderThrottle,
            IPlatformWindow window,
            ClickThroughController clickThrough,
            FocusController focus)
        {
            _settings = settings;
            _renderThrottle = renderThrottle;
            _window = window;
            _clickThrough = clickThrough;
            _focus = focus;
        }

        private void Start() => SetOverlayVisible(_overlayVisible);

        private void OnDisable() => SetOverlayVisible(false);

        private void OnDestroy()
        {
            if (_opaqueBackground == null) return;
            Destroy(_opaqueBackground);
            _opaqueBackground = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame) CycleTransparency();
            if (keyboard.f2Key.wasPressedThisFrame) _settings.ClickThrough = !_settings.ClickThrough;
            if (keyboard.f3Key.wasPressedThisFrame) _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
            if (keyboard.f4Key.wasPressedThisFrame) _settings.HideFromTaskbar = !_settings.HideFromTaskbar;
            if (keyboard.f5Key.wasPressedThisFrame) CycleFrameRate();
            if (keyboard.f6Key.wasPressedThisFrame) _settings.PreventDisplaySleep = !_settings.PreventDisplaySleep;
            if (keyboard.f7Key.wasPressedThisFrame) SetOverlayVisible(!_overlayVisible);

            // The keys only arrive while our own window has focus, which is exactly why FocusController
            // ignores it: the target is still the app the user came from.
            if (keyboard.f8Key.wasPressedThisFrame) _focus.TagCurrent(FocusCategory.Productive);
            if (keyboard.f9Key.wasPressedThisFrame) _focus.TagCurrent(FocusCategory.Unproductive);
            if (keyboard.f10Key.wasPressedThisFrame) _focus.TagCurrent(FocusCategory.Regular);

            // Block clicks only over the overlay itself, not the whole window. Blanket blocking made
            // click-through look broken everywhere while the overlay was up.
            SetOverlayBlocking(_overlayVisible && IsCursorOverOverlay());
        }

        private void SetOverlayBlocking(bool blocking)
        {
            if (_clickThrough == null) return;

            if (blocking)
                _overlayBlock ??= _clickThrough.AcquireBlock("debug-overlay");
            else
            {
                _overlayBlock?.Dispose();
                _overlayBlock = null;
            }
        }

        private bool IsCursorOverOverlay()
        {
            if (!_window.TryGetCursorPosition(out var unityScreenPosition)) return false;

            // IMGUI is top-left origin; the Win32 read is converted to Unity's bottom-left origin.
            var guiPosition = new Vector2(unityScreenPosition.x, Screen.height - unityScreenPosition.y);
            return OverlayRect.Contains(guiPosition);
        }

        private void SetOverlayVisible(bool visible)
        {
            _overlayVisible = visible;

            if (!visible)
                SetOverlayBlocking(false);

            if (visible)
            {
                _overlayLease ??= _renderThrottle.AcquireLease("debug-overlay");
            }
            else
            {
                _overlayLease?.Dispose();
                _overlayLease = null;
            }
        }

        private void CycleTransparency()
        {
            _settings.TransparencyMode = _settings.TransparencyMode switch
            {
                TransparencyMode.Off => TransparencyMode.PerPixelAlpha,
                TransparencyMode.PerPixelAlpha => TransparencyMode.ColorKey,
                _ => TransparencyMode.Off
            };
        }

        private string FocusLabel() =>
            string.IsNullOrEmpty(_focus.CurrentProcessName) ? "(none yet)" : _focus.CurrentProcessName;

        private static string Truncate(string value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + "…";

        private void CycleFrameRate()
        {
            var rates = WindowSettings.AllowedFrameRates;
            var index = Array.IndexOf(rates, _settings.TargetFrameRate);
            _settings.TargetFrameRate = rates[(index + 1) % rates.Length];
        }

        private void OnGUI()
        {
            if (!_overlayVisible) return;

            // GUI.skin.box is semi-transparent, so in ColorKey mode it blends with the key color and
            // the whole overlay turns purple. Lay down a fully opaque backing first: opaque pixels
            // never match the key, so the panel stays readable in every transparency mode.
            GUI.DrawTexture(OverlayRect, OpaqueBackground);
            GUILayout.BeginArea(OverlayRect);
            GUILayout.Label("<b>Window debug</b> (F7 hides)", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"F1 Transparency : {_settings.TransparencyMode}");
            GUILayout.Label($"F2 Click-through: {_settings.ClickThrough}");
            GUILayout.Label($"F3 Always on top: {_settings.AlwaysOnTop} (os={_window.IsTopmost})");
            GUILayout.Label($"F4 Hide taskbar : {_settings.HideFromTaskbar}");
            GUILayout.Label($"F5 Target FPS   : {_settings.TargetFrameRate}");
            GUILayout.Label($"F6 Prevent sleep: {_settings.PreventDisplaySleep}");
            GUILayout.Label($"Native window   : {(_window.IsAvailable ? "available" : "UNAVAILABLE")}");
            GUILayout.Label($"Render leases   : {_renderThrottle.ActiveLeaseCount} " +
                            $"(interval={UnityEngine.Rendering.OnDemandRendering.renderFrameInterval})");
            GUILayout.Label($"F8/F9/F10 tag   : {FocusLabel()} [{_focus.CurrentCategory}]");
            GUILayout.Label($"Focus title     : {Truncate(_focus.CurrentTitle, 40)}");
            GUILayout.EndArea();
        }
    }
}
#endif
