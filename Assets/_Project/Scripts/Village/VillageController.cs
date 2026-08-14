using System;
using gishadev.companion.Events;
using gishadev.companion.Village.Villagers;
using gishadev.tools.Events;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Village
{
    /// <summary>
    /// The village simulation. Owns the villager population and drives their shared AI; the progression
    /// that feeds it lives in <see cref="IncrementalController"/>, which this only reads from.
    /// </summary>
    public sealed class VillageController : IStartable, ITickable, IDisposable
    {
        private readonly IncrementalController _incremental;
        private readonly IEventBus _eventBus;
        private readonly VillagersFactory _factory;
        private readonly VillagersAIController _ai;

        public VillageController(
            IncrementalController incremental,
            IEventBus eventBus,
            VillagersFactory factory,
            VillagersAIController ai)
        {
            _incremental = incremental;
            _eventBus = eventBus;
            _factory = factory;
            _ai = ai;
        }

        /// <summary>One villager for the starting level, then one per level gained.</summary>
        public int TargetVillagers => _incremental.Level + 1;

        void IStartable.Start()
        {
            _eventBus.Subscribe<LevelUpEvent>(OnLevelUp);

            // Primed, because LevelUpEvent only ever describes a transition — a restored save has
            // already done its levelling and would otherwise show an empty village until the next one.
            _factory.SetTarget(TargetVillagers);
        }

        void ITickable.Tick() => _ai.Tick(Time.deltaTime);

        public void Dispose() => _eventBus.Unsubscribe<LevelUpEvent>(OnLevelUp);

        // Reads the target rather than the event's level so the two paths cannot disagree; SetTarget is
        // idempotent, which matters because one tick can cross several thresholds and fire several times.
        private void OnLevelUp(LevelUpEvent levelUpEvent) => _factory.SetTarget(TargetVillagers);
    }
}
