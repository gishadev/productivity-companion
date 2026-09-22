using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    public abstract class PlaceableBase : MonoBehaviour, IPlaceable
    {
        public abstract PlaceableType Type { get; }

        public int Tier { get; protected set; }

        public virtual int VariantCount => 0;

        public virtual void Place(int variantIndex)
        {
        }

        public virtual void ApplyTier(int tier)
        {
        }
    }
}
