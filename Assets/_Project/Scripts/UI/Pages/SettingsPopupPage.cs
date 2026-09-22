using gishadev.companion.Pomodoro;
using gishadev.tools.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace gishadev.companion.UI.Pages
{
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
        [Inject] private VillageWindowSettings _villageWindowSettings;

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

            autoFlipToggle.SetIsOnWithoutNotify(_villageWindowSettings.AutoFlip);
            flipToggle.SetIsOnWithoutNotify(_villageWindowSettings.Flip);
            ShowFlipOverride();
        }

        // Manual flip is overridden while auto-flip is on; its stored value is kept.
        private void ShowFlipOverride() => flipToggle.interactable = !_villageWindowSettings.AutoFlip;

        // Rewritten from the model: setters clamp, and invalid input snaps back.
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
            _villageWindowSettings.AutoFlip = isOn;
            ShowFlipOverride();
        }

        private void OnFlipToggled(bool isOn) => _villageWindowSettings.Flip = isOn;

        private static int ParseMinutes(string value, int fallback) =>
            int.TryParse(value, out var minutes) ? minutes : fallback;
    }
}
