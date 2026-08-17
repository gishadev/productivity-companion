using System;
using gishadev.companion.Events;
using gishadev.companion.Focus;
using gishadev.companion.Pomodoro;
using gishadev.companion.Village.Villagers;
using gishadev.tools.Events;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Village
{
    /// <summary>
    /// The village simulation. Owns the villager population, tells them what the user is currently
    /// doing, and drives their shared AI.
    /// </summary>
    public sealed class VillageController : IStartable, ITickable, IDisposable
    {
        private readonly IncrementalController _incremental;
        private readonly FocusController _focus;
        private readonly PomodoroTimer _pomodoro;
        private readonly IEventBus _eventBus;
        private readonly VillagersFactory _factory;
        private readonly VillagersAIController _ai;

        public VillageController(
            IncrementalController incremental,
            FocusController focus,
            PomodoroTimer pomodoro,
            IEventBus eventBus,
            VillagersFactory factory,
            VillagersAIController ai)
        {
            _incremental = incremental;
            _focus = focus;
            _pomodoro = pomodoro;
            _eventBus = eventBus;
            _factory = factory;
            _ai = ai;
        }

        /// <summary>One villager for the starting level, then one per level gained.</summary>
        public int TargetVillagers => _incremental.Level + 1;

        /// <summary>
        /// Read from <see cref="FocusController.CurrentCategory"/>, not <c>EffectiveCategory</c>. The
        /// latter reports Regular whenever our own window has focus — right for progression, since it
        /// stops the widget paying out the last app's multiplier, but it would make a working villager
        /// down tools every time the user clicks the widget to check the timer.
        /// </summary>
        public VillageActivity Activity
        {
            get
            {
                if (!_pomodoro.IsRunning) return VillageActivity.Neutral;
                if (_pomodoro.Phase != PomodoroPhase.Work) return VillageActivity.Relaxing;

                return _focus.CurrentCategory == FocusCategory.Productive
                    ? VillageActivity.Working
                    : VillageActivity.Neutral;
            }
        }

        void IStartable.Start()
        {
            _eventBus.Subscribe<LevelUpEvent>(OnLevelUp);

            // Primed, because LevelUpEvent only ever describes a transition — a restored save has
            // already done its levelling and would otherwise show an empty village until the next one.
            _factory.SetTarget(TargetVillagers);
        }

        void ITickable.Tick() => _ai.Tick(Time.deltaTime, Activity);

        public void Dispose() => _eventBus.Unsubscribe<LevelUpEvent>(OnLevelUp);

        // Reads the target rather than the event's level so the two paths cannot disagree; SetTarget is
        // idempotent, which matters because one tick can cross several thresholds and fire several times.
        private void OnLevelUp(LevelUpEvent levelUpEvent) => _factory.SetTarget(TargetVillagers);
    }
}
