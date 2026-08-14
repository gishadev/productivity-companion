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

        [Tooltip("Picked at random per villager. Not persisted — re-rolled every launch.")]
        [SerializeField] private Sprite[] villagerSprites;

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

        public Villagers.Villager VillagerPrefab => villagerPrefab;

        public bool HasSprites => villagerSprites != null && villagerSprites.Length > 0;

        // Re-clamped on read rather than trusted from the inspector: [Min] does not touch values already
        // serialized into an asset, so an older or hand-edited file can still carry anything.
        public int MaxVillagers => Mathf.Max(1, maxVillagers);

        public float MinSpacing => Mathf.Max(0f, minSpacing);

        public int PlacementAttempts => Mathf.Max(1, placementAttempts);

        public float WalkSpeed => Mathf.Max(0f, walkSpeed);

        public float WanderRadius => Mathf.Max(0f, wanderRadius);

        public float RandomIdleDuration()
        {
            // Ordered here rather than trusted from the inspector: a max below the min would otherwise
            // make Random.Range silently return values outside the band the field claims.
            var min = Mathf.Max(0f, idleDuration.x);
            var max = Mathf.Max(min, idleDuration.y);

            return Random.Range(min, max);
        }

        public Sprite RandomSprite() =>
            HasSprites ? villagerSprites[Random.Range(0, villagerSprites.Length)] : null;
    }
}
