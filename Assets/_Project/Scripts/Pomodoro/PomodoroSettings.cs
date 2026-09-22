using System;
using gishadev.companion.SavingLoading;
using gishadev.tools.SavingSystem;
using UnityEngine;

namespace gishadev.companion.Pomodoro
{
    public sealed class PomodoroSettings
    {
        public const int MinMinutes = 1;
        public const int MaxMinutes = 999;

        private readonly SaveSlot<PomodoroSettingsData> _slot;
        private readonly PomodoroSettingsData _state;

        public PomodoroSettings(ISaverSystem saver)
        {
            _slot = new SaveSlot<PomodoroSettingsData>(saver, SaveKeys.PomodoroSettings);
            _state = _slot.Load();
            Sanitize(_state);
        }

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

        public int CyclesBeforeLongBreak
        {
            get => _state.cyclesBeforeLongBreak;
            set => SetClamped(ref _state.cyclesBeforeLongBreak, value);
        }

        public bool AutoStartPomodoros
        {
            get => _state.autoStartPomodoros;
            set => SetBool(ref _state.autoStartPomodoros, value);
        }

        public bool AutoStartBreaks
        {
            get => _state.autoStartBreaks;
            set => SetBool(ref _state.autoStartBreaks, value);
        }

        public TimeSpan DurationOf(PomodoroPhase phase) => phase switch
        {
            PomodoroPhase.ShortBreak => TimeSpan.FromMinutes(ShortBreakMinutes),
            PomodoroPhase.LongBreak => TimeSpan.FromMinutes(LongBreakMinutes),
            _ => TimeSpan.FromMinutes(WorkMinutes)
        };

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
            _slot.Save(_state);
            Changed?.Invoke();
        }

        private static void Sanitize(PomodoroSettingsData loaded)
        {
            loaded.workMinutes = Mathf.Clamp(loaded.workMinutes, MinMinutes, MaxMinutes);
            loaded.shortBreakMinutes = Mathf.Clamp(loaded.shortBreakMinutes, MinMinutes, MaxMinutes);
            loaded.longBreakMinutes = Mathf.Clamp(loaded.longBreakMinutes, MinMinutes, MaxMinutes);
            loaded.cyclesBeforeLongBreak = Mathf.Clamp(loaded.cyclesBeforeLongBreak, MinMinutes, MaxMinutes);
        }
    }
}
