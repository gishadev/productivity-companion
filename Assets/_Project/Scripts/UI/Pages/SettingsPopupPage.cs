using gishadev.companion.Pomodoro;
using gishadev.tools.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace gishadev.companion.UI.Pages
{
    /// <summary>
    /// Edits <see cref="PomodoroSettings"/> in place: every field writes through as soon as it is
    /// committed, so there is nothing to apply or cancel when the page closes.
    /// </summary>
    public class SettingsPopupPage : PopupPage
    {
        [SerializeField] private TMP_InputField pomodoroInput;
        [SerializeField] private TMP_InputField shortBreakInput;
        [SerializeField] private TMP_InputField longBreakInput;

        [SerializeField] private Toggle autoPomodoroToggle;
        [SerializeField] private Toggle autoBreakToggle;

        [Inject] private PomodoroSettings _settings;

        private bool _isBound;

        private void Awake()
        {
            _isBound = pomodoroInput != null && shortBreakInput != null && longBreakInput != null &&
                       autoPomodoroToggle != null && autoBreakToggle != null;

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
        }

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

        private static int ParseMinutes(string value, int fallback) =>
            int.TryParse(value, out var minutes) ? minutes : fallback;
    }
}
