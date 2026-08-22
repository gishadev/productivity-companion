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
            var facing = Cardinal(direction);

            if (spriteRenderer != null && !Mathf.Approximately(facing.x, 0f))
                spriteRenderer.flipX = facing.x < 0f;

            if (animator == null) return;

            // The side clips are authored facing right and mirrored for left, so the blend tree only
            // ever sees a non-negative X — otherwise it would need left-facing clips that do not exist.
            animator.SetFloat(MoveXHash, Mathf.Abs(facing.x));
            animator.SetFloat(MoveYHash, facing.y);
            animator.SetFloat(SpeedHash, speed);
        }

        /// <summary>
        /// Quantises a heading to the axis it leans on. Sprite clips cannot be interpolated, so a 2D
        /// blend tree does not blend them — it shows whichever clip currently carries the most weight.
        /// A diagonal heading sits between two clips at near-equal weight, where the smallest numerical
        /// wobble flips which one wins and the villager visibly flickers between facings. Snapping is
        /// what makes the choice deterministic; it costs nothing visually, because a three-clip tree was
        /// only ever going to show one of three facings anyway.
        ///
        /// Movement itself stays diagonal. This quantises the *facing*, not the path.
        /// </summary>
        private static Vector2 Cardinal(Vector2 direction)
        {
            if (direction.sqrMagnitude < Mathf.Epsilon) return Vector2.down;

            // Ties go horizontal: a walk at exactly 45 degrees reads better on the side clip than on
            // the back or front, and picking a side deterministically is the whole point.
            return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? new Vector2(Mathf.Sign(direction.x), 0f)
                : new Vector2(0f, Mathf.Sign(direction.y));
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