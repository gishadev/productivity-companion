using gishadev.companion.Village.POI;
using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    public sealed class JobPlaceable : PlaceableBase
    {
        [SerializeField] private JobPOI jobPoi;

        public override PlaceableType Type => PlaceableType.Job;

        public JobPOI JobPoi => jobPoi;
    }
}
