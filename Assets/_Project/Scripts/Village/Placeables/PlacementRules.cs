using System;
using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    [Serializable]
    public sealed class PlaceableDefinition
    {
        [SerializeField] private PlaceableType type;
        [SerializeField] private PlaceableBase prefab;

        public PlaceableType Type => type;

        public PlaceableBase Prefab => prefab;
    }

    [Serializable]
    public sealed class PlacementRule
    {
        [Min(0)] [SerializeField] private int level;
        [SerializeField] private PlaceableType type;

        public int Level => Mathf.Max(0, level);

        public PlaceableType Type => type;
    }

    // From UnlockLevel the first building reaches Tier, then one more every LevelsPerUpgrade levels.
    [Serializable]
    public sealed class TierRule
    {
        [SerializeField] private PlaceableType type;

        [Tooltip("Index into the placeable's tiers array. Tier 0 is the starting look.")]
        [Min(1)] [SerializeField] private int tier = 1;

        [Min(0)] [SerializeField] private int unlockLevel;

        [Min(1)] [SerializeField] private int levelsPerUpgrade = 1;

        public PlaceableType Type => type;

        public int Tier => Mathf.Max(1, tier);

        public int UnlockLevel => Mathf.Max(0, unlockLevel);

        // Zero would upgrade the whole village at once.
        public int LevelsPerUpgrade => Mathf.Max(1, levelsPerUpgrade);
    }
}
