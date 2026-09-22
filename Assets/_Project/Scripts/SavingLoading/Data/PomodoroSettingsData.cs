using System;

namespace gishadev.companion.SavingLoading
{
    [Serializable]
    public sealed class PomodoroSettingsData
    {
        public int workMinutes = 25;
        public int shortBreakMinutes = 5;
        public int longBreakMinutes = 15;
        public int cyclesBeforeLongBreak = 4;
        public bool autoStartPomodoros = true;
        public bool autoStartBreaks = true;
    }
}
