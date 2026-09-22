using UnityEngine;

namespace gishadev.companion.Village.POI
{
    public sealed class JobPOI : VillagePOI
    {
        [Tooltip("Where the villager stands to work. Falls back to this object when unassigned.")]
        [SerializeField] private Transform workPos;

        public bool IsOccupied => Occupants > 0;

        protected override Transform Target => workPos;

        protected override int Capacity => 1;
    }
}
