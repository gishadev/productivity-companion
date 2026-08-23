using System;
using gishadev.companion.Events;
using gishadev.companion.Focus;
using gishadev.companion.Pomodoro;
using gishadev.tools.Events;
using gishadev.tools.SavingSystem;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Turns focus category into village progress. Regular apps earn a baseline, productive apps a
    /// multiple of it, and unproductive apps instead accrue a capped penalty that has to be paid back
    /// before progress resumes at all.
    /// </summary>
    public sealed class IncrementalController : IStartable, ITickable, IDisposable
    {
        private const string SaveKey = "incremental.state";

        // Waking from sleep, a domain reload or a scene load can hand us one enormous delta. Set well
        // above the worst real frame interval (the widget throttles rendering, not Update, so ticks keep
        // arriving at the target frame rate) so it only ever catches genuine stalls.
        private const float MaxTickDelta = 1f;

        // Far past anything reachable; exists so Threshold() cannot overflow to infinity, which would
        // freeze the level and feed the slider a NaN.
        private const int MaxLevel = 200;

        // Every accruing tick dirties the state, and FileSaverSystem flushes to disk on every Save, so
        // writes are debounced. Caps what an ungraceful kill can cost, without a write per frame.
        private const float PersistInterval = 30f;

        private readonly IncrementalSettingsSO _settings;
        private readonly FocusController _focus;
        private readonly PomodoroTimer _pomodoro;
        private readonly IEventBus _eventBus;
        private readonly ISaverSystem _saver;
        private readonly IncrementalView _view;

        // double, and normalized into the current level rather than counted in raw points.
        //
        // The normalizing is for the slider and for save-file stability. The double is the part that
        // matters: at 32-bit precision a frame's increment stops moving the accumulator at all once the
        // level threshold passes ~2.8e5 — around level 31, which is only ~520 hours of use — and
        // progress silently freezes on a full-looking bar. Normalizing does *not* avoid this, because
        // the increment shrinks in step with the threshold it is divided by; both representations stall
        // at the same level. Only the wider mantissa does, pushing it to level 98 (~1e14 points).
        private double _progress;
        private float _penaltySeconds;
        private bool _penaltyFired;

        private bool _dirty;
        private float _nextPersist;

        // -1 is unmatchable by the quantised value and by any level, so the first refresh always writes
        // through. Same job done by the null on the category, whose default would otherwise be a real
        // value that could match on the first comparison and suppress the initial write.
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
            _saver = saver;
            _view = view;

            // In the constructor, not Start: anything that resolves this controller should see a valid
            // level immediately, without depending on entry point registration order.
            Restore();
        }

        public int Level { get; private set; }

        /// <summary>How far into the current level, 0 to 1.</summary>
        public float Progress => (float)_progress;

        public float PenaltySeconds => _penaltySeconds;

        /// <summary>While true nothing accrues: productive and regular time pays off the penalty instead.</summary>
        public bool IsPenalised => _penaltySeconds > 0f;

        /// <summary>A break that is actually ticking, which is the only state that shelters from penalties.</summary>
        public bool IsOnBreak => _pomodoro.IsRunning && _pomodoro.Phase != PomodoroPhase.Work;

        void IStartable.Start() => Refresh();

        void ITickable.Tick()
        {
            var dt = Mathf.Min(Time.unscaledDeltaTime, MaxTickDelta);

            // A stopped timer — paused, reset or never started — freezes the whole mechanic. Progress
            // does not move and the penalty holds its value rather than accruing or draining, so the
            // widget picks up exactly where it left off when the session resumes.
            if (_pomodoro.IsRunning) Advance(_focus.EffectiveCategory, dt);

            Refresh();
            if (_dirty && Time.unscaledTime >= _nextPersist) Persist();
        }

        private void Advance(FocusCategory category, float dt)
        {
            // On a break, unproductive time is inert rather than punished: it neither accrues penalty
            // nor pays one off, so an owed penalty simply waits until the break ends.
            if (category == FocusCategory.Unproductive)
            {
                if (!IsOnBreak) AccruePenalty(dt);
                return;
            }

            if (IsPenalised) RecoverPenalty(category, dt);
            else AccrueProgress(category, dt);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Jumps to a level. Debug tooling only, which is why it is compiled out of release builds —
        /// nothing in the app is allowed to hand out levels.
        ///
        /// Fires <see cref="LevelUpEvent"/> in both directions. The name reads oddly on a decrease, but
        /// every consumer reconciles from <see cref="Level"/> rather than accumulating from the payload,
        /// so one event puts the village into the right shape either way.
        /// </summary>
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

        /// <summary>
        /// Straight to a triggered penalty, without the two unproductive minutes it normally takes.
        /// Only fires the event when the latch was down, so pressing it twice cannot leave two triggers
        /// outstanding against a single clear.
        /// </summary>
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

        /// <summary>
        /// Pays the penalty off outright. Mirrors the pairing rule in RecoverPenalty: a clear is only
        /// announced when a trigger actually preceded it, or consumers would be told to recover from
        /// something that never happened.
        /// </summary>
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

        /// <summary>Back to a fresh install: level, progress and any penalty owed.</summary>
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

            // A zero rate leaves the state untouched rather than marking it dirty.
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

                // Carry the overflow across, rescaled into the new level's larger frame so it is worth
                // the same number of points either side of the boundary.
                var next = Threshold(Level);
                _progress = (_progress - 1d) * threshold / next;
                threshold = next;

                // Immediately, rather than on the debounce: a level is the one thing worth a disk write.
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

            // Only pairs with a penalty that actually fired: draining a partial one the user never
            // maxed out is not something a consumer needs to recover from.
            if (!_penaltyFired) return;

            _penaltyFired = false;
            _eventBus.Fire(new PenaltyClearedEvent());
        }

        private float MultiplierFor(FocusCategory category) => category == FocusCategory.Productive
            ? _settings.ProductiveMultiplier
            : _settings.RegularMultiplier;

        // Only ever reached from Advance, which the running check already gates.
        private float PomodoroMultiplier() =>
            _pomodoro.Phase == PomodoroPhase.Work ? 1f : _settings.BreakMultiplier;

        private float RecoveryFor(FocusCategory category) => category == FocusCategory.Productive
            ? _settings.ProductiveRecoveryPerSecond
            : _settings.RegularRecoveryPerSecond;

        private double Threshold(int level) => _settings.BaseThreshold * Math.Pow(_settings.GrowthFactor, level);

        private void Refresh()
        {
            // Unity's == is overloaded to report a destroyed object as null; ?. and "is null" bypass it.
            if (_view == null) return;

            var penalised = IsPenalised;
            var value = Mathf.Clamp01(penalised ? _penaltySeconds / _settings.MaxPenaltySeconds : (float)_progress);

            // Ticks arrive far faster than the widget renders while unfocused, so only push when the
            // bar would visibly move.
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
            _saver.Save(SaveKey, JsonUtility.ToJson(new State { level = Level, progress = (float)_progress }));
        }

        private void Restore()
        {
            if (!_saver.TryLoad(SaveKey, out var json) || string.IsNullOrEmpty(json)) return;

            State state;
            try
            {
                state = JsonUtility.FromJson<State>(json);
            }
            catch (Exception exception)
            {
                // This runs while the container is being built, so letting it escape takes the app down
                // over a corrupt save. Starting over is the better failure.
                Debug.LogWarning($"[Incremental] Discarding unreadable save state: {exception.Message}");
                return;
            }

            if (state == null) return;

            Level = Mathf.Clamp(state.level, 0, MaxLevel);
            _progress = float.IsFinite(state.progress) ? Mathf.Clamp01(state.progress) : 0f;
        }

        // progress is a float on the way to disk even though it is accumulated as a double: only the
        // repeated addition needs the wider mantissa, and rounding a 0-1 fraction once per load costs
        // ~6e-8 of a level. Keeping it float also stays inside the field types JsonUtility is known to
        // round-trip, rather than betting the save file on its handling of double.
        [Serializable]
        private sealed class State
        {
            public int level;
            public float progress;
        }
    }
}
