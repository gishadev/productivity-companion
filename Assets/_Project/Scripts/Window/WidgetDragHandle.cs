using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Title-bar style grab area: drag it to reposition the widget, and the position is remembered
    /// between runs.
    /// </summary>
    /// <remarks>
    /// This moves the widget <em>inside</em> the fullscreen transparent window rather than moving the
    /// OS window. Because that window already covers the whole display, the two look identical to the
    /// user — but this needs no native calls, works in the editor, and never fights the topmost
    /// watchdog. The tradeoff is that the widget cannot be dragged onto a second monitor.
    ///
    /// Attach to a UI object with a Graphic that has Raycast Target enabled (a fully transparent
    /// Image is fine — alpha does not affect raycasting).
    /// </remarks>
    public sealed class WidgetDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string KeyPrefix = "window.widgetPos.";

        [Tooltip("The widget root to move. Defaults to this object's parent.")]
        [SerializeField] private RectTransform target;

        [Tooltip("Keeps the widget from being dragged off the edge of the screen.")]
        [SerializeField] private bool clampToScreen = true;

        [Tooltip("Remembers the widget position between runs.")]
        [SerializeField] private bool persistPosition = true;

        [Tooltip("Distinguishes saved positions when there is more than one draggable widget.")]
        [SerializeField] private string positionId = "widget";

        private Canvas _canvas;
        private RectTransform _parentRect;

        private ClickThroughController _clickThrough;
        private RenderThrottle _renderThrottle;

        private IDisposable _clickThroughBlock;
        private IDisposable _renderLease;

        /// <summary>
        /// Called by <see cref="WindowSceneBinder"/> once the window scope is up. Dragging still works
        /// without it — the holds below are simply not taken — so a missing binder degrades rather
        /// than throws.
        /// </summary>
        public void Initialize(ClickThroughController clickThrough, RenderThrottle renderThrottle)
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
            if (persistPosition) RestorePosition();
            if (clampToScreen) ClampIntoParent();
        }

        private void OnDisable() => ReleaseHolds();

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Without this the cursor crossing empty space mid-drag would flip the window to
            // click-through, Unity would stop receiving mouse messages, and the drag would die.
            _clickThroughBlock ??= _clickThrough?.AcquireBlock("widget-drag");

            // Dragging is an animation; don't let the idle throttle stutter it.
            _renderLease ??= _renderThrottle?.AcquireLease("widget-drag");
        }

        public void OnDrag(PointerEventData eventData)
        {
            // eventData.delta is in screen pixels; anchored position is in canvas units.
            var scale = _canvas != null ? _canvas.scaleFactor : 1f;
            if (scale <= 0f) scale = 1f;

            target.anchoredPosition += eventData.delta / scale;

            if (clampToScreen) ClampIntoParent();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ReleaseHolds();
            if (persistPosition) SavePosition();
        }

        private void ReleaseHolds()
        {
            _clickThroughBlock?.Dispose();
            _clickThroughBlock = null;
            _renderLease?.Dispose();
            _renderLease = null;
        }

        /// <summary>Keeps the widget's rect fully inside its parent.</summary>
        /// <remarks>
        /// Only meaningful for point anchors. With stretched anchors (anchorMin != anchorMax)
        /// anchoredPosition denotes an offset from the stretched edges rather than a position, so
        /// clamping it against the parent rect would be wrong; those are left alone.
        /// </remarks>
        private void ClampIntoParent()
        {
            if (_parentRect == null) return;
            if (target.anchorMin != target.anchorMax) return;

            var parentRect = _parentRect.rect;
            var size = target.rect.size;
            var pivot = target.pivot;

            // Where the widget's anchor sits in the parent's local space.
            var anchor = new Vector2(
                parentRect.xMin + parentRect.width * target.anchorMin.x,
                parentRect.yMin + parentRect.height * target.anchorMin.y);

            // anchoredPosition places the pivot relative to that anchor, so convert the parent's
            // edges into the equivalent bounds on anchoredPosition.
            var position = target.anchoredPosition;
            position.x = Mathf.Clamp(position.x,
                parentRect.xMin + size.x * pivot.x - anchor.x,
                parentRect.xMax - size.x * (1f - pivot.x) - anchor.x);
            position.y = Mathf.Clamp(position.y,
                parentRect.yMin + size.y * pivot.y - anchor.y,
                parentRect.yMax - size.y * (1f - pivot.y) - anchor.y);

            target.anchoredPosition = position;
        }

        private string PositionKey => KeyPrefix + positionId;

        private void SavePosition()
        {
            var position = target.anchoredPosition;
            PlayerPrefs.SetFloat(PositionKey + ".x", position.x);
            PlayerPrefs.SetFloat(PositionKey + ".y", position.y);
            PlayerPrefs.Save();
        }

        private void RestorePosition()
        {
            if (!PlayerPrefs.HasKey(PositionKey + ".x")) return;

            target.anchoredPosition = new Vector2(
                PlayerPrefs.GetFloat(PositionKey + ".x"),
                PlayerPrefs.GetFloat(PositionKey + ".y"));
        }
    }
}
