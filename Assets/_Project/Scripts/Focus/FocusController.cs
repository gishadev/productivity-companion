using System;
using gishadev.companion.Events;
using gishadev.companion.Window.Native;
using gishadev.tools.Events;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Focus
{
    /// <summary>
    /// Polls which application owns the foreground and fires <see cref="FocusCategoryChangedEvent"/>
    /// whenever its classification changes.
    /// </summary>
    public sealed class FocusController : IStartable, ITickable, IDisposable
    {
        private const float PollInterval = 0.5f;

        private readonly IForegroundWindowProvider _provider;
        private readonly FocusRules _rules;
        private readonly IEventBus _eventBus;

        private float _nextPoll;

        public FocusController(IForegroundWindowProvider provider, FocusRules rules, IEventBus eventBus)
        {
            _provider = provider;
            _rules = rules;
            _eventBus = eventBus;
        }

        public FocusCategory CurrentCategory { get; private set; } = FocusCategory.Regular;

        /// <summary>True while the player itself holds the foreground.</summary>
        public bool IsOwnWindowFocused { get; private set; }

        /// <summary>
        /// What the user's current activity should count as. Identical to <see cref="CurrentCategory"/>
        /// except while our own window is focused, which counts as <see cref="FocusCategory.Regular"/>
        /// rather than inheriting the app they came from: the sticky category is right for tagging, but
        /// as a reward input it would pay out the last app's multiplier for sitting in the widget — and,
        /// worse, keep accruing an unproductive penalty with no way to escape it from inside the app.
        /// </summary>
        public FocusCategory EffectiveCategory =>
            IsOwnWindowFocused ? FocusCategory.Regular : CurrentCategory;

        /// <summary>Always the last foreign application: the player's own window is ignored.</summary>
        public string CurrentProcessName { get; private set; } = string.Empty;

        public string CurrentTitle { get; private set; } = string.Empty;

        /// <summary>Assigns <see cref="CurrentProcessName"/> to a category. Used by the focus settings UI and debug hotkeys.</summary>
        public bool TagCurrent(FocusCategory category) => _rules.Set(CurrentProcessName, category);

        void IStartable.Start()
        {
            _rules.Changed += Reclassify;
            Poll();
        }

        public void Dispose() => _rules.Changed -= Reclassify;

        void ITickable.Tick()
        {
            // Unscaled so a paused or slowed timescale cannot stall it.
            if (Time.unscaledTime < _nextPoll) return;
            Poll();
        }

        private void Poll()
        {
            _nextPoll = Time.unscaledTime + PollInterval;

            if (!_provider.IsAvailable) return;

            var info = _provider.GetForegroundWindow();
            if (!info.IsValid) return;

            IsOwnWindowFocused = info.IsOwnProcess;

            // Ignoring our own window keeps clicking the widget from resetting the user's activity, and
            // is what leaves the tagging hotkeys pointed at the app they just came from.
            if (info.IsOwnProcess) return;

            CurrentProcessName = info.ProcessName;
            CurrentTitle = info.Title;
            Reclassify();
        }

        private void Reclassify()
        {
            var next = _rules.Classify(CurrentProcessName);
            if (next == CurrentCategory) return;

            var previous = CurrentCategory;
            CurrentCategory = next;
            _eventBus.Fire(new FocusCategoryChangedEvent(next, previous, CurrentProcessName, CurrentTitle));
        }
    }
}
