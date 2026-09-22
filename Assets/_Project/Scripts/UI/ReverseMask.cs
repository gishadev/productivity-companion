using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    // Stencil source for ReverseMaskable: children are hidden where this graphic is opaque.
    [AddComponentMenu("UI/Reverse Mask", 14)]
    [DisallowMultipleComponent]
    public class ReverseMask : Mask
    {
        [Tooltip("Also swallow pointer events inside this rect, so the punched-out area is not clickable.")]
        [SerializeField] private bool blockRaycastsInsideMask;

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            showMaskGraphic = false;
        }
#endif

        public override bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled || !blockRaycastsInsideMask) return true;
            return !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera);
        }
    }
}
