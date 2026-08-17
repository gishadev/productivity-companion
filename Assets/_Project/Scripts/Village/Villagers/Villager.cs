using UnityEngine;

namespace gishadev.companion.Village.Villagers
{
    /// <summary>
    /// Renders one villager and nothing else. State lives in <see cref="VillagersAIController"/>, which
    /// writes into this — there is deliberately no per-villager logic here to multiply by the villager
    /// count.
    /// </summary>
    public sealed class Villager : MonoBehaviour
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int WorkingHash = Animator.StringToHash("Working");

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        private void Awake()
        {
            if (spriteRenderer == null)
                Debug.LogError($"[Village] {name}: no sprite renderer assigned; the villager is invisible.", this);

            if (animator == null)
                Debug.LogError($"[Village] {name}: no animator assigned; the villager will not animate.", this);
        }

        /// <summary>Which character this villager is. One controller, one clip set per character.</summary>
        public void SetVariant(AnimatorOverrideController variant)
        {
            if (animator != null && variant != null) animator.runtimeAnimatorController = variant;
        }

        /// <summary>
        /// Direction is kept when <paramref name="speed"/> is zero, so a villager idles facing wherever
        /// it last walked instead of snapping back to a default pose.
        /// </summary>
        public void SetMovement(Vector2 direction, float speed)
        {
            if (spriteRenderer != null && !Mathf.Approximately(direction.x, 0f))
                spriteRenderer.flipX = direction.x < 0f;

            if (animator == null) return;

            // The side clips are authored facing right and mirrored for left, so the blend tree only
            // ever sees a non-negative X — otherwise it would need left-facing clips that do not exist.
            animator.SetFloat(MoveXHash, Mathf.Abs(direction.x));
            animator.SetFloat(MoveYHash, direction.y);
            animator.SetFloat(SpeedHash, speed);
        }

        public void SetWorking(bool working)
        {
            if (animator != null) animator.SetBool(WorkingHash, working);
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }

        public void SetSortingOrder(int order)
        {
            if (spriteRenderer != null && spriteRenderer.sortingOrder != order)
                spriteRenderer.sortingOrder = order;
        }
    }
}