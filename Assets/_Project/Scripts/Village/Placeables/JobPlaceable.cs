using gishadev.companion.Village.POI;
using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// A workplace. Deliberately not a <see cref="TieredPlaceable"/>: a job post is one villager at one
    /// fixed spot, so it has nothing to upgrade and no capacity to raise. It either exists or it does
    /// not, and that is the whole of what the level decides about it.
    /// </summary>
    public sealed class JobPlaceable : PlaceableBase
    {
        [SerializeField] private JobPOI jobPoi;

        public override PlaceableType Type => PlaceableType.Job;

        public JobPOI JobPoi => jobPoi;
    }
}
