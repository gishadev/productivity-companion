using gishadev.companion.Focus;
using gishadev.tools.Events;

namespace gishadev.companion.Events
{
    // Fired on change only; late subscribers read FocusController.CurrentCategory.
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
