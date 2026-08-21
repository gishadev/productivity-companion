using gishadev.companion.Village.POI;
using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// A home. Upgrading it is not only cosmetic: each tier also sets how many villagers the building
    /// can shelter during a break.
    /// </summary>
    public sealed class HousePlaceable : TieredPlaceable
    {
        [SerializeField] private RelaxPOI relaxPoi;

        public override PlaceableType Type => PlaceableType.House;

        public RelaxPOI RelaxPoi => relaxPoi;

        // Capacity is read from the same tier row as the sprite rather than a parallel array here: two
        // tier-indexed collections in two files are two things that can silently fall out of step.
        protected override void OnTierApplied(int tier)
        {
            if (relaxPoi != null && Tiers != null) relaxPoi.SetCapacity(Tiers.CapacityFor(tier));
        }
    }
}
