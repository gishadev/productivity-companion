namespace gishadev.companion.Village.Villagers
{
    public enum VillagerState
    {
        Idle = 0,
        Wander = 1,
        GoToJob = 2,
        Working = 3,
        GoToRelax = 4,

        // GameObject disabled, agent still ticks.
        Hiding = 5
    }
}
