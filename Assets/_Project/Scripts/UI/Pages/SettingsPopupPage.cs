using gishadev.companion.Pomodoro;
using gishadev.tools.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace gishadev.companion.UI.Pages
{
    /// <summary>
    /// Edits <see cref="PomodoroSettings"/> and <see cref="SimulationSettings"/> in place: every field
    /// writes through as soon as it is committed, so there is nothing to apply or cancel on close.
    /// </summary>
    public class SettingsPopupPage : PopupPage
    {
        [SerializeField] private TMP_InputField pomodoroInput;
        [SerializeField] private TMP_InputField shortBreakInput;
        [SerializeField] private TMP_InputField longBreakInput;

        [SerializeField] private Toggle autoPomodoroToggle;
        [SerializeField] private Toggle autoBreakToggle;

        [SerializeField] private Toggle autoFlipToggle;
        [SerializeField] private Toggle flipToggle;

        [Inject] private PomodoroSettings _settings;
        [Inject] private SimulationSettings _simulationSettings;

        private bool _isBound;

        private void Awake()
        {
            _isBound = pomodoroInput != null && shortBreakInput != null && longBreakInput != null &&
                       autoPomodoroToggle != null && autoBreakToggle != null &&
                       autoFlipToggle != null && flipToggle != null;

            if (!_isBound)
            {
                Debug.LogError($"[Settings] {name}: not every field is assigned; settings cannot be edited.", this);
                return;
            }

            pomodoroInput.onEndEdit.AddListener(OnPomodoroEdited);
            shortBreakInput.onEndEdit.AddListener(OnShortBreakEdited);
            longBreakInput.onEndEdit.AddListener(OnLongBreakEdited);

            autoPomodoroToggle.onValueChanged.AddListener(OnAutoPomodoroToggled);
            autoBreakToggle.onValueChanged.AddListener(OnAutoBreakToggled);

            autoFlipToggle.onValueChanged.AddListener(OnAutoFlipToggled);
            flipToggle.onValueChanged.AddListener(OnFlipToggled);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (!_isBound) return;

            pomodoroInput.onEndEdit.RemoveListener(OnPomodoroEdited);
            shortBreakInput.onEndEdit.RemoveListener(OnShortBreakEdited);
            longBreakInput.onEndEdit.RemoveListener(OnLongBreakEdited);

            autoPomodoroToggle.onValueChanged.RemoveListener(OnAutoPomodoroToggled);
            autoBreakToggle.onValueChanged.RemoveListener(OnAutoBreakToggled);

            autoFlipToggle.onValueChanged.RemoveListener(OnAutoFlipToggled);
            flipToggle.onValueChanged.RemoveListener(OnFlipToggled);
        }

        public override void Enter()
        {
            base.Enter();
            ShowSettings();
        }

        private void ShowSettings()
        {
            if (!_isBound) return;

            pomodoroInput.SetTextWithoutNotify(_settings.WorkMinutes.ToString());
            shortBreakInput.SetTextWithoutNotify(_settings.ShortBreakMinutes.ToString());
            longBreakInput.SetTextWithoutNotify(_settings.LongBreakMinutes.ToString());

            autoPomodoroToggle.SetIsOnWithoutNotify(_settings.AutoStartPomodoros);
            autoBreakToggle.SetIsOnWithoutNotify(_settings.AutoStartBreaks);

            autoFlipToggle.SetIsOnWithoutNotify(_simulationSettings.AutoFlip);
            flipToggle.SetIsOnWithoutNotify(_simulationSettings.Flip);
            ShowFlipOverride();
        }

        // Auto-flip owns the side while it is on, so the manual toggle greys out rather than silently
        // doing nothing. Its stored value is left alone, and comes back when auto-flip goes off.
        private void ShowFlipOverride() => flipToggle.interactable = !_simulationSettings.AutoFlip;

        // The setters clamp, so the field is rewritten from the model rather than from what was typed:
        // an empty box or a value out of range snaps back to what was actually stored.
        private void OnPomodoroEdited(string value)
        {
            _settings.WorkMinutes = ParseMinutes(value, _settings.WorkMinutes);
            pomodoroInput.SetTextWithoutNotify(_settings.WorkMinutes.ToString());
        }

        private void OnShortBreakEdited(string value)
        {
            _settings.ShortBreakMinutes = ParseMinutes(value, _settings.ShortBreakMinutes);
            shortBreakInput.SetTextWithoutNotify(_settings.ShortBreakMinutes.ToString());
        }

        private void OnLongBreakEdited(string value)
        {
            _settings.LongBreakMinutes = ParseMinutes(value, _settings.LongBreakMinutes);
            longBreakInput.SetTextWithoutNotify(_settings.LongBreakMinutes.ToString());
        }

        private void OnAutoPomodoroToggled(bool isOn) => _settings.AutoStartPomodoros = isOn;

        private void OnAutoBreakToggled(bool isOn) => _settings.AutoStartBreaks = isOn;

        private void OnAutoFlipToggled(bool isOn)
        {
            _simulationSettings.AutoFlip = isOn;
            ShowFlipOverride();
        }

        private void OnFlipToggled(bool isOn) => _simulationSettings.Flip = isOn;

        private static int ParseMinutes(string value, int fallback) =>
            int.TryParse(value, out var minutes) ? minutes : fallback;
    }
}
