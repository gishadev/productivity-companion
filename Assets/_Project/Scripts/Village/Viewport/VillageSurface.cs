using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.Village
{
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

        // A blank surface must not block click-through.
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => Image.texture != null;
    }
}
