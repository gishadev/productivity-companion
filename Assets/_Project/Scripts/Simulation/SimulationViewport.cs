using UnityEngine;

namespace gishadev.companion.Simulation
{
    /// <summary>
    /// Maps between screen space and the simulation's world space. Deliberately holds no gameplay: every
    /// pointer-driven feature — picking, drag and place, labels pinned over entities — needs this one
    /// mapping and nothing else, which is what keeps the render-texture indirection from leaking.
    /// </summary>
    public sealed class SimulationViewport
    {
        private readonly SimulationSceneRig _rig;
        private readonly SimulationSurface _surface;

        public SimulationViewport(SimulationSceneRig rig, SimulationSurface surface)
        {
            _rig = rig;
            _surface = surface;
        }

        public bool IsUsable => _rig != null && _rig.IsValid && _surface != null && _surface.isActiveAndEnabled;

        /// <summary>False when the simulation is hidden, or the point is outside the surface.</summary>
        public bool TryScreenToWorld(Vector2 screenPosition, out Vector2 world)
        {
            world = default;
            if (!IsUsable) return false;

            var rect = _surface.Rect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, screenPosition, _surface.EventCamera, out var local))
                return false;

            var viewport = Rect.PointToNormalized(rect.rect, local);
            if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;

            world = _rig.Camera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, 0f));
            return true;
        }

        /// <summary>Where a point in the simulation lands on screen, for pinning UI over an entity.</summary>
        public bool TryWorldToScreen(Vector2 world, out Vector2 screenPosition)
        {
            screenPosition = default;
            if (!IsUsable) return false;

            var viewport = _rig.Camera.WorldToViewportPoint(world);
            var rect = _surface.Rect;
            var local = Rect.NormalizedToPoint(rect.rect, viewport);

            screenPosition = RectTransformUtility.WorldToScreenPoint(_surface.EventCamera, rect.TransformPoint(local));
            return true;
        }
    }
}
