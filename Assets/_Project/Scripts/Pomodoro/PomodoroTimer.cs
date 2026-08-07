using System;
using gishadev.tools.SavingSystem;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Pomodoro
{
    /// <summary>
    /// The Pomodoro state machine; knows nothing about UI, audio, or the window. Time is tracked
    /// against a UTC end timestamp rather than accumulated frame deltas: the app throttles its frame
    /// rate and stops rendering while unfocused, so deltas would drift — and a timestamp lets a session
    /// survive the machine sleeping or the app closing.
    /// </summary>
    public sealed class PomodoroTimer : ITickable
    {
        private const string SaveKey = "pomodoro.state";

        private readonly PomodoroSettings _settings;
        private readonly ISaverSystem _saver;

        private DateTime _endUtc;
        private TimeSpan _remaining;
        private bool _restoredPhaseExpired;

        public PomodoroTimer(PomodoroSettings settings, ISaverSystem saver)
        {
            _settings = settings;
            _saver = saver;
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

        /// <summary>Raised whether started by the user or by auto-advance.</summary>
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

        /// <summary>
        /// Switches to <paramref name="phase"/> and starts it. Picking the phase already loaded resumes
        /// it with whatever was banked; picking a different one abandons the current phase and starts
        /// the new one at full length — it is a change of mind, not a completion, so no cycle is counted.
        /// </summary>
        public void StartPhase(PomodoroPhase phase)
        {
            if (phase != Phase)
            {
                Phase = phase;
                IsRunning = false;
                _remaining = TimeSpan.Zero;
            }

            Start();
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
        public void Skip() => CompletePhase(allowAutoStart: true);

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
                // Reported once and deliberately not auto-advanced: chaining would fire off however
                // many phases elapsed overnight.
                _restoredPhaseExpired = false;
                CompletePhase(allowAutoStart: false);
                return;
            }

            if (!IsRunning) return;
            if (DateTime.UtcNow < _endUtc) return;

            CompletePhase(allowAutoStart: true);
        }

        private void CompletePhase(bool allowAutoStart)
        {
            var completed = Phase;

            if (completed == PomodoroPhase.Work)
                CompletedWorkSessions++;

            Phase = NextPhase(completed);
            IsRunning = false;
            _remaining = TimeSpan.Zero;

            Save();
            PhaseCompleted?.Invoke(completed);

            if (allowAutoStart && _settings.ShouldAutoStartAfter(completed))
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
            _saver.Save(SaveKey, JsonUtility.ToJson(new State
            {
                phase = (int)Phase,
                completedWorkSessions = CompletedWorkSessions,
                running = IsRunning,
                endUtcTicks = _endUtc.Ticks,
                remainingTicks = _remaining.Ticks
            }));
        }

        private void Restore()
        {
            if (!_saver.TryLoad(SaveKey, out var json) || string.IsNullOrEmpty(json))
                return;

            var state = JsonUtility.FromJson<State>(json);
            if (state == null) return;

            Phase = Enum.IsDefined(typeof(PomodoroPhase), state.phase)
                ? (PomodoroPhase)state.phase
                : PomodoroPhase.Work;
            CompletedWorkSessions = Math.Max(0, state.completedWorkSessions);
            _remaining = new TimeSpan(SanitizeTicks(state.remainingTicks));
            _endUtc = new DateTime(SanitizeTicks(state.endUtcTicks), DateTimeKind.Utc);

            if (!state.running) return;

            if (DateTime.UtcNow < _endUtc)
            {
                // Still mid-phase: pick it up where the wall clock says it is.
                IsRunning = true;
                return;
            }

            // Ran out while closed. Deferred to the first tick: this is the constructor, so nothing
            // has subscribed yet and the event would be raised into the void.
            _restoredPhaseExpired = true;
        }

        private static long SanitizeTicks(long ticks) =>
            ticks >= 0 && ticks <= DateTime.MaxValue.Ticks ? ticks : 0;

        [Serializable]
        private sealed class State
        {
            public int phase;
            public int completedWorkSessions;
            public bool running;
            public long endUtcTicks;
            public long remainingTicks;
        }
    }
}
