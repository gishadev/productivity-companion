using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.Pomodoro
{
    /// <summary>
    /// The scene-authored face of the Pomodoro widget: it renders what it is told to render and
    /// reports clicks. It holds no timer state and makes no decisions — <see cref="PomodoroPresenter"/>
    /// owns all of that.
    /// </summary>
    /// <remarks>
    /// Deliberately free of [Inject] members. The project's root scope
    /// (<c>gishadev.tools.AutoInjectLifetimeScope</c>) sweeps the scene on every load and injects every
    /// MonoBehaviour carrying an [Inject] attribute <em>from the root container</em>, logging an error
    /// for each one it cannot satisfy. Since the Pomodoro services live in a child scope, an attribute
    /// here would log on every scene load. Instead this component is pulled into the container with
    /// <c>RegisterComponentInHierarchy</c> and handed to the presenter as a constructor dependency.
    ///
    /// Clicks are wired in code rather than through the buttons' UnityEvent lists so the whole flow is
    /// greppable from source and cannot be silently unhooked by a scene edit.
    /// </remarks>
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

        private Image _playButtonIcon;

        /// <summary>Raised on every press of the play button, whatever the timer is currently doing.</summary>
        public event Action PlayPauseClicked;

        /// <summary>Raised on every press of the settings button.</summary>
        public event Action SettingsClicked;

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

        private void OnPlayButtonClicked() => PlayPauseClicked?.Invoke();

        private void OnSettingsButtonClicked() => SettingsClicked?.Invoke();
    }
}
