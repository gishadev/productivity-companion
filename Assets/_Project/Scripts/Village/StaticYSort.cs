using UnityEngine;

namespace gishadev.companion.Village
{
    // For props that don't move. Villagers are sorted by VillagersAIController instead.
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

        // Only matters while a prop is dragged in the editor; a no-op once settled.
        private void Update()
        {
            if (Application.isPlaying) return;

            Apply();
        }
#endif
    }
}
