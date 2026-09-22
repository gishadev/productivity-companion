using UnityEngine;

namespace gishadev.companion.Village
{
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

        // From offset/size, not Collider2D.bounds, which is zero while the object is inactive.
        // Rotation is ignored.
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

        private void Reset() => Box.isTrigger = true;
    }
}
