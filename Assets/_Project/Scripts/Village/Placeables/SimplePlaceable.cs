namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// Scenery — trees, wells, fences. Tiered, because growth stages are the obvious next thing to want
    /// from it, but it carries no behaviour of its own.
    /// </summary>
    public sealed class SimplePlaceable : TieredPlaceable
    {
        public override PlaceableType Type => PlaceableType.Tree;
    }
}
