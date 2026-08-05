using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    /// <summary>
    /// Add next to a Button to drive any number of Graphics with independent
    /// per-state ColorBlocks. Set the Button's Transition to None.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MultiGraphicButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField] private MultiGraphicTransition transition;

        private Button _button;
        private bool _isHovered;
        private bool _isSelected;
        private bool _wasInteractable;

        private void Awake()
        {
            _button = GetComponent<Button>();
            transition.Initialize(this);
        }

        private void OnValidate() => transition.Validate();

        private void OnEnable()
        {
            _wasInteractable = IsInteractable();
            transition.Transition(ResolveState());
        }

        private void OnDisable() => transition.Transition(UIState.Disabled, immediate: true);

        // ── Pointer events ────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData e)
        {
            _isHovered = true;
            if (!IsInteractable()) return;
            transition.Transition(UIState.Highlighted);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _isHovered = false;
            if (!IsInteractable()) return;
            transition.Transition(_isSelected ? UIState.Selected : UIState.Normal);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!IsInteractable()) return;
            transition.Transition(UIState.Pressed, immediate: true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!IsInteractable()) return;
            transition.Transition(_isHovered ? UIState.Highlighted : UIState.Normal);
        }

        public void OnSelect(BaseEventData e)
        {
            _isSelected = true;
            if (!IsInteractable()) return;
            transition.Transition(UIState.Selected);
        }

        public void OnDeselect(BaseEventData e)
        {
            _isSelected = false;
            if (!IsInteractable()) return;
            transition.Transition(_isHovered ? UIState.Highlighted : UIState.Normal);
        }

        // ── Interactable watch ────────────────────────────────────────────────────

        private void Update()
        {
            bool interactable = IsInteractable();
            if (interactable == _wasInteractable) return;
            _wasInteractable = interactable;
            transition.Transition(interactable ? ResolveState() : UIState.Disabled);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private UIState ResolveState()
        {
            if (!IsInteractable()) return UIState.Disabled;
            if (_isSelected) return UIState.Selected;
            if (_isHovered) return UIState.Highlighted;
            return UIState.Normal;
        }

        private bool IsInteractable() => _button != null && _button.interactable;
    }
}