namespace gishadev.companion.Village.Placeables
{
    public interface IPlaceable
    {
        PlaceableType Type { get; }

        int Tier { get; }

        // Fixes the shape for life.
        void Place(int variantIndex);

        void ApplyTier(int tier);
    }
}
