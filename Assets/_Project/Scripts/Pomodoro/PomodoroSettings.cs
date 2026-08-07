using System;
using gishadev.tools.SavingSystem;
using UnityEngine;

namespace gishadev.companion.Pomodoro
{
    /// <summary>Persisted Pomodoro durations and flow options.</summary>
    public sealed class PomodoroSettings
    {
        public const int MinMinutes = 1;
        public const int MaxMinutes = 999;

        private const string SaveKey = "pomodoro.settings";

        private readonly ISaverSystem _saver;
        private readonly State _state;

        public PomodoroSettings(ISaverSystem saver)
        {
            _saver = saver;
            _state = LoadState(saver);
        }

        /// <summary>Raised after any value is persisted.</summary>
        public event Action Changed;

        public int WorkMinutes
        {
            get => _state.workMinutes;
            set => SetClamped(ref _state.workMinutes, value);
        }

        public int ShortBreakMinutes
        {
            get => _state.shortBreakMinutes;
            set => SetClamped(ref _state.shortBreakMinutes, value);
        }

        public int LongBreakMinutes
        {
            get => _state.longBreakMinutes;
            set => SetClamped(ref _state.longBreakMinutes, value);
        }

        /// <summary>Number of work sessions before a long break replaces the short one.</summary>
        public int CyclesBeforeLongBreak
        {
            get => _state.cyclesBeforeLongBreak;
            set => SetClamped(ref _state.cyclesBeforeLongBreak, value);
        }

        /// <summary>Starts the next work phase on its own once a break finishes.</summary>
        public bool AutoStartPomodoros
        {
            get => _state.autoStartPomodoros;
            set => SetBool(ref _state.autoStartPomodoros, value);
        }

        /// <summary>Starts the break on its own once a work phase finishes.</summary>
        public bool AutoStartBreaks
        {
            get => _state.autoStartBreaks;
            set => SetBool(ref _state.autoStartBreaks, value);
        }

        /// <summary>Configured length of the given phase.</summary>
        public TimeSpan DurationOf(PomodoroPhase phase) => phase switch
        {
            PomodoroPhase.ShortBreak => TimeSpan.FromMinutes(ShortBreakMinutes),
            PomodoroPhase.LongBreak => TimeSpan.FromMinutes(LongBreakMinutes),
            _ => TimeSpan.FromMinutes(WorkMinutes)
        };

        /// <summary>Whether the phase following <paramref name="completed"/> may start without the user.</summary>
        public bool ShouldAutoStartAfter(PomodoroPhase completed) =>
            completed == PomodoroPhase.Work ? AutoStartBreaks : AutoStartPomodoros;

        private void SetClamped(ref int field, int value)
        {
            var clamped = Mathf.Clamp(value, MinMinutes, MaxMinutes);
            if (field == clamped) return;

            field = clamped;
            Persist();
        }

        private void SetBool(ref bool field, bool value)
        {
            if (field == value) return;

            field = value;
            Persist();
        }

        private void Persist()
        {
            _saver.Save(SaveKey, JsonUtility.ToJson(_state));
            Changed?.Invoke();
        }

        private static State LoadState(ISaverSystem saver)
        {
            if (!saver.TryLoad(SaveKey, out var json) || string.IsNullOrEmpty(json))
                return new State();

            var loaded = JsonUtility.FromJson<State>(json);
            if (loaded == null) return new State();

            loaded.workMinutes = Mathf.Clamp(loaded.workMinutes, MinMinutes, MaxMinutes);
            loaded.shortBreakMinutes = Mathf.Clamp(loaded.shortBreakMinutes, MinMinutes, MaxMinutes);
            loaded.longBreakMinutes = Mathf.Clamp(loaded.longBreakMinutes, MinMinutes, MaxMinutes);
            loaded.cyclesBeforeLongBreak = Mathf.Clamp(loaded.cyclesBeforeLongBreak, MinMinutes, MaxMinutes);
            return loaded;
        }

        // Field initializers double as the defaults: JsonUtility leaves anything the saved blob is
        // missing untouched, so an older file gains new settings rather than zeroing them.
        [Serializable]
        private sealed class State
        {
            public int workMinutes = 25;
            public int shortBreakMinutes = 5;
            public int longBreakMinutes = 15;
            public int cyclesBeforeLongBreak = 4;
            public bool autoStartPomodoros = true;
            public bool autoStartBreaks = true;
        }
    }
}
