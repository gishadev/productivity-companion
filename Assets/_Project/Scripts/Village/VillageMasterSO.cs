using System.Collections.Generic;
using gishadev.companion.Village.Placeables;
using UnityEngine;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Everything the village needs to build itself. Edit-time data only: nothing here is written at
    /// runtime or persisted, which is why it is an asset rather than one of the save-backed settings
    /// classes the rest of the app uses.
    /// </summary>
    [CreateAssetMenu(fileName = "VillageMaster", menuName = "Companion/Village Master")]
    public sealed class VillageMasterSO : ScriptableObject
    {
        [Header("Villagers")]
        [SerializeField] private Villagers.Villager villagerPrefab;

        [Tooltip("One per character. Picked at random per villager and not persisted — re-rolled every " +
                 "launch. An Animator owns the sprite, so variety is a clip set rather than a sprite.")]
        [SerializeField] private AnimatorOverrideController[] villagerVariants;

        [Tooltip("Hard ceiling.")]
        [Min(1)] [SerializeField] private int maxVillagers = 24;

        [Header("Placement")]
        [Tooltip("Best effort only: placement gives up and accepts an overlap once the area is full.")]
        [Min(0f)] [SerializeField] private float minSpacing = 0.12f;

        [Min(1)] [SerializeField] private int placementAttempts = 8;

        [Header("Wandering")]
        [Tooltip("World units per second. The whole village is only ~4.5 units wide, so this is small.")]
        [Min(0f)] [SerializeField] private float walkSpeed = 0.25f;

        [Tooltip("How far a villager will pick its next spot from where it is standing.")]
        [Min(0f)] [SerializeField] private float wanderRadius = 0.8f;

        [Tooltip("Seconds spent standing between walks, as min/max. Randomised so they desynchronise.")]
        [SerializeField] private Vector2 idleDuration = new Vector2(1.5f, 5f);

        [Header("Breaks")]
        [Tooltip("Odds a villager heads indoors when it finishes an idle during a break.")]
        [Range(0f, 1f)] [SerializeField] private float relaxChance = 0.35f;

        [Tooltip("Seconds spent hidden inside, as min/max. A work phase turns them out early regardless.")]
        [SerializeField] private Vector2 hideDuration = new Vector2(5f, 15f);

        [Header("Placeables")]
        [Tooltip("Which prefab each kind of spot builds.")]
        [SerializeField] private PlaceableDefinition[] placeablePrefabs;

        [Tooltip("The village build order: one entry per building, in the order they should appear.")]
        [SerializeField] private PlacementRule[] placementRules;

        [Tooltip("When each tier starts sweeping through the buildings of a type.")]
        [SerializeField] private TierRule[] tierRules;

        public Villagers.Villager VillagerPrefab => villagerPrefab;

        public bool HasVariants => villagerVariants != null && villagerVariants.Length > 0;

        // Re-clamped on read rather than trusted from the inspector: [Min] does not touch values already
        // serialized into an asset, so an older or hand-edited file can still carry anything.
        public int MaxVillagers => Mathf.Max(1, maxVillagers);

        public float MinSpacing => Mathf.Max(0f, minSpacing);

        public int PlacementAttempts => Mathf.Max(1, placementAttempts);

        public float WalkSpeed => Mathf.Max(0f, walkSpeed);

        public float WanderRadius => Mathf.Max(0f, wanderRadius);

        public float RelaxChance => Mathf.Clamp01(relaxChance);

        public IReadOnlyList<PlacementRule> PlacementRules =>
            (IReadOnlyList<PlacementRule>)placementRules ?? System.Array.Empty<PlacementRule>();

        public IReadOnlyList<TierRule> TierRules =>
            (IReadOnlyList<TierRule>)tierRules ?? System.Array.Empty<TierRule>();

        /// <summary>Null when the type has no prefab assigned, which leaves its spots empty.</summary>
        public PlaceableBase PrefabFor(PlaceableType type)
        {
            if (placeablePrefabs == null) return null;

            for (var i = 0; i < placeablePrefabs.Length; i++)
            {
                var definition = placeablePrefabs[i];
                if (definition != null && definition.Type == type) return definition.Prefab;
            }

            return null;
        }

        public float RandomIdleDuration() => RandomInBand(idleDuration);

        public float RandomHideDuration() => RandomInBand(hideDuration);

        public AnimatorOverrideController RandomVariant() =>
            HasVariants ? villagerVariants[Random.Range(0, villagerVariants.Length)] : null;

        // Ordered here rather than trusted from the inspector: a max below the min would otherwise make
        // Random.Range silently return values outside the band the field claims.
        private static float RandomInBand(Vector2 band)
        {
            var min = Mathf.Max(0f, band.x);
            var max = Mathf.Max(min, band.y);

            return Random.Range(min, max);
        }
    }
}
