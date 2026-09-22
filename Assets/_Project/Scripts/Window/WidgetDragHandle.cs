using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace gishadev.companion.Window
{
    // Moves the widget inside the fullscreen window, not the OS window, so it can't cross monitors.
    public sealed class WidgetDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("The widget root to move. Defaults to this object's parent.")] [SerializeField]
        private RectTransform target;

        [Tooltip("Keeps the widget from being dragged off the edge of the screen.")] [SerializeField]
        private bool clampToScreen = true;

        private Canvas _canvas;
        private RectTransform _parentRect;

        private ClickThroughController _clickThrough;
        private RenderThrottle _renderThrottle;

        private IDisposable _clickThroughBlock;
        private IDisposable _renderLease;

        [Inject]
        public void Construct(ClickThroughController clickThrough, RenderThrottle renderThrottle)
        {
            _clickThrough = clickThrough;
            _renderThrottle = renderThrottle;
        }

        private void Awake()
        {
            if (target == null)
                target = transform.parent as RectTransform;

            if (target == null)
            {
                Debug.LogError($"[Window] {name}: no target RectTransform; drag disabled.", this);
                enabled = false;
                return;
            }

            _canvas = target.GetComponentInParent<Canvas>();
            _parentRect = target.parent as RectTransform;
        }

        private void Start()
        {
            if (clampToScreen) ClampIntoParent();
        }

        private void OnDisable() => ReleaseHolds();

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Otherwise crossing empty space mid-drag enables click-through and kills the drag.
            _clickThroughBlock ??= _clickThrough?.AcquireBlock("widget-drag");
            _renderLease ??= _renderThrottle?.AcquireLease("widget-drag");
        }

        public void OnDrag(PointerEventData eventData)
        {
            var scale = _canvas != null ? _canvas.scaleFactor : 1f;
            if (scale <= 0f) scale = 1f;

            target.anchoredPosition += eventData.delta / scale;

            if (clampToScreen) ClampIntoParent();
        }

        public void OnEndDrag(PointerEventData eventData) => ReleaseHolds();

        private void ReleaseHolds()
        {
            _clickThroughBlock?.Dispose();
            _clickThroughBlock = null;
            _renderLease?.Dispose();
            _renderLease = null;
        }

        // Point anchors only.
        private void ClampIntoParent()
        {
            if (_parentRect == null) return;
            if (target.anchorMin != target.anchorMax) return;

            var parentRect = _parentRect.rect;
            var size = target.rect.size;
            var pivot = target.pivot;

            var anchor = new Vector2(
                parentRect.xMin + parentRect.width * target.anchorMin.x,
                parentRect.yMin + parentRect.height * target.anchorMin.y);

            var position = target.anchoredPosition;
            position.x = Mathf.Clamp(position.x,
                parentRect.xMin + size.x * pivot.x - anchor.x,
                parentRect.xMax - size.x * (1f - pivot.x) - anchor.x);
            position.y = Mathf.Clamp(position.y,
                parentRect.yMin + size.y * pivot.y - anchor.y,
                parentRect.yMax - size.y * (1f - pivot.y) - anchor.y);

            target.anchoredPosition = position;
        }
    }
}