namespace gishadev.companion.Village.Villagers
{
    /// <summary>What a villager is currently doing. Ticked by <see cref="VillagersAIController"/>.</summary>
    public enum VillagerState
    {
        Idle = 0,
        Wander = 1,
        GoToJob = 2,
        Working = 3,
        GoToRelax = 4,

        /// <summary>Inside a building: the GameObject is disabled, but the agent keeps ticking.</summary>
        Hiding = 5
    }
}
