using gishadev.tools.Events;

namespace gishadev.companion.Events
{
    // One tick can cross several levels; read the span, don't assume +1.
    public class LevelUpEvent : IEvent
    {
        public LevelUpEvent(int level, int previousLevel)
        {
            Level = level;
            PreviousLevel = previousLevel;
        }

        public int Level { get; }
        public int PreviousLevel { get; }
    }

    // Fired once when the cap is reached.
    public class PenaltyTriggeredEvent : IEvent
    {
        public PenaltyTriggeredEvent(float penaltySeconds, string processName)
        {
            PenaltySeconds = penaltySeconds;
            ProcessName = processName;
        }

        public float PenaltySeconds { get; }

        public string ProcessName { get; }
    }

    // Only ever follows a PenaltyTriggeredEvent.
    public class PenaltyClearedEvent : IEvent
    {
    }
}
