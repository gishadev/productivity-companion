using System;
using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// A building's whole upgrade ladder, as an asset. Held apart from the prefab so art can be
    /// retimed or repainted without dirtying the prefab, and so several prefabs can share one ladder.
    ///
    /// The ladder is one asset rather than one asset per tier on purpose: every tier has to list the
    /// same shapes in the same order, and that contract is only checkable if the tiers sit side by side
    /// in a single inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "PlaceableTiers", menuName = "Companion/Placeable Tiers")]
    public sealed class PlaceableTiersSO : ScriptableObject
    {
        [Tooltip("Lowest tier first. Every tier lists the same shapes in the same order — the index is " +
                 "the shape, the tier is the material.")]
        [SerializeField] private PlaceableTier[] tiers;

        public int TierCount => tiers?.Length ?? 0;

        /// <summary>How many shapes are on offer. Read from tier 0, which every tier mirrors.</summary>
        public int VariantCount => VariantsAt(0)?.Length ?? 0;

        /// <summary>
        /// How many villagers the building shelters at this tier. Only meaningful where the placeable
        /// owns a RelaxPOI; ladders for scenery leave it at its default and nothing reads it.
        /// </summary>
        public int CapacityFor(int tier)
        {
            if (TierCount == 0) return 1;

            return tiers[Mathf.Clamp(tier, 0, TierCount - 1)].Capacity;
        }

        /// <summary>Both indices are clamped, so an under-filled ladder degrades instead of throwing.</summary>
        public Sprite SpriteFor(int tier, int variantIndex)
        {
            if (TierCount == 0) return null;

            var variants = VariantsAt(Mathf.Clamp(tier, 0, TierCount - 1));
            if (variants == null || variants.Length == 0) return null;

            // Clamped rather than wrapped: a tier authored with fewer shapes than its neighbours should
            // fall back to its last one, not silently draw a different building.
            return variants[Mathf.Clamp(variantIndex, 0, variants.Length - 1)];
        }

        private Sprite[] VariantsAt(int tier) =>
            tiers != null && tier >= 0 && tier < tiers.Length ? tiers[tier].Variants : null;

#if UNITY_EDITOR
        // The shape-index contract spans tiers, so a mismatched row means some building silently swaps
        // shape on upgrade. Cheap to catch here; invisible at runtime.
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
