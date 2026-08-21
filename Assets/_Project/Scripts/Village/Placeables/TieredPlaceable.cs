using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// A placeable whose look changes with the level. It keeps a fixed shape for life and swaps which
    /// tier's art that shape is drawn from; the art itself lives in a shared asset.
    /// </summary>
    public abstract class TieredPlaceable : PlaceableBase
    {
        [Tooltip("Serialized rather than fetched: the renderer lives on a child on every prefab here.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("The upgrade ladder. Shared, so retiming or repainting art does not touch this prefab.")]
        [SerializeField] private PlaceableTiersSO tiers;

        private int _variantIndex;

        /// <summary>The ladder, for subclasses that read more from a tier than its sprite.</summary>
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

            // Clamping lives in the asset, next to the data that decides what is out of range.
            var sprite = tiers.SpriteFor(Tier, _variantIndex);
            if (spriteRenderer != null && sprite != null) spriteRenderer.sprite = sprite;

            OnTierApplied(Tier);
        }

        /// <summary>Hook for whatever else a tier means to this kind of building.</summary>
        protected virtual void OnTierApplied(int tier)
        {
        }
    }
}
