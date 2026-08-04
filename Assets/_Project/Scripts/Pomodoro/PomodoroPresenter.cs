using System;
using VContainer.Unity;

namespace gishadev.companion.Pomodoro
{
    /// <summary>
    /// Binds <see cref="PomodoroTimer"/> to <see cref="PomodoroWidgetView"/>: turns button presses into
    /// timer commands, and timer state into a countdown label. The only type that knows about both
    /// sides, so the model stays UI-free and the view stays logic-free.
    /// </summary>
    /// <remarks>
    /// A plain C# entry point rather than a MonoBehaviour — nothing here needs a transform, and keeping
    /// it out of the scene means the wiring lives entirely in <c>PomodoroInstaller</c> where it can be
    /// read in one place.
    /// </remarks>
    public sealed class PomodoroPresenter : IStartable, ITickable, IDisposable
    {
        private readonly PomodoroTimer _timer;
        private readonly PomodoroSettings _settings;
        private readonly PomodoroWidgetView _view;

        /// <summary>Whole seconds currently on the label; -1 forces the next refresh through.</summary>
        private int _displayedSeconds = -1;

        public PomodoroPresenter(PomodoroTimer timer, PomodoroSettings settings, PomodoroWidgetView view)
        {
            _timer = timer;
            _settings = settings;
            _view = view;
        }

        /// <remarks>
        /// Subscribing here rather than lazily matters: <see cref="PomodoroTimer"/> restores its state in
        /// its constructor and defers a phase that expired while the app was closed to its first Tick.
        /// VContainer dispatches IStartable during Initialization and ITickable during Update, so this
        /// runs first and that event is not raised into the void.
        /// </remarks>
        void IStartable.Start()
        {
            _view.PlayPauseClicked += OnPlayPauseClicked;
            _view.SettingsClicked += OnSettingsClicked;

            _timer.StateChanged += Refresh;
            _timer.PhaseStarted += OnPhaseChanged;
            _timer.PhaseCompleted += OnPhaseCompleted;

            // A duration edited while the timer sits idle should show up on the label immediately.
            _settings.Changed += Refresh;

            Refresh();
        }

        public void Dispose()
        {
            _view.PlayPauseClicked -= OnPlayPauseClicked;
            _view.SettingsClicked -= OnSettingsClicked;

            _timer.StateChanged -= Refresh;
            _timer.PhaseStarted -= OnPhaseChanged;
            _timer.PhaseCompleted -= OnPhaseCompleted;

            _settings.Changed -= Refresh;
        }

        void ITickable.Tick() => RefreshTime();

        private void OnPlayPauseClicked()
        {
            if (_timer.IsRunning)
                _timer.Pause();
            else
                _timer.Start();
        }

        private void OnSettingsClicked()
        {
            // Seam for the settings panel. PomodoroSettings is already bindable; the UI for it is not
            // built yet, so the press is intentionally inert rather than wired to a placeholder.
        }

        // Start() also raises PhaseStarted when resuming from a pause, so every handler here has to be
        // an idempotent refresh rather than a transition.
        private void OnPhaseChanged(PomodoroPhase phase) => Refresh();

        private void OnPhaseCompleted(PomodoroPhase phase)
        {
            // The natural place for a chime or a notification later, via gishadev.tools' SFXEmitter.
            Refresh();
        }

        private void Refresh()
        {
            _view.SetRunning(_timer.IsRunning);

            _displayedSeconds = -1;
            RefreshTime();
        }

        /// <summary>
        /// Pushes the countdown to the label, but only when the whole-second value has actually moved —
        /// this runs every frame.
        /// </summary>
        private void RefreshTime()
        {
            var seconds = (int)Math.Ceiling(RemainingToShow().TotalSeconds);
            if (seconds == _displayedSeconds) return;

            _displayedSeconds = seconds;
            _view.SetTime(Format(seconds));
        }

        /// <remarks>
        /// A stopped phase with nothing banked reports zero remaining, which would leave a fresh install
        /// (or a widget just after Reset) reading 00:00. Fall back to the phase's configured length so an
        /// idle widget advertises what pressing play will give you.
        /// </remarks>
        private TimeSpan RemainingToShow()
        {
            var remaining = _timer.Remaining;
            if (!_timer.IsRunning && remaining <= TimeSpan.Zero)
                return _settings.DurationOf(_timer.Phase);

            return remaining;
        }

        /// <summary>
        /// Formats whole seconds as MM:SS. Minutes are not wrapped at 60 — a 90 minute phase reads
        /// 90:00, which keeps the widget narrow and avoids an hours field that is empty almost always.
        /// </summary>
        private static string Format(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
