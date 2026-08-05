using System;
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
    /// call. Clicks are wired in code, not UnityEvent lists, so a scene edit cannot silently unhook them.
    /// </summary>
    public sealed class PomodoroWidgetView : MonoBehaviour
    {
        [Tooltip("Countdown label. Written as MM:SS.")]
        [SerializeField] private TextMeshProUGUI timeLabel;

        [Tooltip("Starts the current phase, or pauses it while it runs.")]
        [SerializeField] private Button playButton;

        [Tooltip("Opens Pomodoro settings. Raises an event only; no panel exists yet.")]
        [SerializeField] private Button settingsButton;

        [Tooltip("Play button icon while the timer is stopped or paused. Optional.")]
        [SerializeField] private Sprite playIcon;

        [Tooltip("Play button icon while the timer runs. Optional.")]
        [SerializeField] private Sprite pauseIcon;

        [Inject] private IEventBus _eventBus;
        
        private Image _playButtonIcon;

        private void Awake()
        {
            if (timeLabel == null)
                Debug.LogError($"[Pomodoro] {name}: no time label assigned; the countdown will not show.", this);

            if (playButton == null)
                Debug.LogError($"[Pomodoro] {name}: no play button assigned; the timer cannot be started.", this);
            else
                _playButtonIcon = playButton.image;
        }

        private void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayButtonClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        }

        /// <summary>Sets the countdown text verbatim; the caller owns the formatting.</summary>
        public void SetTime(string text)
        {
            if (timeLabel == null) return;
            timeLabel.text = text;
        }

        /// <summary>Switches the play button between its "start" and "pause" affordance.</summary>
        public void SetRunning(bool running)
        {
            // Both icons are optional: an unstyled button still works, it just doesn't change face.
            if (_playButtonIcon == null || playIcon == null || pauseIcon == null) return;
            _playButtonIcon.sprite = running ? pauseIcon : playIcon;
        }

        private void OnPlayButtonClicked() => _eventBus.Fire(new PlayPauseClickedEvent());
        private void OnSettingsButtonClicked() => _eventBus.Fire(new SettingsClickedEvent());
    }
}
