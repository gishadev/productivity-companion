using UnityEngine;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Depth-sorts a prop that does not move — houses, scenery. Villagers are sorted by
    /// <see cref="Villagers.VillagersAIController"/> as part of the move they already do, so they
    /// deliberately do not carry this: an Update per prop is affordable, an Update per villager is the
    /// thing the whole AI design exists to avoid.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StaticYSort : MonoBehaviour
    {
        [Tooltip("Nudges this sprite in front of (+) or behind (-) others at the same height.")]
        [SerializeField] private int orderOffset;

        private SpriteRenderer _renderer;

        private void OnEnable() => Apply();

        private void Apply()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) return;

            var order = YSorting.OrderFor(transform.position.y) + orderOffset;
            if (_renderer.sortingOrder == order) return;

            _renderer.sortingOrder = order;
        }

#if UNITY_EDITOR
        private void OnValidate() => Apply();

        // Re-sorts while a prop is being dragged around the scene, which is the only time a "static"
        // sprite actually moves. Apply is a no-op once the order settles, so this costs nothing.
        private void Update()
        {
            if (Application.isPlaying) return;

            Apply();
        }
#endif
    }
}
