using System;
using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    /// <summary>Which prefab a spot of this type builds.</summary>
    [Serializable]
    public sealed class PlaceableDefinition
    {
        [SerializeField] private PlaceableType type;
        [SerializeField] private PlaceableBase prefab;

        public PlaceableType Type => type;

        public PlaceableBase Prefab => prefab;
    }

    /// <summary>
    /// One building appears at this level. Deliberately one entry per placement rather than a cadence:
    /// the array then reads as the village's build order, which is the thing worth tuning by hand.
    /// </summary>
    [Serializable]
    public sealed class PlacementRule
    {
        [Min(0)] [SerializeField] private int level;
        [SerializeField] private PlaceableType type;

        public int Level => Mathf.Max(0, level);

        public PlaceableType Type => type;
    }

    /// <summary>
    /// From <see cref="UnlockLevel"/> the first building of this type reaches this tier, and one more
    /// joins it every <see cref="LevelsPerUpgrade"/> levels until all of them have.
    /// </summary>
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

        // Clamped at the read: a zero here would divide by zero and upgrade the whole village at once.
        public int LevelsPerUpgrade => Mathf.Max(1, levelsPerUpgrade);
    }
}
