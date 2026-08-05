using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    [RequireComponent(typeof(Button))]
    public class TabButton : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private MultiGraphicTransition transition;

        public ITabGroup TabGroup { get; private set; }

        private bool _transitionInitialized;

        private void Awake() => EnsureTransitionInitialized();

        private void OnValidate() => transition.Validate();

        public void Initialize(ITabGroup group)
        {
            EnsureTransitionInitialized();
            TabGroup = group;
        }

        // Guards against transitions firing before Awake runs (e.g. when this tab is
        // instantiated and selected within the same frame, under an inactive parent).
        private void EnsureTransitionInitialized()
        {
            if (_transitionInitialized) return;
            transition.Initialize(this);
            _transitionInitialized = true;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (TabGroup == null) return;
            transition.Transition(UIState.Pressed, immediate: true);
            TabGroup.OnTabSelected(this);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (TabGroup == null || TabGroup.IsActive(this)) return;
            transition.Transition(UIState.Highlighted);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (TabGroup == null || TabGroup.IsActive(this)) return;
            transition.Transition(UIState.Normal);
        }

        public void SetActive(bool isActive)
            => transition.Transition(isActive ? UIState.Selected : UIState.Normal);

        public void SetInteractable(bool interactable)
            => transition.Transition(interactable ? UIState.Normal : UIState.Disabled);
    }
}