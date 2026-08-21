using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    /// <summary>
    /// A plot authored into the village. The type is fixed here rather than decided at runtime, so the
    /// layout stays something you arrange by eye instead of something the controller has to infer.
    /// </summary>
    public sealed class PlaceableSpot : MonoBehaviour
    {
        [SerializeField] private PlaceableType type;

        public PlaceableType Type => type;

        /// <summary>What is currently built here, or null while the plot is empty.</summary>
        public PlaceableBase Current { get; private set; }

        public bool IsOccupied => Current != null;

        public void Attach(PlaceableBase placeable) => Current = placeable;

        public void Clear()
        {
            if (Current != null)
            {
                // Deactivated before destroying, not just destroyed: Destroy is deferred to the end of
                // the frame, so a POI on this building would still be found and claimable by the
                // registry refresh that follows immediately.
                Current.gameObject.SetActive(false);
                Object.Destroy(Current.gameObject);
            }

            Current = null;
        }
    }
}
