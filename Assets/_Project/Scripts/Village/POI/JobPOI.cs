using UnityEngine;

namespace gishadev.companion.Village.POI
{
    /// <summary>
    /// A work post. Claimable only while the user is in a productive app during a running work session;
    /// the villager who takes it plays the job animation until that stops being true.
    /// </summary>
    public sealed class JobPOI : VillagePOI
    {
        [Tooltip("Where the villager stands to work. Falls back to this object when unassigned.")]
        [SerializeField] private Transform workPos;

        /// <summary>One villager at a time, so occupancy is a flag rather than a count.</summary>
        public bool IsOccupied => Occupants > 0;

        protected override Transform Target => workPos;

        protected override int Capacity => 1;
    }
}
