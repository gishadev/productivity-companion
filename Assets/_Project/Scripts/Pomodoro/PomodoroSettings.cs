using System;
using UnityEngine;

namespace gishadev.companion.Pomodoro
{
    /// <summary>
    /// Persisted Pomodoro durations and flow options. Follows the same PlayerPrefs-backed shape as
    /// <c>Window.WindowSettings</c>; the tools package provides no persistence helper to reuse.
    /// </summary>
    public sealed class PomodoroSettings
    {
        private const string KeyPrefix = "pomodoro.";

        private const string WorkKey = KeyPrefix + "workMinutes";
        private const string ShortBreakKey = KeyPrefix + "shortBreakMinutes";
        private const string LongBreakKey = KeyPrefix + "longBreakMinutes";
        private const string CyclesKey = KeyPrefix + "cyclesBeforeLongBreak";
        private const string AutoAdvanceKey = KeyPrefix + "autoAdvance";

        private int _workMinutes;
        private int _shortBreakMinutes;
        private int _longBreakMinutes;
        private int _cyclesBeforeLongBreak;
        private bool _autoAdvance;

        public PomodoroSettings()
        {
            _workMinutes = Mathf.Max(1, PlayerPrefs.GetInt(WorkKey, 25));
            _shortBreakMinutes = Mathf.Max(1, PlayerPrefs.GetInt(ShortBreakKey, 5));
            _longBreakMinutes = Mathf.Max(1, PlayerPrefs.GetInt(LongBreakKey, 15));
            _cyclesBeforeLongBreak = Mathf.Max(1, PlayerPrefs.GetInt(CyclesKey, 4));
            _autoAdvance = PlayerPrefs.GetInt(AutoAdvanceKey, 1) != 0;
        }

        /// <summary>Raised after any value is persisted.</summary>
        public event Action Changed;

        public int WorkMinutes
        {
            get => _workMinutes;
            set => SetInt(ref _workMinutes, value, WorkKey);
        }

        public int ShortBreakMinutes
        {
            get => _shortBreakMinutes;
            set => SetInt(ref _shortBreakMinutes, value, ShortBreakKey);
        }

        public int LongBreakMinutes
        {
            get => _longBreakMinutes;
            set => SetInt(ref _longBreakMinutes, value, LongBreakKey);
        }

        /// <summary>Number of work sessions before a long break replaces the short one.</summary>
        public int CyclesBeforeLongBreak
        {
            get => _cyclesBeforeLongBreak;
            set => SetInt(ref _cyclesBeforeLongBreak, value, CyclesKey);
        }

        /// <summary>When false, the timer stops at each phase boundary and waits for Start().</summary>
        public bool AutoAdvance
        {
            get => _autoAdvance;
            set
            {
                if (_autoAdvance == value) return;
                _autoAdvance = value;
                PlayerPrefs.SetInt(AutoAdvanceKey, value ? 1 : 0);
                Persist();
            }
        }

        /// <summary>Configured length of the given phase.</summary>
        public TimeSpan DurationOf(PomodoroPhase phase) => phase switch
        {
            PomodoroPhase.ShortBreak => TimeSpan.FromMinutes(_shortBreakMinutes),
            PomodoroPhase.LongBreak => TimeSpan.FromMinutes(_longBreakMinutes),
            _ => TimeSpan.FromMinutes(_workMinutes)
        };

        private void SetInt(ref int field, int value, string key)
        {
            var clamped = Mathf.Max(1, value);
            if (field == clamped) return;
            field = clamped;
            PlayerPrefs.SetInt(key, clamped);
            Persist();
        }

        private void Persist()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
