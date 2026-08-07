using gishadev.companion.Events;
using gishadev.tools.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace gishadev.companion.Pomodoro
{
    /// <summary>
    /// Renders what it is told to and reports clicks; <see cref="PomodoroPresenter"/> owns all state.
    /// Field injection because Unity constructs it, so there is no constructor for the container to
    /// call. The play and reset buttons are wired in code; the radial options are not, because the
    /// selector drives non-interactable buttons — hook their OnClick to the OnRadial* methods here.
    /// </summary>
    public sealed class PomodoroWidgetView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeLabel;

        [Tooltip("Radial phase picker. Opened by the play button while the timer is stopped.")]
        [SerializeField] private GameObject radialRoot;
        [SerializeField] private Button playButton;
        [SerializeField] private Button resetButton;

        [SerializeField] private Sprite playIcon;
        [SerializeField] private Sprite pauseIcon;

        [Inject] private IEventBus _eventBus;

        private Image _playButtonIcon;
        private PomodoroPhase _phase;
        private bool _isRunning;
        private bool _isResumable;

        private void Awake()
        {
            if (timeLabel == null)
                Debug.LogError($"[Pomodoro] {name}: no time label assigned; the countdown will not show.", this);

            if (playButton == null)
                Debug.LogError($"[Pomodoro] {name}: no play button assigned; the timer cannot be started.", this);
            else
                _playButtonIcon = playButton.image;

            if (radialRoot == null)
                Debug.LogError($"[Pomodoro] {name}: no radial root assigned; no phase can be picked.", this);
            else
                radialRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClicked);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetButtonClicked);
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayButtonClicked);
            if (resetButton != null) resetButton.onClick.RemoveListener(OnResetButtonClicked);
        }

        /// <summary>Sets the countdown text verbatim; the caller owns the formatting.</summary>
        public void SetTime(string text)
        {
            if (timeLabel == null) return;
            timeLabel.text = text;
        }

        /// <summary>
        /// Pushes the timer state the play button branches on. <paramref name="resumable"/> means a
        /// phase is part-way through but stopped.
        /// </summary>
        public void SetState(PomodoroPhase phase, bool running, bool resumable)
        {
            _phase = phase;
            _isRunning = running;
            _isResumable = resumable;

            // A running timer has nothing left to pick, so the picker never survives into that state.
            if (running && radialRoot != null) radialRoot.SetActive(false);

            // Both icons are optional: an unstyled button still works, it just doesn't change face.
            if (_playButtonIcon == null || playIcon == null || pauseIcon == null) return;
            _playButtonIcon.sprite = running ? pauseIcon : playIcon;
        }

        // Public so each radial option's Button can reach them from its OnClick list: the options are
        // non-interactable and RadialOptionSelector invokes onClick on their behalf.
        public void OnRadialPomodoroClicked() => StartPhase(PomodoroPhase.Work);
        public void OnRadialShortBreakClicked() => StartPhase(PomodoroPhase.ShortBreak);
        public void OnRadialLongBreakClicked() => StartPhase(PomodoroPhase.LongBreak);

        // Pause what is running, resume what is part-way through, and only offer the picker once there
        // is nothing to resume — so changing phase mid-session means resetting first.
        private void OnPlayButtonClicked()
        {
            if (_isRunning)
            {
                _eventBus.Fire(new PauseClickedEvent());
                return;
            }

            if (_isResumable)
            {
                _eventBus.Fire(new PlayClickedEvent(_phase));
                return;
            }

            if (radialRoot != null) radialRoot.SetActive(!radialRoot.activeSelf);
        }

        private void OnResetButtonClicked() => _eventBus.Fire(new ResetClickedEvent());

        private void StartPhase(PomodoroPhase phase)
        {
            if (radialRoot != null) radialRoot.SetActive(false);
            _eventBus.Fire(new PlayClickedEvent(phase));
        }
    }
}
