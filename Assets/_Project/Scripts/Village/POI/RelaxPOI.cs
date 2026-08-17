using UnityEngine;

namespace gishadev.companion.Village.POI
{
    /// <summary>
    /// Somewhere to disappear into during a break — a house, a tent. The villager walks to the entrance,
    /// is switched off for a while, and reappears in the same spot.
    /// </summary>
    public sealed class RelaxPOI : VillagePOI
    {
        [Tooltip("The doorway. Villagers walk here, vanish, and reappear here.")]
        [SerializeField] private Transform enterPos;

        [Tooltip("How many villagers fit inside. Above 1 on purpose: with few buildings and many " +
                 "villagers, a capacity of 1 makes hiding almost never visible.")]
        [Min(1)] [SerializeField] private int capacity = 3;

        protected override Transform Target => enterPos;

        protected override int Capacity => Mathf.Max(1, capacity);
    }
}
