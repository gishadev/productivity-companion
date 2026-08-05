using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Title-bar style grab area; position is remembered between runs. Moves the widget inside the
    /// fullscreen transparent window rather than moving the OS window — identical to the user, but
    /// needs no native calls and never fights the topmost watchdog. The widget therefore cannot be
    /// dragged onto a second monitor. Attach to a UI object whose Graphic has Raycast Target enabled.
    /// </summary>
    public sealed class WidgetDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string KeyPrefix = "window.widgetPos.";

        [Tooltip("The widget root to move. Defaults to this object's parent.")] [SerializeField]
        private RectTransform target;

        [Tooltip("Keeps the widget from being dragged off the edge of the screen.")] [SerializeField]
        private bool clampToScreen = true;

        [Tooltip("Remembers the widget position between runs.")] [SerializeField]
        private bool persistPosition = true;

        [Tooltip("Distinguishes saved positions when there is more than one draggable widget.")] [SerializeField]
        private string positionId = "widget";

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
            if (persistPosition) RestorePosition();
            if (clampToScreen) ClampIntoParent();
        }

        private void OnDisable() => ReleaseHolds();

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Without the block, the cursor crossing empty space mid-drag flips the window to
            // click-through, Unity stops receiving mouse messages, and the drag dies.
            _clickThroughBlock ??= _clickThrough?.AcquireBlock("widget-drag");
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

        /// <summary>
        /// Keeps the widget's rect inside its parent. Point anchors only: with stretched anchors
        /// anchoredPosition is an offset from the stretched edges, not a position, so clamping it
        /// against the parent rect would be wrong.
        /// </summary>
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