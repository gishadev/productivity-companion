using UnityEngine;

namespace gishadev.companion.Village.Villagers
{
    // View only; state lives in VillagersAIController.
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

        public void SetVariant(AnimatorOverrideController variant)
        {
            if (animator != null && variant != null) animator.runtimeAnimatorController = variant;
        }

        // Keeps facing when speed is zero.
        public void SetMovement(Vector2 direction, float speed)
        {
            var facing = Cardinal(direction);

            if (spriteRenderer != null && !Mathf.Approximately(facing.x, 0f))
                spriteRenderer.flipX = facing.x < 0f;

            if (animator == null) return;

            // Side clips face right and are mirrored, so the blend tree only sees non-negative X.
            animator.SetFloat(MoveXHash, Mathf.Abs(facing.x));
            animator.SetFloat(MoveYHash, facing.y);
            animator.SetFloat(SpeedHash, speed);
        }

        // Sprite clips can't blend, so a diagonal heading flickers between facings. Snap the facing
        // (not the path) to an axis.
        private static Vector2 Cardinal(Vector2 direction)
        {
            if (direction.sqrMagnitude < Mathf.Epsilon) return Vector2.down;

            // Ties go horizontal.
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