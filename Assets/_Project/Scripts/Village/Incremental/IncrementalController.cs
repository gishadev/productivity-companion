using System;
using gishadev.companion.Events;
using gishadev.companion.Focus;
using gishadev.companion.Pomodoro;
using gishadev.companion.SavingLoading;
using gishadev.tools.Events;
using gishadev.tools.SavingSystem;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Village
{
    public sealed class IncrementalController : IStartable, ITickable, IDisposable
    {
        // Guards against huge deltas after sleep or domain reload.
        private const float MaxTickDelta = 1f;

        // Keeps Threshold() from overflowing to infinity.
        private const int MaxLevel = 200;

        // FileSaverSystem flushes on every Save, so accrual writes are debounced.
        private const float PersistInterval = 30f;

        private readonly IncrementalSettingsSO _settings;
        private readonly FocusController _focus;
        private readonly PomodoroTimer _pomodoro;
        private readonly IEventBus _eventBus;
        private readonly SaveSlot<IncrementalData> _slot;
        private readonly IncrementalView _view;

        // double, not float: at float precision a frame's increment stops moving progress around level 31.
        private double _progress;
        private float _penaltySeconds;
        private bool _penaltyFired;

        private bool _dirty;
        private float _nextPersist;

        // -1 / null force the first refresh through.
        private int _pushedValue = -1;
        private bool _pushedPenalty;
        private int _pushedLevel = -1;
        private FocusCategory? _pushedCategory;
        private bool _pushedOnBreak;
        private bool _pushedPaused;

        public IncrementalController(
            IncrementalSettingsSO settings,
            FocusController focus,
            PomodoroTimer pomodoro,
            IEventBus eventBus,
            ISaverSystem saver,
            IncrementalView view)
        {
            _settings = settings;
            _focus = focus;
            _pomodoro = pomodoro;
            _eventBus = eventBus;
            _slot = new SaveSlot<IncrementalData>(saver, SaveKeys.Incremental);
            _view = view;

            // In the constructor so resolvers see a valid level regardless of entry point order.
            Restore();
        }

        public int Level { get; private set; }

        // 0..1 within the current level.
        public float Progress => (float)_progress;

        public float PenaltySeconds => _penaltySeconds;

        public bool IsPenalised => _penaltySeconds > 0f;

        // Only a running break shelters from penalties.
        public bool IsOnBreak => _pomodoro.IsRunning && _pomodoro.Phase != PomodoroPhase.Work;

        void IStartable.Start() => Refresh();

        void ITickable.Tick()
        {
            var dt = Mathf.Min(Time.unscaledDeltaTime, MaxTickDelta);

            // A stopped timer freezes both progress and penalty.
            if (_pomodoro.IsRunning) Advance(_focus.EffectiveCategory, dt);

            Refresh();
            if (_dirty && Time.unscaledTime >= _nextPersist) Persist();
        }

        private void Advance(FocusCategory category, float dt)
        {
            // On a break, unproductive time neither accrues nor pays off penalty.
            if (category == FocusCategory.Unproductive)
            {
                if (!IsOnBreak) AccruePenalty(dt);
                return;
            }

            if (IsPenalised) RecoverPenalty(category, dt);
            else AccrueProgress(category, dt);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Fires LevelUpEvent in both directions; consumers reconcile from Level.
        public void DebugSetLevel(int level)
        {
            var previous = Level;

            Level = Mathf.Clamp(level, 0, MaxLevel);
            _progress = 0d;
            _dirty = true;

            Persist();
            Refresh();

            if (Level != previous) _eventBus.Fire(new LevelUpEvent(Level, previous));
        }

        public bool DebugPenaltyFired => _penaltyFired;

        // Fires only when the latch was down, so triggers and clears stay paired.
        public void DebugFillPenalty()
        {
            _penaltySeconds = _settings.MaxPenaltySeconds;

            if (!_penaltyFired)
            {
                _penaltyFired = true;
                _eventBus.Fire(new PenaltyTriggeredEvent(_penaltySeconds, _focus.CurrentProcessName));
            }

            Refresh();
        }

        public void DebugClearPenalty()
        {
            _penaltySeconds = 0f;

            if (_penaltyFired)
            {
                _penaltyFired = false;
                _eventBus.Fire(new PenaltyClearedEvent());
            }

            Refresh();
        }

        public void DebugResetState()
        {
            _penaltySeconds = 0f;
            _penaltyFired = false;

            DebugSetLevel(0);
        }
#endif

        public void Dispose()
        {
            if (_dirty) Persist();
        }

        private void AccrueProgress(FocusCategory category, float dt)
        {
            var points = _settings.BasePointsPerSecond * MultiplierFor(category) * PomodoroMultiplier() * dt;

            if (points <= 0f) return;

            var threshold = Threshold(Level);
            _progress += points / threshold;
            _dirty = true;

            while (_progress >= 1d)
            {
                if (Level >= MaxLevel)
                {
                    _progress = 1d;
                    return;
                }

                var previous = Level;
                Level++;

                // Carry the overflow into the next level's scale.
                var next = Threshold(Level);
                _progress = (_progress - 1d) * threshold / next;
                threshold = next;

                // Level-ups persist immediately rather than on the debounce.
                _nextPersist = 0f;
                _eventBus.Fire(new LevelUpEvent(Level, previous));
            }
        }

        private void AccruePenalty(float dt)
        {
            var max = _settings.MaxPenaltySeconds;
            _penaltySeconds = Mathf.Min(_penaltySeconds + dt, max);

            if (_penaltyFired || _penaltySeconds < max) return;

            _penaltyFired = true;
            _eventBus.Fire(new PenaltyTriggeredEvent(_penaltySeconds, _focus.CurrentProcessName));
        }

        private void RecoverPenalty(FocusCategory category, float dt)
        {
            _penaltySeconds = Mathf.Max(0f, _penaltySeconds - RecoveryFor(category) * dt);
            if (_penaltySeconds > 0f) return;

            // Clears only pair with a fired trigger.
            if (!_penaltyFired) return;

            _penaltyFired = false;
            _eventBus.Fire(new PenaltyClearedEvent());
        }

        private float MultiplierFor(FocusCategory category) => category == FocusCategory.Productive
            ? _settings.ProductiveMultiplier
            : _settings.RegularMultiplier;

        private float PomodoroMultiplier() =>
            _pomodoro.Phase == PomodoroPhase.Work ? 1f : _settings.BreakMultiplier;

        private float RecoveryFor(FocusCategory category) => category == FocusCategory.Productive
            ? _settings.ProductiveRecoveryPerSecond
            : _settings.RegularRecoveryPerSecond;

        private double Threshold(int level) => _settings.BaseThreshold * Math.Pow(_settings.GrowthFactor, level);

        private void Refresh()
        {
            // Unity's == catches destroyed objects; ?. and "is null" don't.
            if (_view == null) return;

            var penalised = IsPenalised;
            var value = Mathf.Clamp01(penalised ? _penaltySeconds / _settings.MaxPenaltySeconds : (float)_progress);

            // Only push when the bar would visibly move.
            var quantised = Mathf.RoundToInt(value * 200f);
            if (quantised != _pushedValue || penalised != _pushedPenalty)
            {
                _pushedValue = quantised;
                _pushedPenalty = penalised;
                _view.SetProgress(value, penalised);
            }

            if (Level != _pushedLevel)
            {
                _pushedLevel = Level;
                _view.SetLevel(Level);
            }

            var paused = !_pomodoro.IsRunning;
            var onBreak = IsOnBreak;
            var category = _focus.EffectiveCategory;

            if (category != _pushedCategory || onBreak != _pushedOnBreak || paused != _pushedPaused)
            {
                _pushedCategory = category;
                _pushedOnBreak = onBreak;
                _pushedPaused = paused;
                _view.SetActivity(category, onBreak, paused);
            }
        }

        private void Persist()
        {
            _dirty = false;
            _nextPersist = Time.unscaledTime + PersistInterval;
            _slot.Save(new IncrementalData { level = Level, progress = (float)_progress });
        }

        private void Restore()
        {
            var state = _slot.Load();

            Level = Mathf.Clamp(state.level, 0, MaxLevel);
            _progress = float.IsFinite(state.progress) ? Mathf.Clamp01(state.progress) : 0f;
        }
    }
}
