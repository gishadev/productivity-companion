using gishadev.companion.Village.POI;
using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    public sealed class HousePlaceable : TieredPlaceable
    {
        [SerializeField] private RelaxPOI relaxPoi;

        public override PlaceableType Type => PlaceableType.House;

        public RelaxPOI RelaxPoi => relaxPoi;

        protected override void OnTierApplied(int tier)
        {
            if (relaxPoi != null && Tiers != null) relaxPoi.SetCapacity(Tiers.CapacityFor(tier));
        }
    }
}
