using System;
using gishadev.companion.SavingLoading;
using gishadev.tools.SavingSystem;
using VContainer.Unity;

namespace gishadev.companion.Pomodoro
{
    // Tracks a UTC end timestamp, not frame deltas: rendering is throttled while unfocused, and a
    // session must survive sleep or restart.
    public sealed class PomodoroTimer : ITickable
    {
        private readonly PomodoroSettings _settings;
        private readonly SaveSlot<PomodoroTimerData> _slot;

        private DateTime _endUtc;
        private TimeSpan _remaining;
        private bool _restoredPhaseExpired;

        public PomodoroTimer(PomodoroSettings settings, ISaverSystem saver)
        {
            _settings = settings;
            _slot = new SaveSlot<PomodoroTimerData>(saver, SaveKeys.PomodoroTimer);
            Restore();
        }

        public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Work;

        public bool IsRunning { get; private set; }

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

        public event Action<PomodoroPhase> PhaseStarted;

        public event Action<PomodoroPhase> PhaseCompleted;

        public event Action StateChanged;

        public void Start()
        {
            if (IsRunning) return;

            if (_remaining <= TimeSpan.Zero)
                _remaining = _settings.DurationOf(Phase);

            _endUtc = DateTime.UtcNow + _remaining;
            IsRunning = true;

            Save();
            PhaseStarted?.Invoke(Phase);
            StateChanged?.Invoke();
        }

        // Same phase resumes; a different phase restarts at full length without counting a cycle.
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

        public void Skip() => CompletePhase(allowAutoStart: true);

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
                // Not auto-advanced, or it would chain through every phase that elapsed while closed.
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
            _slot.Save(new PomodoroTimerData
            {
                phase = (int)Phase,
                completedWorkSessions = CompletedWorkSessions,
                running = IsRunning,
                endUtcTicks = _endUtc.Ticks,
                remainingTicks = _remaining.Ticks
            });
        }

        private void Restore()
        {
            var state = _slot.Load();

            Phase = Enum.IsDefined(typeof(PomodoroPhase), state.phase)
                ? (PomodoroPhase)state.phase
                : PomodoroPhase.Work;
            CompletedWorkSessions = Math.Max(0, state.completedWorkSessions);
            _remaining = new TimeSpan(SanitizeTicks(state.remainingTicks));
            _endUtc = new DateTime(SanitizeTicks(state.endUtcTicks), DateTimeKind.Utc);

            if (!state.running) return;

            if (DateTime.UtcNow < _endUtc)
            {
                IsRunning = true;
                return;
            }

            // Deferred to the first tick: nothing has subscribed yet in the constructor.
            _restoredPhaseExpired = true;
        }

        private static long SanitizeTicks(long ticks) =>
            ticks >= 0 && ticks <= DateTime.MaxValue.Ticks ? ticks : 0;
    }
}
