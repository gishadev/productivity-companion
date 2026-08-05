using System;
using gishadev.companion.Events;
using gishadev.tools.Events;
using VContainer.Unity;

namespace gishadev.companion.Pomodoro
{
    public sealed class PomodoroPresenter : IStartable, ITickable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly PomodoroTimer _timer;
        private readonly PomodoroSettings _settings;
        private readonly PomodoroWidgetView _pomodoroWidgetView;

        // -1 forces the next refresh through.
        private int _displayedSeconds = -1;

        public PomodoroPresenter(
            IEventBus eventBus,
            PomodoroTimer timer,
            PomodoroSettings settings,
            PomodoroWidgetView view)
        {
            _eventBus = eventBus;
            _timer = timer;
            _settings = settings;
            _pomodoroWidgetView = view;
        }

        // Subscribing in Start, not lazily: the timer defers a phase that expired while the app was
        // closed to its first Tick, and IStartable runs before ITickable — so the event is not lost.
        void IStartable.Start()
        {
            _eventBus.Subscribe<PlayPauseClickedEvent>(OnPlayPauseClicked);
            _eventBus.Subscribe<SettingsClickedEvent>(OnSettingsClicked);

            _timer.StateChanged += Refresh;
            _timer.PhaseStarted += OnPhaseChanged;
            _timer.PhaseCompleted += OnPhaseCompleted;

            // A duration edited while the timer sits idle should show up on the label immediately.
            _settings.Changed += Refresh;

            Refresh();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayPauseClickedEvent>(OnPlayPauseClicked);
            _eventBus.Unsubscribe<SettingsClickedEvent>(OnSettingsClicked);

            _timer.StateChanged -= Refresh;
            _timer.PhaseStarted -= OnPhaseChanged;
            _timer.PhaseCompleted -= OnPhaseCompleted;

            _settings.Changed -= Refresh;
        }

        void ITickable.Tick() => RefreshTime();

        private void OnPlayPauseClicked(PlayPauseClickedEvent playPauseClickedEvent)
        {
            if (_timer.IsRunning)
                _timer.Pause();
            else
                _timer.Start();
        }

        private void OnSettingsClicked(SettingsClickedEvent settingsClickedEvent)
        {
            // Seam for the settings panel; inert until that UI exists.
        }

        // Start() also raises PhaseStarted when resuming from a pause, so handlers must be idempotent
        // refreshes rather than transitions.
        private void OnPhaseChanged(PomodoroPhase phase) => Refresh();

        private void OnPhaseCompleted(PomodoroPhase phase) => Refresh();

        private void Refresh()
        {
            _pomodoroWidgetView.SetRunning(_timer.IsRunning);

            _displayedSeconds = -1;
            RefreshTime();
        }

        // Runs every frame; only writes when the whole-second value moved.
        private void RefreshTime()
        {
            var seconds = (int)Math.Ceiling(RemainingToShow().TotalSeconds);
            if (seconds == _displayedSeconds) return;

            _displayedSeconds = seconds;
            _pomodoroWidgetView.SetTime(Format(seconds));
        }

        // A stopped phase with nothing banked reports zero, which would leave a fresh install reading
        // 00:00. Fall back to the configured length so an idle widget shows what play will give you.
        private TimeSpan RemainingToShow()
        {
            var remaining = _timer.Remaining;
            if (!_timer.IsRunning && remaining <= TimeSpan.Zero)
                return _settings.DurationOf(_timer.Phase);

            return remaining;
        }

        // MM:SS, minutes deliberately not wrapped at 60: a 90 minute phase reads 90:00, which keeps the
        // widget narrow and avoids an hours field that is almost always empty.
        private static string Format(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}