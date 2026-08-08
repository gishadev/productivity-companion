using gishadev.companion.Focus;
using gishadev.tools.Events;

namespace gishadev.companion.Events
{
    /// <summary>
    /// The foreground application's classification changed. Fired on change only — a subscriber that
    /// starts late reads <see cref="Focus.FocusController.CurrentCategory"/> to prime itself.
    /// </summary>
    public class FocusCategoryChangedEvent : IEvent
    {
        public FocusCategoryChangedEvent(FocusCategory category, FocusCategory previous, string processName,
            string title)
        {
            Category = category;
            Previous = previous;
            ProcessName = processName;
            Title = title;
        }

        public FocusCategory Category { get; }

        public FocusCategory Previous { get; }

        public string ProcessName { get; }

        public string Title { get; }
    }
}
