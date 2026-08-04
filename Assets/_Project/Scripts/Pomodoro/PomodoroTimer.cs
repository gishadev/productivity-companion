using System;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Pomodoro
{
    /// <summary>
    /// The Pomodoro state machine. Knows nothing about UI, audio, or the window — consumers subscribe
    /// to the events.
    /// </summary>
    /// <remarks>
    /// Time is tracked against the wall clock (a UTC end timestamp) rather than accumulated frame
    /// deltas. The app throttles its frame rate and stops rendering while unfocused, so a
    /// delta-accumulating timer would drift; a timestamp also lets a session survive the machine
    /// sleeping or the app closing.
    ///
    /// Registered as a VContainer entry point, so it needs no MonoBehaviour: Tick() only compares
    /// UtcNow against the end time.
    /// </remarks>
    public sealed class PomodoroTimer : ITickable
    {
        private const string KeyPrefix = "pomodoro.state.";
        private const string PhaseKey = KeyPrefix + "phase";
        private const string EndUtcKey = KeyPrefix + "endUtcTicks";
        private const string RemainingKey = KeyPrefix + "remainingTicks";
        private const string RunningKey = KeyPrefix + "running";
        private const string CompletedKey = KeyPrefix + "completedWorkSessions";

        private readonly PomodoroSettings _settings;

        private DateTime _endUtc;
        private TimeSpan _remaining;
        private bool _restoredPhaseExpired;

        public PomodoroTimer(PomodoroSettings settings)
        {
            _settings = settings;
            Restore();
        }

        public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Work;

        public bool IsRunning { get; private set; }

        /// <summary>Work sessions finished so far; drives when a long break is due.</summary>
        public int CompletedWorkSessions { get; private set; }

        public TimeSpan Remaining
        {
            get
            {
                if (!IsRunning) return _remaining;
                var left = _endUtc - DateTime.UtcNow;
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        /// <summary>Raised when a phase begins, whether started by the user or by auto-advance.</summary>
        public event Action<PomodoroPhase> PhaseStarted;

        /// <summary>Raised when a phase reaches its end, or is ended early via <see cref="Skip"/>.</summary>
        public event Action<PomodoroPhase> PhaseCompleted;

        /// <summary>Coarse "something changed" signal for views to refresh on.</summary>
        public event Action StateChanged;

        /// <summary>Starts the current phase, or resumes it if paused.</summary>
        public void Start()
        {
            if (IsRunning) return;

            // A phase that has never run (or was reset) has no remaining time banked yet.
            if (_remaining <= TimeSpan.Zero)
                _remaining = _settings.DurationOf(Phase);

            _endUtc = DateTime.UtcNow + _remaining;
            IsRunning = true;

            Save();
            PhaseStarted?.Invoke(Phase);
            StateChanged?.Invoke();
        }

        public void Pause()
        {
            if (!IsRunning) return;

            _remaining = Remaining;
            IsRunning = false;

            Save();
            StateChanged?.Invoke();
        }

        /// <summary>Ends the current phase immediately and advances, as if it had run out.</summary>
        public void Skip() => CompletePhase(_settings.AutoAdvance);

        /// <summary>Returns to a stopped Work phase and clears the cycle count.</summary>
        public void Reset()
        {
            Phase = PomodoroPhase.Work;
            CompletedWorkSessions = 0;
            IsRunning = false;
            _remaining = TimeSpan.Zero;

            Save();
            StateChanged?.Invoke();
        }

        void ITickable.Tick()
        {
            if (_restoredPhaseExpired)
            {
                // A phase that ran out while the app was closed. Reported once, and deliberately not
                // auto-advanced — chaining here would fire off however many phases elapsed overnight.
                _restoredPhaseExpired = false;
                CompletePhase(autoAdvance: false);
                return;
            }

            if (!IsRunning) return;
            if (DateTime.UtcNow < _endUtc) return;

            CompletePhase(_settings.AutoAdvance);
        }

        private void CompletePhase(bool autoAdvance)
        {
            var completed = Phase;

            if (completed == PomodoroPhase.Work)
                CompletedWorkSessions++;

            Phase = NextPhase(completed);
            IsRunning = false;
            _remaining = TimeSpan.Zero;

            Save();
            PhaseCompleted?.Invoke(completed);

            if (autoAdvance)
                Start();
            else
                StateChanged?.Invoke();
        }

        private PomodoroPhase NextPhase(PomodoroPhase completed)
        {
            if (completed != PomodoroPhase.Work)
                return PomodoroPhase.Work;

            return CompletedWorkSessions % _settings.CyclesBeforeLongBreak == 0
                ? PomodoroPhase.LongBreak
                : PomodoroPhase.ShortBreak;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(PhaseKey, (int)Phase);
            PlayerPrefs.SetInt(CompletedKey, CompletedWorkSessions);
            PlayerPrefs.SetInt(RunningKey, IsRunning ? 1 : 0);
            PlayerPrefs.SetString(EndUtcKey, _endUtc.Ticks.ToString());
            PlayerPrefs.SetString(RemainingKey, _remaining.Ticks.ToString());
            PlayerPrefs.Save();
        }

        private void Restore()
        {
            Phase = (PomodoroPhase)PlayerPrefs.GetInt(PhaseKey, (int)PomodoroPhase.Work);
            CompletedWorkSessions = PlayerPrefs.GetInt(CompletedKey, 0);
            _remaining = new TimeSpan(ParseTicks(RemainingKey));
            _endUtc = new DateTime(ParseTicks(EndUtcKey), DateTimeKind.Utc);

            var wasRunning = PlayerPrefs.GetInt(RunningKey, 0) != 0;
            if (!wasRunning)
            {
                IsRunning = false;
                return;
            }

            if (DateTime.UtcNow < _endUtc)
            {
                // Still mid-phase: pick it up where the wall clock says it is.
                IsRunning = true;
                return;
            }

            // The phase ran out while we were closed. Deferred to the first tick rather than handled
            // here: this runs in the constructor, so nothing has subscribed yet and the event would
            // be raised into the void.
            IsRunning = false;
            _restoredPhaseExpired = true;
        }

        private static long ParseTicks(string key)
        {
            var raw = PlayerPrefs.GetString(key, "0");
            return long.TryParse(raw, out var ticks) && ticks >= 0 ? ticks : 0;
        }
    }
}
