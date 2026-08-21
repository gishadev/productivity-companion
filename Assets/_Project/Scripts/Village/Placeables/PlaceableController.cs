using System;
using System.Collections.Generic;
using gishadev.companion.Events;
using gishadev.companion.Village.POI;
using gishadev.tools.Events;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// Builds the village out of the current level. Like the villager population, this reconciles to a
    /// target rather than reacting to individual level-ups: a save restored at level 50 has to produce
    /// the right village in one pass, with no history to replay.
    /// </summary>
    public sealed class PlaceableController : IStartable, IDisposable
    {
        private readonly VillageMasterSO _master;
        private readonly IncrementalController _incremental;
        private readonly IEventBus _eventBus;
        private readonly POIRegistry _pois;

        private readonly Dictionary<PlaceableType, TypeState> _states =
            new Dictionary<PlaceableType, TypeState>();

        public PlaceableController(
            VillageMasterSO master,
            IncrementalController incremental,
            IEventBus eventBus,
            POIRegistry pois)
        {
            _master = master;
            _incremental = incremental;
            _eventBus = eventBus;
            _pois = pois;
        }

        void IStartable.Start()
        {
            ScanSpots();

            _eventBus.Subscribe<LevelUpEvent>(OnLevelUp);

            // Primed for the same reason the villager population is: LevelUpEvent only ever describes a
            // transition, so a restored save would otherwise stand in an empty village until it levelled.
            Apply(_incremental.Level);
        }

        public void Dispose() => _eventBus.Unsubscribe<LevelUpEvent>(OnLevelUp);

        /// <summary>Idempotent, so a tick crossing several levels still lands on the right village.</summary>
        public void Apply(int level)
        {
            var changed = false;

            foreach (var pair in _states)
            {
                var state = pair.Value;
                var due = Mathf.Min(DueCount(pair.Key, level), state.Spots.Count);

                changed |= Reconcile(state, due);
                ApplyTiers(pair.Key, state, due, level);
            }

            // Placed buildings carry the POIs villagers claim, and the registry caches its scan. Without
            // this a house built mid-session is never found, and the failure is completely silent.
            if (changed) _pois.Refresh();
        }

        private bool Reconcile(TypeState state, int due)
        {
            var changed = false;

            for (var i = 0; i < state.Spots.Count; i++)
            {
                var spot = state.Spots[i];
                if (spot == null) continue;

                var wanted = i < due;
                if (wanted == spot.IsOccupied) continue;

                if (wanted) Build(spot, state.VariantIndex[i]);
                else spot.Clear();

                changed = true;
            }

            return changed;
        }

        private void ApplyTiers(PlaceableType type, TypeState state, int due, int level)
        {
            for (var i = 0; i < due; i++)
            {
                var spot = state.Spots[i];
                if (spot == null || !spot.IsOccupied) continue;

                spot.Current.ApplyTier(TierFor(type, state, i, due, level));
            }
        }

        // The displayed tier is the highest one this building qualifies for, so overlapping rules
        // degrade gracefully instead of fighting: a building can reach tier 3 without having visibly
        // passed through tier 2, and simply shows tier 3.
        private int TierFor(PlaceableType type, TypeState state, int spotIndex, int placedCount, int level)
        {
            var tier = 0;
            var rules = _master.TierRules;

            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null || rule.Type != type || rule.Tier <= tier) continue;

                var upgraded = UpgradedCount(rule, level, placedCount);
                if (upgraded <= 0) continue;

                if (state.RankFor(rule.Tier, spotIndex) < upgraded) tier = rule.Tier;
            }

            return tier;
        }

        private static int UpgradedCount(TierRule rule, int level, int placedCount)
        {
            if (level < rule.UnlockLevel) return 0;

            var steps = (level - rule.UnlockLevel) / rule.LevelsPerUpgrade + 1;
            return Mathf.Clamp(steps, 0, placedCount);
        }

        private int DueCount(PlaceableType type, int level)
        {
            var count = 0;
            var rules = _master.PlacementRules;

            for (var i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule != null && rule.Type == type && rule.Level <= level) count++;
            }

            return count;
        }

        private void Build(PlaceableSpot spot, int variantIndex)
        {
            var prefab = _master.PrefabFor(spot.Type);
            if (prefab == null) return;

            // Parented to the spot so the hierarchy stays readable and clearing a plot cannot orphan
            // whatever was standing on it.
            var instance = Object.Instantiate(
                prefab, spot.transform.position, spot.transform.rotation, spot.transform);

            instance.Place(variantIndex);
            spot.Attach(instance);
        }

        private void OnLevelUp(LevelUpEvent levelUpEvent) => Apply(_incremental.Level);

        private void ScanSpots()
        {
            _states.Clear();

            // Inactive included, matching every other village scene lookup: the world can be built while
            // the widget is hidden.
            var spots = Object.FindObjectsByType<PlaceableSpot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var spot in spots)
            {
                if (!_states.TryGetValue(spot.Type, out var state))
                {
                    state = new TypeState();
                    _states[spot.Type] = state;
                }

                state.Spots.Add(spot);
            }

            foreach (var pair in _states)
                pair.Value.Seed(_master.PrefabFor(pair.Key));
        }

        /// <summary>Every spot of one type, plus the random draws that stay fixed for the session.</summary>
        private sealed class TypeState
        {
            public readonly List<PlaceableSpot> Spots = new List<PlaceableSpot>();

            // Upgrade order, one draw per tier: the house that led the sweep to tier 2 is deliberately
            // not the one guaranteed to lead the sweep to tier 3.
            private readonly Dictionary<int, int[]> _ranksByTier = new Dictionary<int, int[]>();

            public int[] VariantIndex { get; private set; } = Array.Empty<int>();

            /// <summary>
            /// The shape each spot draws, fixed for the session. Never re-rolled on upgrade: the house
            /// shapes have different footprints, so a spot that changed shape mid-game would resize and
            /// could grow into its neighbour. The tier changes the material, not the silhouette.
            /// </summary>
            public void Seed(PlaceableBase prefab)
            {
                var variants = prefab != null ? prefab.VariantCount : 0;

                VariantIndex = new int[Spots.Count];
                for (var i = 0; i < VariantIndex.Length; i++)
                    VariantIndex[i] = variants > 0 ? Random.Range(0, variants) : 0;
            }

            public int RankFor(int tier, int spotIndex)
            {
                if (!_ranksByTier.TryGetValue(tier, out var ranks) || ranks.Length != Spots.Count)
                {
                    ranks = BuildRanks(Spots.Count);
                    _ranksByTier[tier] = ranks;
                }

                // Out of range would mean the spot list changed under us; reporting the worst rank keeps
                // that spot un-upgraded rather than throwing.
                return spotIndex >= 0 && spotIndex < ranks.Length ? ranks[spotIndex] : int.MaxValue;
            }

            // Fisher-Yates, inverted into ranks so the per-spot lookup is an index rather than a search.
            private static int[] BuildRanks(int count)
            {
                var order = new int[count];
                for (var i = 0; i < count; i++) order[i] = i;

                for (var i = count - 1; i > 0; i--)
                {
                    var j = Random.Range(0, i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                var ranks = new int[count];
                for (var position = 0; position < count; position++) ranks[order[position]] = position;

                return ranks;
            }
        }
    }
}
