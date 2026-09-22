using System;

namespace gishadev.companion.SavingLoading
{
    [Serializable]
    public sealed class PomodoroTimerData
    {
        public int phase;
        public int completedWorkSessions;
        public bool running;
        public long endUtcTicks;
        public long remainingTicks;
    }
}
