using UnityEngine;

namespace gishadev.companion.Village.POI
{
    public abstract class VillagePOI : MonoBehaviour
    {
        private int _occupants;

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

        // Clamped so an extra release can't make the POI claimable by everyone.
        public void Release() => _occupants = Mathf.Max(0, _occupants - 1);

        // The AI holds POI references across disables, so shed occupants here.
        protected virtual void OnDisable() => _occupants = 0;
    }
}
