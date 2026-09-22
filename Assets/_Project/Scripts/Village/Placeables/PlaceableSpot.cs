using UnityEngine;

namespace gishadev.companion.Village.Placeables
{
    public sealed class PlaceableSpot : MonoBehaviour
    {
        [SerializeField] private PlaceableType type;

        public PlaceableType Type => type;

        public PlaceableBase Current { get; private set; }

        public bool IsOccupied => Current != null;

        public void Attach(PlaceableBase placeable) => Current = placeable;

        public void Clear()
        {
            if (Current != null)
            {
                // Deactivate first: Destroy is deferred, and the registry refresh that follows would still find its POIs.
                Current.gameObject.SetActive(false);
                Object.Destroy(Current.gameObject);
            }

            Current = null;
        }
    }
}
