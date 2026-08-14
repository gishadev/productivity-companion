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
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (spriteRenderer == null)
                Debug.LogError($"[Village] {name}: no sprite renderer assigned; the villager is invisible.", this);
        }

        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        }

        /// <summary>Which way the villager faces; the art is authored facing right.</summary>
        public void SetFacingLeft(bool facingLeft)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = facingLeft;
        }

        public void SetSortingOrder(int order)
        {
            if (spriteRenderer != null && spriteRenderer.sortingOrder != order)
                spriteRenderer.sortingOrder = order;
        }
    }
}
