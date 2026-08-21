using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// Something built on a <see cref="PlaceableSpot"/>. The base assumes nothing varies: a building
    /// that never changes its look ignores both hooks and needs no data to do so.
    /// </summary>
    public abstract class PlaceableBase : MonoBehaviour, IPlaceable
    {
        public abstract PlaceableType Type { get; }

        public int Tier { get; protected set; }

        /// <summary>How many shapes are on offer. Zero when the building has only one look.</summary>
        public virtual int VariantCount => 0;

        public virtual void Place(int variantIndex)
        {
        }

        public virtual void ApplyTier(int tier)
        {
        }
    }
}
