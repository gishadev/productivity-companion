using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    public class DynamicSimulationWindowView : MonoBehaviour
    {
        [SerializeField] private GameObject simulationTargetObject;
        [SerializeField] private Button hideButton;
        [SerializeField] private Sprite hideSprite;
        [SerializeField] private Sprite showSprite;

        [Header("Docking")]
        [Tooltip("Where the simulation window sits while the widget is in the lower half of the screen.")]
        [SerializeField]
        private Vector2 topPosition;

        [Tooltip("Where the simulation window sits while the widget is in the upper half of the screen.")]
        [SerializeField]
        private Vector2 bottomPosition;

        [Tooltip("How far past the middle the widget must travel before the window flips, as a fraction of screen height.")]
        [SerializeField] [Range(0f, 0.25f)]
        private float switchHysteresis = 0.03f;

        private RectTransform _simulationRect;
        private RectTransform _dockParent;
        private RectTransform _screenRect;

        private bool _isHidden;
        private bool _isDockedToBottom;
        private bool _canDock;

        private void Awake()
        {
            _simulationRect = simulationTargetObject != null
                ? simulationTargetObject.transform as RectTransform
                : null;
            _dockParent = _simulationRect != null ? _simulationRect.parent as RectTransform : null;

            var canvas = GetComponentInParent<Canvas>();
            _screenRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;

            _canDock = _simulationRect != null && _dockParent != null && _screenRect != null;

            if (!_canDock)
                Debug.LogError($"[Simulation] {name}: no simulation window inside a canvas; docking disabled.", this);
        }

        private void OnEnable()
        {
            hideButton.onClick.AddListener(OnHideButtonClicked);
        }

        private void Start()
        {
            if (_canDock) ApplyDock(ShouldDockToBottom());
        }

        private void OnDisable()
        {
            hideButton.onClick.RemoveListener(OnHideButtonClicked);
        }

        // Polled rather than driven by the drag: three separate WidgetDragHandles move the widget, and
        // a resolution change moves the screen under a widget that never moved at all.
        private void Update()
        {
            if (!_canDock) return;

            var dockToBottom = ShouldDockToBottom();
            if (dockToBottom == _isDockedToBottom) return;

            ApplyDock(dockToBottom);
        }

        /// <summary>True once the widget sits in the upper half, where the window has to hang below it.</summary>
        private bool ShouldDockToBottom()
        {
            var height = Mathf.InverseLerp(_screenRect.rect.yMin, _screenRect.rect.yMax,
                _screenRect.InverseTransformPoint(DockAnchor).y);

            // The band is only ever crossed away from the current side, so a widget parked on the
            // middle line settles instead of flipping every frame.
            var threshold = _isDockedToBottom ? 0.5f - switchHysteresis : 0.5f + switchHysteresis;
            return height >= threshold;
        }

        /// <summary>
        /// Where the window hangs from: its own position with the offset we applied taken back out.
        /// Reading the window directly would feed the flip back into its own trigger, and reading the
        /// parent instead is no good either — it is a full-screen rect whose centre only happens to
        /// track the widget. Point anchors only, which is what anchoredPosition being a plain
        /// translation depends on.
        /// </summary>
        private Vector3 DockAnchor =>
            _simulationRect.position - _dockParent.TransformVector(_simulationRect.anchoredPosition);

        private void ApplyDock(bool dockToBottom)
        {
            _isDockedToBottom = dockToBottom;
            _simulationRect.anchoredPosition = dockToBottom ? bottomPosition : topPosition;
        }

        private void OnHideButtonClicked()
        {
            _isHidden = !_isHidden;
            hideButton.image.sprite = _isHidden ? showSprite : hideSprite;
            simulationTargetObject.SetActive(!_isHidden);
        }
    }
}
