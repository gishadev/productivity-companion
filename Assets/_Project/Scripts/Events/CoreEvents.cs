using gishadev.companion.Pomodoro;
using gishadev.tools.Events;

namespace gishadev.companion.Events
{
    /// <summary>Starts (or resumes) a specific phase. Raised by the radial options, not the play button.</summary>
    public class PlayClickedEvent : IEvent
    {
        public PlayClickedEvent(PomodoroPhase phase) => Phase = phase;

        public PomodoroPhase Phase { get; }
    }

    public class PauseClickedEvent : IEvent
    {
    }

    public class ResetClickedEvent : IEvent
    {
    }
}
