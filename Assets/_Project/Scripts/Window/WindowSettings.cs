using System;
using UnityEngine;

namespace gishadev.companion.Window
{
    /// <summary>Persisted, independently toggleable window behaviors.</summary>
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

        // Tied to the transparency default: the window covers the whole screen, so transparent-without-
        // click-through would leave the user unable to interact with their desktop at all.
        public const bool DefaultClickThrough = true;

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
            _alwaysOnTop = GetBool(AlwaysOnTopKey, false);
            _hideFromTaskbar = GetBool(HideFromTaskbarKey, false);
            _targetFrameRate = SanitizeFrameRate(PlayerPrefs.GetInt(TargetFrameRateKey, 60));
            _preventDisplaySleep = GetBool(PreventDisplaySleepKey, false);
        }

        /// <summary>Raised after a setting is persisted, carrying the one that changed.</summary>
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

        /// <summary>Clamped to <see cref="AllowedFrameRates"/>; out-of-range values snap to the nearest.</summary>
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
