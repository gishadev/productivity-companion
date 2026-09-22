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

        // Subscribe in Start: the timer reports a phase that expired while closed on its first Tick.
        void IStartable.Start()
        {
            _eventBus.Subscribe<PlayClickedEvent>(OnPlayClicked);
            _eventBus.Subscribe<PauseClickedEvent>(OnPauseClicked);
            _eventBus.Subscribe<ResetClickedEvent>(OnResetClicked);

            _timer.StateChanged += Refresh;
            _timer.PhaseStarted += OnPhaseChanged;
            _timer.PhaseCompleted += OnPhaseCompleted;

            _settings.Changed += Refresh;

            Refresh();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<PlayClickedEvent>(OnPlayClicked);
            _eventBus.Unsubscribe<PauseClickedEvent>(OnPauseClicked);
            _eventBus.Unsubscribe<ResetClickedEvent>(OnResetClicked);

            _timer.StateChanged -= Refresh;
            _timer.PhaseStarted -= OnPhaseChanged;
            _timer.PhaseCompleted -= OnPhaseCompleted;

            _settings.Changed -= Refresh;
        }

        void ITickable.Tick() => RefreshTime();

        private void OnPlayClicked(PlayClickedEvent playClickedEvent) => _timer.StartPhase(playClickedEvent.Phase);

        private void OnPauseClicked(PauseClickedEvent pauseClickedEvent) => _timer.Pause();

        private void OnResetClicked(ResetClickedEvent resetClickedEvent) => _timer.Reset();

        // Also raised on resume from pause, so this must stay an idempotent refresh.
        private void OnPhaseChanged(PomodoroPhase phase) => Refresh();

        private void OnPhaseCompleted(PomodoroPhase phase) => Refresh();

        private void Refresh()
        {
            _pomodoroWidgetView.SetState(_timer.Phase, _timer.IsRunning,
                !_timer.IsRunning && _timer.Remaining > TimeSpan.Zero);

            _displayedSeconds = -1;
            RefreshTime();
        }

        private void RefreshTime()
        {
            var seconds = (int)Math.Ceiling(RemainingToShow().TotalSeconds);
            if (seconds == _displayedSeconds) return;

            _displayedSeconds = seconds;
            _pomodoroWidgetView.SetTime(Format(seconds));
        }

        // Idle with nothing banked: show the configured length instead of 00:00.
        private TimeSpan RemainingToShow()
        {
            var remaining = _timer.Remaining;
            if (!_timer.IsRunning && remaining <= TimeSpan.Zero)
                return _settings.DurationOf(_timer.Phase);

            return remaining;
        }

        // Minutes aren't wrapped at 60: a 90 minute phase reads 90:00.
        private static string Format(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}