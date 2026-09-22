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
    public sealed class VillageController : IStartable, ITickable, IDisposable
    {
        private readonly IncrementalController _incremental;
        private readonly FocusController _focus;
        private readonly PomodoroTimer _pomodoro;
        private readonly IEventBus _eventBus;
        private readonly VillagersFactory _factory;
        private readonly VillagersAIController _ai;
        private readonly PenaltyVillageFireView _fireView;

        public VillageController(
            IncrementalController incremental,
            FocusController focus,
            PomodoroTimer pomodoro,
            IEventBus eventBus,
            VillagersFactory factory,
            VillagersAIController ai,
            PenaltyVillageFireView fireView)
        {
            _incremental = incremental;
            _focus = focus;
            _pomodoro = pomodoro;
            _eventBus = eventBus;
            _factory = factory;
            _ai = ai;
            _fireView = fireView;
        }

        public int TargetVillagers => _incremental.Level + 1;

        // CurrentCategory, not EffectiveCategory: otherwise villagers down tools whenever the widget is clicked.
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
            _eventBus.Subscribe<PenaltyTriggeredEvent>(OnPenaltyTriggered);
            _eventBus.Subscribe<PenaltyClearedEvent>(OnPenaltyCleared);

            // Penalty isn't restored from saves, so the fire must start off.
            SetFire(false);

            // Primed: LevelUpEvent only describes transitions, not a restored level.
            _factory.SetTarget(TargetVillagers);
        }

        void ITickable.Tick() => _ai.Tick(Time.deltaTime, Activity);

        public void Dispose()
        {
            _eventBus.Unsubscribe<LevelUpEvent>(OnLevelUp);
            _eventBus.Unsubscribe<PenaltyTriggeredEvent>(OnPenaltyTriggered);
            _eventBus.Unsubscribe<PenaltyClearedEvent>(OnPenaltyCleared);
        }

        private void OnLevelUp(LevelUpEvent levelUpEvent) => _factory.SetTarget(TargetVillagers);

        // Driven by events, not IsPenalised: the fire marks a penalty that hit the cap.
        private void OnPenaltyTriggered(PenaltyTriggeredEvent penaltyEvent) => SetFire(true);

        private void OnPenaltyCleared(PenaltyClearedEvent penaltyEvent) => SetFire(false);

        private void SetFire(bool onFire)
        {
            if (_fireView != null) _fireView.SetOnFire(onFire);
        }
    }
}
