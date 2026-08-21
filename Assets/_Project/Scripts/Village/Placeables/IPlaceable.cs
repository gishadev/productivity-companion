namespace gishadev.companion.Village.Placeables
{
    /// <summary>Something built on a <see cref="PlaceableSpot"/>.</summary>
    public interface IPlaceable
    {
        PlaceableType Type { get; }

        int Tier { get; }

        /// <summary>Fixes the shape for life. The variant index is what keeps a footprint stable.</summary>
        void Place(int variantIndex);

        /// <summary>Swaps which tier's art the fixed variant index is read from.</summary>
        void ApplyTier(int tier);
    }
}
