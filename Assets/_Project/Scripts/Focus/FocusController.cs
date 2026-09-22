using System;
using gishadev.companion.Events;
using gishadev.companion.Window.Native;
using gishadev.tools.Events;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Focus
{
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

        public bool IsOwnWindowFocused { get; private set; }

        // Our own window counts as Regular, so sitting in the widget neither earns the last app's
        // multiplier nor keeps accruing its penalty.
        public FocusCategory EffectiveCategory =>
            IsOwnWindowFocused ? FocusCategory.Regular : CurrentCategory;

        // Last foreign app; our own window is ignored.
        public string CurrentProcessName { get; private set; } = string.Empty;

        public string CurrentTitle { get; private set; } = string.Empty;

        public bool TagCurrent(FocusCategory category) => _rules.Set(CurrentProcessName, category);

        void IStartable.Start()
        {
            _rules.Changed += Reclassify;
            Poll();
        }

        public void Dispose() => _rules.Changed -= Reclassify;

        void ITickable.Tick()
        {
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

            // Ignoring our own window keeps tagging pointed at the app the user came from.
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
