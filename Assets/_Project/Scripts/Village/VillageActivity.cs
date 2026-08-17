namespace gishadev.companion.Village
{
    /// <summary>
    /// What the village should currently be doing, derived from the pomodoro and the focused app. The
    /// single input the villager AI takes from the rest of the app.
    /// </summary>
    public enum VillageActivity
    {
        /// <summary>Nothing special: wander only. Covers a paused or stopped timer.</summary>
        Neutral = 0,

        /// <summary>A running work session in a productive app. Jobs are claimable.</summary>
        Working = 1,

        /// <summary>A running break. Villagers may hide.</summary>
        Relaxing = 2
    }
}
