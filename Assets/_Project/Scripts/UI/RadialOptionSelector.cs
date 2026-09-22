using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    // Hit-tests by pointer angle: radial slices overlap as rects, so uGUI raycasts can't separate them.
    [AddComponentMenu("UI/Radial Option Selector", 16)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class RadialOptionSelector : MonoBehaviour
    {
        [Serializable]
        public sealed class Option
        {
            [Tooltip("Optional. onClick is invoked on select; interactable stays off.")]
            public Button button;

            [Tooltip("Tinted per state. Empty falls back to the button's Target Graphic.")]
            public List<Graphic> graphics = new();

            public ColorBlock colors = ColorBlock.defaultColorBlock;

            [Tooltip("Relative arc size. Equal weights split the circle evenly.")]
            [Min(0.01f)] public float weight = 1f;

            public UnityEvent onSelected;
        }

        [Header("Options (radial order)")]
        [SerializeField] private List<Option> options = new();

        [Header("Detection")]
        [Tooltip("Clicks closer to the centre than this dismiss the menu.")]
        [Min(0f)] [SerializeField] private float minRadius = 60f;

        [Tooltip("Clicks further from the centre than this dismiss the menu.")]
        [Min(0f)] [SerializeField] private float maxRadius = 170f;

        [Tooltip("Where the first option's arc begins, in degrees clockwise from straight up.")]
        [SerializeField] private float startAngle;

        [SerializeField] private bool clockwise = true;

        [Tooltip("Rects owned by real uGUI buttons — the play button in the hole, say. Presses there " +
                 "are left alone instead of dismissing, so the two do not fight over the same click.")]
        [SerializeField] private List<RectTransform> ignoreRects = new();

        [Header("Behaviour")]
        [Tooltip("Object disabled when the menu is dismissed. Defaults to this object's parent.")]
        [SerializeField] private GameObject hideTarget;

        [SerializeField] private bool hideOnSelect;

        [Tooltip("Index selected the first time this is enabled. -1 for none.")]
        [SerializeField] private int defaultIndex = -1;

        [Header("Gizmos")]
        [SerializeField] private bool alwaysDrawGizmos;

        public UnityEvent<int> onOptionSelected;

        private RectTransform _rect;
        private Canvas _canvas;
        private int _hovered = -1;
        private int _pressed = -1;
        private int _selected = -1;
        private bool _initialised;

        public int SelectedIndex => _selected;
        public int OptionCount => options.Count;

        private void Awake()
        {
            _rect = (RectTransform)transform;

            foreach (var option in options)
            {
                if (option?.button == null) continue;
                // A non-interactable Selectable repaints to disabledColor on every pointer event, overwriting our tints.
                option.button.transition = Selectable.Transition.None;
            }
        }

        private void OnEnable()
        {
            if (!_initialised)
            {
                _selected = defaultIndex >= 0 && defaultIndex < options.Count ? defaultIndex : -1;
                _initialised = true;
            }

            _hovered = -1;
            _pressed = -1;
            RefreshAll(instant: true);
        }

        private void OnDisable()
        {
            _hovered = -1;
            _pressed = -1;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || options.Count == 0) return;

            var screenPosition = mouse.position.ReadValue();
            var ignored = IsOverIgnoredRect(screenPosition);
            var index = ignored ? -1 : IndexAt(screenPosition);
            SetHovered(index);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (ignored) return;

                if (index < 0)
                {
                    Hide();
                    return;
                }

                _pressed = index;
                Refresh(index, instant: true);
            }

            if (!mouse.leftButton.wasReleasedThisFrame) return;

            var pressed = _pressed;
            _pressed = -1;
            if (pressed >= 0 && pressed == index) Select(index);
            else Refresh(pressed, instant: false);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= options.Count) return;

            var previous = _selected;
            _selected = index;
            Refresh(previous, instant: false);
            Refresh(index, instant: false);

            var option = options[index];
            option.onSelected?.Invoke();
            option.button?.onClick.Invoke();
            onOptionSelected?.Invoke(index);

            if (hideOnSelect) Hide();
        }

        public void Hide()
        {
            var target = hideTarget != null ? hideTarget
                : transform.parent != null ? transform.parent.gameObject
                : gameObject;
            target.SetActive(false);
        }

        // -1 outside the ring (a click there dismisses).
        private int IndexAt(Vector2 screenPosition)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPosition, EventCamera,
                    out var local))
                return -1;

            var distance = local.magnitude;
            if (distance < minRadius || distance > maxRadius) return -1;

            var angle = Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg;
            var relative = Mathf.Repeat(clockwise ? angle - startAngle : startAngle - angle, 360f);

            var total = TotalWeight();
            var cursor = 0f;
            for (var i = 0; i < options.Count; i++)
            {
                cursor += 360f * options[i].weight / total;
                if (relative < cursor) return i;
            }

            return options.Count - 1;
        }

        private bool IsOverIgnoredRect(Vector2 screenPosition)
        {
            foreach (var rect in ignoreRects)
            {
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, EventCamera))
                    return true;
            }

            return false;
        }

        private Camera EventCamera
        {
            get
            {
                if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
                if (_canvas == null) return null;
                var root = _canvas.rootCanvas;
                return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            }
        }

        private float TotalWeight()
        {
            var total = 0f;
            foreach (var option in options) total += Mathf.Max(0.01f, option.weight);
            return total <= 0f ? 1f : total;
        }

        private void SetHovered(int index)
        {
            if (index == _hovered) return;

            var previous = _hovered;
            _hovered = index;
            Refresh(previous, instant: false);
            Refresh(index, instant: false);
        }

        private void RefreshAll(bool instant)
        {
            for (var i = 0; i < options.Count; i++) Refresh(i, instant);
        }

        private void Refresh(int index, bool instant)
        {
            if (index < 0 || index >= options.Count) return;

            var option = options[index];
            var color = MultiGraphicTransition.ResolveColor(option.colors, StateOf(index)) *
                        option.colors.colorMultiplier;
            var duration = instant ? 0f : option.colors.fadeDuration;

            if (option.graphics.Count == 0)
            {
                option.button?.targetGraphic?.CrossFadeColor(color, duration, true, true);
                return;
            }

            foreach (var graphic in option.graphics)
                if (graphic != null)
                    graphic.CrossFadeColor(color, duration, true, true);
        }

        private UIState StateOf(int index)
        {
            if (index == _pressed) return UIState.Pressed;
            if (index == _hovered) return UIState.Highlighted;
            if (index == _selected) return UIState.Selected;
            return UIState.Normal;
        }

        private void Reset()
        {
            if (transform.parent != null) hideTarget = transform.parent.gameObject;
            CollectOptionsFromChildren();
        }

        [ContextMenu("Collect Options From Children")]
        private void CollectOptionsFromChildren()
        {
            options.Clear();
            foreach (Transform child in transform)
            {
                var button = child.GetComponent<Button>();
                if (button != null) options.Add(new Option { button = button });
            }
        }

        private void OnValidate()
        {
            if (maxRadius < minRadius) maxRadius = minRadius;
            foreach (var option in options)
                if (option != null && option.colors.colorMultiplier <= 0f)
                    option.colors = ColorBlock.defaultColorBlock;
        }

        private void OnDrawGizmos()
        {
            if (alwaysDrawGizmos) DrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!alwaysDrawGizmos) DrawGizmos();
        }

        private void DrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            DrawCircle(minRadius);

            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
            DrawCircle(maxRadius);

            if (options.Count == 0) return;

            var total = TotalWeight();
            var cursor = 0f;
            for (var i = 0; i < options.Count; i++)
            {
                var sweep = 360f * Mathf.Max(0.01f, options[i].weight) / total;

                Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
                var edge = DirectionAt(cursor);
                Gizmos.DrawLine(edge * minRadius, edge * maxRadius);

                Gizmos.color = i == _hovered ? Color.yellow : new Color(0.4f, 0.7f, 1f, 0.8f);
                var mid = DirectionAt(cursor + sweep * 0.5f);
                Gizmos.DrawLine(mid * maxRadius, mid * (maxRadius + 14f));

                cursor += sweep;
            }

            Gizmos.color = new Color(1f, 0.3f, 0.9f, 0.7f);
            foreach (var rect in ignoreRects)
            {
                if (rect == null) continue;
                Gizmos.matrix = rect.localToWorldMatrix;
                Gizmos.DrawWireCube(rect.rect.center, new Vector3(rect.rect.width, rect.rect.height, 0f));
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        private Vector3 DirectionAt(float relativeAngle)
        {
            var degrees = clockwise ? startAngle + relativeAngle : startAngle - relativeAngle;
            var radians = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), Mathf.Cos(radians), 0f);
        }

        private static void DrawCircle(float radius, int segments = 72)
        {
            if (radius <= 0f) return;

            var previous = new Vector3(0f, radius, 0f);
            for (var i = 1; i <= segments; i++)
            {
                var t = i / (float)segments * Mathf.PI * 2f;
                var next = new Vector3(Mathf.Sin(t) * radius, Mathf.Cos(t) * radius, 0f);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
