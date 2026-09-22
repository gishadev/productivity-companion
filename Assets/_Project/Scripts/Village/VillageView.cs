using UnityEngine;

namespace gishadev.companion.Village
{
    public sealed class VillageView : MonoBehaviour
    {
        [Tooltip("Villagers are parented here. Falls back to this object when unassigned.")]
        [SerializeField] private Transform villagersRoot;

        [Tooltip("Bounds villagers spawn inside. Without it they all stack on the root.")]
        [SerializeField] private WalkableArea walkableArea;

        public Transform VillagersRoot => villagersRoot != null ? villagersRoot : transform;

        public WalkableArea WalkableArea => walkableArea;
    }
}
