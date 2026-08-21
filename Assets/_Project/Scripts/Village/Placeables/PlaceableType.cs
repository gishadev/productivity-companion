namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// What kind of thing a spot holds. Fixed per spot at authoring time, so the controller never has
    /// to decide what belongs where — only how many of each the current level has earned.
    /// </summary>
    public enum PlaceableType
    {
        House = 0,
        Tree = 1,
        Job = 2
    }
}
