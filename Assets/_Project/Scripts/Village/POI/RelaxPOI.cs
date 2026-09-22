using UnityEngine;

namespace gishadev.companion.Village.POI
{
    public sealed class RelaxPOI : VillagePOI
    {
        [Tooltip("The doorway. Villagers walk here, vanish, and reappear here.")]
        [SerializeField] private Transform enterPos;

        [Tooltip("How many villagers fit inside. Above 1 on purpose: with few buildings and many " +
                 "villagers, a capacity of 1 makes hiding almost never visible.")]
        [Min(1)] [SerializeField] private int capacity = 3;

        protected override Transform Target => enterPos;

        // Shrinking never evicts; it only refuses new claims until back under the limit.
        public void SetCapacity(int value) => capacity = Mathf.Max(1, value);

        protected override int Capacity => Mathf.Max(1, capacity);
    }
}
