using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.Village
{
    /// <summary>
    /// The UI-side face of the simulation: one RawImage the world is rendered into. Everything that
    /// already moves the widget around — docking, dragging, hiding, the click-through hit test — sees a
    /// plain Graphic and needs no knowledge of what is behind it.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class VillageSurface : MonoBehaviour, ICanvasRaycastFilter
    {
        private RawImage _image;
        private RectTransform _rect;
        private Canvas _canvas;

        public RawImage Image
        {
            get
            {
                if (_image == null) _image = GetComponent<RawImage>();
                return _image;
            }
        }

        public RectTransform Rect
        {
            get
            {
                if (_rect == null) _rect = (RectTransform)transform;
                return _rect;
            }
        }

        /// <summary>Null under a ScreenSpaceOverlay canvas, which is what the RectTransform helpers want.</summary>
        public Camera EventCamera
        {
            get
            {
                if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
                if (_canvas == null) return null;

                var root = _canvas.rootCanvas;
                return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            }
        }

        // Until the render target is allocated the surface is a blank rect, and registering that as a
        // hit would block click-through over a window that is showing nothing.
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => Image.texture != null;
    }
}
