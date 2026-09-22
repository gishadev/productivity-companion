using gishadev.companion.Pomodoro;
using gishadev.tools.Events;

namespace gishadev.companion.Events
{
    // Raised by the radial options, not the play button.
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
