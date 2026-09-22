using System;
using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    // Every tier must list the same shapes in the same order (checked in OnValidate).
    [CreateAssetMenu(fileName = "PlaceableTiers", menuName = "Companion/Placeable Tiers")]
    public sealed class PlaceableTiersSO : ScriptableObject
    {
        [Tooltip("Lowest tier first. Every tier lists the same shapes in the same order — the index is " +
                 "the shape, the tier is the material.")]
        [SerializeField] private PlaceableTier[] tiers;

        public int TierCount => tiers?.Length ?? 0;

        public int VariantCount => VariantsAt(0)?.Length ?? 0;

        // Only read by placeables that own a RelaxPOI.
        public int CapacityFor(int tier)
        {
            if (TierCount == 0) return 1;

            return tiers[Mathf.Clamp(tier, 0, TierCount - 1)].Capacity;
        }

        public Sprite SpriteFor(int tier, int variantIndex)
        {
            if (TierCount == 0) return null;

            var variants = VariantsAt(Mathf.Clamp(tier, 0, TierCount - 1));
            if (variants == null || variants.Length == 0) return null;

            // Clamped, not wrapped, so a short tier falls back to its last shape.
            return variants[Mathf.Clamp(variantIndex, 0, variants.Length - 1)];
        }

        private Sprite[] VariantsAt(int tier) =>
            tiers != null && tier >= 0 && tier < tiers.Length ? tiers[tier].Variants : null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (TierCount < 2) return;

            var expected = VariantCount;
            for (var i = 1; i < TierCount; i++)
            {
                var count = VariantsAt(i)?.Length ?? 0;
                if (count == expected) continue;

                Debug.LogWarning(
                    $"[Village] {name}: tier {i} lists {count} shapes but tier 0 lists {expected}. " +
                    "Buildings using the missing indices will fall back to a different shape.", this);
                return;
            }
        }
#endif

        [Serializable]
        private sealed class PlaceableTier
        {
            [Tooltip("One per shape. Keep the order identical across tiers.")]
            [SerializeField] private Sprite[] variants;

            [Tooltip("Villagers sheltered at this tier. Read only by placeables that own a RelaxPOI.")]
            [Min(1)] [SerializeField] private int capacity = 3;

            public Sprite[] Variants => variants;

            public int Capacity => Mathf.Max(1, capacity);
        }
    }
}
