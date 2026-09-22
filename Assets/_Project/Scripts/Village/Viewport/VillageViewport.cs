using UnityEngine;

namespace gishadev.companion.Village
{
    public sealed class VillageViewport
    {
        private readonly VillageSceneRig _rig;
        private readonly VillageSurface _surface;

        public VillageViewport(VillageSceneRig rig, VillageSurface surface)
        {
            _rig = rig;
            _surface = surface;
        }

        public bool IsUsable => _rig != null && _rig.IsValid && _surface != null && _surface.isActiveAndEnabled;

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
