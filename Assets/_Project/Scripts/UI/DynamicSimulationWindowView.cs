using UnityEngine;
using UnityEngine.UI;
using VContainer;

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

        private VillageWindowSettings _windowSettings;

        private RectTransform _simulationRect;
        private RectTransform _dockParent;
        private RectTransform _screenRect;

        private bool _isDockedToBottom;
        private bool _canDock;

        // Method injection: the scope builds after Awake.
        [Inject]
        public void Construct(VillageWindowSettings windowSettings)
        {
            _windowSettings = windowSettings;
            _windowSettings.Changed += OnWindowSettingsChanged;

            OnWindowSettingsChanged();
        }

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

        private void OnDisable()
        {
            hideButton.onClick.RemoveListener(OnHideButtonClicked);
        }

        private void OnDestroy()
        {
            if (_windowSettings != null)
                _windowSettings.Changed -= OnWindowSettingsChanged;
        }

        // Polled: several drag handles and resolution changes can move the widget.
        private void Update()
        {
            if (!_canDock || _windowSettings == null || !_windowSettings.AutoFlip) return;

            var dockToBottom = ShouldDockToBottom();
            if (dockToBottom == _isDockedToBottom) return;

            ApplyDock(dockToBottom);
        }

        private void OnWindowSettingsChanged()
        {
            ApplyHidden();
            if (_canDock) ApplyDock(_windowSettings.AutoFlip ? ShouldDockToBottom() : _windowSettings.Flip);
        }

        private void ApplyHidden()
        {
            hideButton.image.sprite = _windowSettings.IsHidden ? showSprite : hideSprite;
            simulationTargetObject.SetActive(!_windowSettings.IsHidden);
        }

        private bool ShouldDockToBottom()
        {
            var height = Mathf.InverseLerp(_screenRect.rect.yMin, _screenRect.rect.yMax,
                _screenRect.InverseTransformPoint(DockAnchor).y);

            // Hysteresis, so a widget parked on the midline doesn't flip every frame.
            var threshold = _isDockedToBottom ? 0.5f - switchHysteresis : 0.5f + switchHysteresis;
            return height >= threshold;
        }

        // Own position minus the applied offset, so the flip can't feed back into its own trigger.
        // Point anchors only.
        private Vector3 DockAnchor =>
            _simulationRect.position - _dockParent.TransformVector(_simulationRect.anchoredPosition);

        private void ApplyDock(bool dockToBottom)
        {
            _isDockedToBottom = dockToBottom;
            _simulationRect.anchoredPosition = dockToBottom ? bottomPosition : topPosition;
        }

        private void OnHideButtonClicked() => _windowSettings.IsHidden = !_windowSettings.IsHidden;
    }
}
