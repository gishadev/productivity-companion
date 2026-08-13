using gishadev.tools.Events;

namespace gishadev.companion.Events
{
    /// <summary>
    /// One per level gained. A single tick can cross several thresholds, so a consumer that spawns
    /// something per level must read the span rather than assume one level per event.
    /// </summary>
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

    /// <summary>Fired once when unproductive time reaches the cap, not repeatedly while it sits there.</summary>
    public class PenaltyTriggeredEvent : IEvent
    {
        public PenaltyTriggeredEvent(float penaltySeconds, string processName)
        {
            PenaltySeconds = penaltySeconds;
            ProcessName = processName;
        }

        public float PenaltySeconds { get; }

        /// <summary>The app that tipped it over; empty when nothing has been tracked yet.</summary>
        public string ProcessName { get; }
    }

    /// <summary>
    /// The penalty has been paid off in full. Only ever follows a <see cref="PenaltyTriggeredEvent"/>,
    /// so the two pair up: without it, anything that reacts to a penalty has no signal to recover on.
    /// </summary>
    public class PenaltyClearedEvent : IEvent
    {
    }
}
