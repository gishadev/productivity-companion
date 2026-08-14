using UnityEngine;

namespace gishadev.companion.Village
{
    /// <summary>
    /// The ground villagers may occupy, authored as a box in the scene rather than as numbers in an
    /// asset: the area has to line up with the ground art, which is something you place by eye.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WalkableArea : MonoBehaviour
    {
        private BoxCollider2D _box;

        public BoxCollider2D Box
        {
            get
            {
                if (_box == null) _box = GetComponent<BoxCollider2D>();
                return _box;
            }
        }

        /// <summary>
        /// Derived from the collider's offset and size rather than read from <c>Collider2D.bounds</c>,
        /// which physics only fills in for a collider that is enabled and on an active object. The
        /// village can be built while hidden, and a zero bounds would silently stack every villager on
        /// one point. Rotation is ignored — a rotated spawn box is not a case worth supporting.
        /// </summary>
        public Bounds WorldBounds
        {
            get
            {
                var box = Box;
                var scale = transform.lossyScale;
                var size = new Vector3(
                    Mathf.Abs(box.size.x * scale.x),
                    Mathf.Abs(box.size.y * scale.y),
                    0f);

                return new Bounds(transform.TransformPoint(box.offset), size);
            }
        }

        public Vector2 RandomPoint()
        {
            var bounds = WorldBounds;
            return new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y));
        }

        /// <summary>Nearest point inside the area, so a wander target is always reachable ground.</summary>
        public Vector2 ClampInside(Vector2 world)
        {
            var bounds = WorldBounds;
            return new Vector2(
                Mathf.Clamp(world.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(world.y, bounds.min.y, bounds.max.y));
        }

        public bool Contains(Vector2 world)
        {
            var bounds = WorldBounds;
            return world.x >= bounds.min.x && world.x <= bounds.max.x &&
                   world.y >= bounds.min.y && world.y <= bounds.max.y;
        }

        // The collider marks out an area, it does not block anything.
        private void Reset() => Box.isTrigger = true;
    }
}
