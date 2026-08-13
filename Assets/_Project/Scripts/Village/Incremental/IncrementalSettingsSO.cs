using UnityEngine;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Balance data for the progression mechanic. An asset rather than the plain settings class the rest
    /// of the app uses, because nothing here is user-editable or written at runtime: it ships with the
    /// build and is tuned in the inspector, so it has no business in the save file.
    /// </summary>
    [CreateAssetMenu(fileName = "IncrementalSettings", menuName = "Companion/Incremental Settings")]
    public sealed class IncrementalSettingsSO : ScriptableObject
    {
        [Header("Progression")]
        [Min(0f)] [SerializeField] private float basePointsPerSecond = 1f;
        [Min(0f)] [SerializeField] private float regularMultiplier = 1f;
        [Min(0f)] [SerializeField] private float productiveMultiplier = 2.5f;

        [Tooltip("Points needed for level 1. Each level after costs this times the growth factor.")]
        [Min(1f)] [SerializeField] private float baseThreshold = 60f;

        [Min(1f)] [SerializeField] private float growthFactor = 1.35f;

        [Tooltip("Scales progression during a pomodoro break. Work always runs at full rate.")]
        [Range(0f, 1f)] [SerializeField] private float breakMultiplier = 0.25f;

        [Header("Penalty")]
        [Tooltip("Continuous unproductive seconds before the penalty fires.")]
        [Min(1f)] [SerializeField] private float maxPenaltySeconds = 120f;

        [Tooltip("Penalty seconds paid back per second spent in a regular app.")]
        [Min(0f)] [SerializeField] private float regularRecoveryPerSecond = 1f;

        [Tooltip("Penalty seconds paid back per second spent in a productive app.")]
        [Min(0f)] [SerializeField] private float productiveRecoveryPerSecond = 2f;

        public float BasePointsPerSecond => Mathf.Max(0f, basePointsPerSecond);

        public float RegularMultiplier => Mathf.Max(0f, regularMultiplier);

        public float ProductiveMultiplier => Mathf.Max(0f, productiveMultiplier);

        // Re-clamped here, not just via [Min]: the attribute does not touch values already serialized
        // into an asset, and a threshold of zero clears every level instantly and unbounds the level-up
        // loop. Same for a growth factor below 1, which shrinks thresholds toward zero as the level rises.
        public float BaseThreshold => Mathf.Max(1f, baseThreshold);

        public float GrowthFactor => Mathf.Max(1f, growthFactor);

        public float BreakMultiplier => Mathf.Clamp01(breakMultiplier);

        public float MaxPenaltySeconds => Mathf.Max(1f, maxPenaltySeconds);

        public float RegularRecoveryPerSecond => Mathf.Max(0f, regularRecoveryPerSecond);

        public float ProductiveRecoveryPerSecond => Mathf.Max(0f, productiveRecoveryPerSecond);
    }
}
