using UnityEngine;

namespace gishadev.companion.Village.POI
{
    /// <summary>
    /// Somewhere a villager can go and do something. Placed in the scene; the shared base exists so the
    /// AI can hold one reference type and the registry one list, whatever the villager is going there for.
    /// </summary>
    public abstract class VillagePOI : MonoBehaviour
    {
        private int _occupants;

        /// <summary>Where the villager stands. Falls back to this object when unassigned.</summary>
        public Vector2 TargetPosition => Target != null ? (Vector2)Target.position : (Vector2)transform.position;

        public int Occupants => _occupants;

        public bool HasSpace => _occupants < Capacity;

        protected abstract Transform Target { get; }

        protected abstract int Capacity { get; }

        public bool TryClaim()
        {
            if (!HasSpace) return false;

            _occupants++;
            return true;
        }

        // Clamped rather than just decremented: a release that outnumbers its claim would drive the
        // count negative and leave the POI permanently claimable by any number of villagers.
        public void Release() => _occupants = Mathf.Max(0, _occupants - 1);

        // The AI holds POI references across state changes, so a disabled POI has to shed its occupants
        // or it comes back still believing it is full.
        protected virtual void OnDisable() => _occupants = 0;
    }
}
