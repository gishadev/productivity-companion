using System;
using UnityEngine;

namespace gishadev.companion.Window
{
    public sealed class WindowSettings
    {
        private const string KeyPrefix = "window.";

        private const string TransparencyKey = KeyPrefix + "transparencyMode";
        private const string ClickThroughKey = KeyPrefix + "clickThrough";
        private const string AlwaysOnTopKey = KeyPrefix + "alwaysOnTop";
        private const string HideFromTaskbarKey = KeyPrefix + "hideFromTaskbar";
        private const string TargetFrameRateKey = KeyPrefix + "targetFrameRate";
        private const string PreventDisplaySleepKey = KeyPrefix + "preventDisplaySleep";

        public static readonly int[] AllowedFrameRates = { 15, 30, 60 };

        public const TransparencyMode DefaultTransparencyMode = TransparencyMode.PerPixelAlpha;

        // Transparent without click-through would lock the user out of their desktop.
        public const bool DefaultClickThrough = true;

        // A widget that sinks behind other apps is useless; the only other toggle is the dev-only F3 hotkey.
        public const bool DefaultAlwaysOnTop = true;

        private TransparencyMode _transparencyMode;
        private bool _clickThrough;
        private bool _alwaysOnTop;
        private bool _hideFromTaskbar;
        private int _targetFrameRate;
        private bool _preventDisplaySleep;

        public WindowSettings()
        {
            _transparencyMode =
                (TransparencyMode)PlayerPrefs.GetInt(TransparencyKey, (int)DefaultTransparencyMode);
            _clickThrough = GetBool(ClickThroughKey, DefaultClickThrough);
            _alwaysOnTop = GetBool(AlwaysOnTopKey, DefaultAlwaysOnTop);
            _hideFromTaskbar = GetBool(HideFromTaskbarKey, false);
            _targetFrameRate = SanitizeFrameRate(PlayerPrefs.GetInt(TargetFrameRateKey, 60));
            _preventDisplaySleep = GetBool(PreventDisplaySleepKey, false);
        }

        public event Action<WindowSetting> Changed;

        public TransparencyMode TransparencyMode
        {
            get => _transparencyMode;
            set
            {
                if (_transparencyMode == value) return;
                _transparencyMode = value;
                PlayerPrefs.SetInt(TransparencyKey, (int)value);
                Persist(WindowSetting.Transparency);
            }
        }

        public bool ClickThrough
        {
            get => _clickThrough;
            set
            {
                if (_clickThrough == value) return;
                _clickThrough = value;
                SetBool(ClickThroughKey, value);
                Persist(WindowSetting.ClickThrough);
            }
        }

        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (_alwaysOnTop == value) return;
                _alwaysOnTop = value;
                SetBool(AlwaysOnTopKey, value);
                Persist(WindowSetting.AlwaysOnTop);
            }
        }

        public bool HideFromTaskbar
        {
            get => _hideFromTaskbar;
            set
            {
                if (_hideFromTaskbar == value) return;
                _hideFromTaskbar = value;
                SetBool(HideFromTaskbarKey, value);
                Persist(WindowSetting.HideFromTaskbar);
            }
        }

        // Snaps to the nearest AllowedFrameRates entry.
        public int TargetFrameRate
        {
            get => _targetFrameRate;
            set
            {
                var sanitized = SanitizeFrameRate(value);
                if (_targetFrameRate == sanitized) return;
                _targetFrameRate = sanitized;
                PlayerPrefs.SetInt(TargetFrameRateKey, sanitized);
                Persist(WindowSetting.TargetFrameRate);
            }
        }

        public bool PreventDisplaySleep
        {
            get => _preventDisplaySleep;
            set
            {
                if (_preventDisplaySleep == value) return;
                _preventDisplaySleep = value;
                SetBool(PreventDisplaySleepKey, value);
                Persist(WindowSetting.PreventDisplaySleep);
            }
        }

        private void Persist(WindowSetting setting)
        {
            PlayerPrefs.Save();
            Changed?.Invoke(setting);
        }

        private static int SanitizeFrameRate(int value)
        {
            var best = AllowedFrameRates[0];
            var bestDistance = int.MaxValue;
            foreach (var candidate in AllowedFrameRates)
            {
                var distance = Mathf.Abs(candidate - value);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        private static bool GetBool(string key, bool defaultValue) =>
            PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;

        private static void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
    }
}
