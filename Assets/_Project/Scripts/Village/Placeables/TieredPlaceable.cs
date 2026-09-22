using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    public abstract class TieredPlaceable : PlaceableBase
    {
        [Tooltip("Serialized rather than fetched: the renderer lives on a child on every prefab here.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("The upgrade ladder. Shared, so retiming or repainting art does not touch this prefab.")]
        [SerializeField] private PlaceableTiersSO tiers;

        private int _variantIndex;

        protected PlaceableTiersSO Tiers => tiers;

        public int VariantIndex => _variantIndex;

        public int TierCount => tiers != null ? tiers.TierCount : 0;

        public override int VariantCount => tiers != null ? tiers.VariantCount : 0;

        public override void Place(int variantIndex)
        {
            _variantIndex = Mathf.Max(0, variantIndex);
            ApplyTier(0);
        }

        public override void ApplyTier(int tier)
        {
            if (tiers == null || tiers.TierCount == 0) return;

            Tier = Mathf.Clamp(tier, 0, tiers.TierCount - 1);

            var sprite = tiers.SpriteFor(Tier, _variantIndex);
            if (spriteRenderer != null && sprite != null) spriteRenderer.sprite = sprite;

            OnTierApplied(Tier);
        }

        protected virtual void OnTierApplied(int tier)
        {
        }
    }
}
