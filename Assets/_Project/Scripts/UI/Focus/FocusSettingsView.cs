using System;
using System.Collections.Generic;
using gishadev.companion.Focus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace gishadev.companion.UI
{
    /// <summary>
    /// Classifies the last foreign foreground app and lists every classified one. The list is a view of
    /// <see cref="FocusRules"/>, which owns persistence, so it is rebuilt from the rules rather than saved.
    /// </summary>
    public class FocusSettingsView : MonoBehaviour
    {
        private const string NoAppLabel = "—";

        [SerializeField] private TMP_Text appToFocusTMP;
        [SerializeField] private Button productiveButton;
        [SerializeField] private Button unproductiveButton;
        [SerializeField] private Transform focusListParent;
        [SerializeField] private FocusGUIElement focusGUIElementPrefab;

        private readonly List<FocusGUIElement> _focusGUIList = new();

        private FocusController _focus;
        private FocusRules _rules;
        private string _shownAppName;

        // Injection can land after Awake/Start (the scope's build is deferred), so wiring happens here.
        [Inject]
        public void Construct(FocusController focus, FocusRules rules)
        {
            _focus = focus;
            _rules = rules;

            _rules.Changed += Sync;
            productiveButton.onClick.AddListener(OnProductiveClicked);
            unproductiveButton.onClick.AddListener(OnUnproductiveClicked);

            ShowCurrentApp();
            Sync();
        }

        private void OnDestroy()
        {
            if (_rules == null) return;

            _rules.Changed -= Sync;
            productiveButton.onClick.RemoveListener(OnProductiveClicked);
            unproductiveButton.onClick.RemoveListener(OnUnproductiveClicked);
        }

        // CurrentProcessName has no change event; while this view is focused it holds the app the user came from.
        private void Update()
        {
            if (_focus == null || _focus.CurrentProcessName == _shownAppName) return;
            ShowCurrentApp();
        }

        private void ShowCurrentApp()
        {
            _shownAppName = _focus.CurrentProcessName;

            var hasApp = !string.IsNullOrEmpty(_shownAppName);
            appToFocusTMP.text = hasApp ? _shownAppName : NoAppLabel;
            productiveButton.interactable = hasApp;
            unproductiveButton.interactable = hasApp;
        }

        private void OnProductiveClicked() => _focus.TagCurrent(FocusCategory.Productive);

        private void OnUnproductiveClicked() => _focus.TagCurrent(FocusCategory.Unproductive);

        private void OnElementRemoveClicked(FocusGUIElement element) => _rules.Remove(element.AppName);

        private void Sync()
        {
            var desired = new Dictionary<string, FocusCategory>(StringComparer.OrdinalIgnoreCase);
            foreach (var app in _rules.Productive) desired[app] = FocusCategory.Productive;
            foreach (var app in _rules.Unproductive) desired[app] = FocusCategory.Unproductive;

            for (var i = _focusGUIList.Count - 1; i >= 0; i--)
            {
                var element = _focusGUIList[i];
                if (desired.ContainsKey(element.AppName)) continue;

                element.RemoveClicked -= OnElementRemoveClicked;
                Destroy(element.gameObject);
                _focusGUIList.RemoveAt(i);
            }

            foreach (var pair in desired)
            {
                var app = pair.Key;
                var element = _focusGUIList.Find(e => string.Equals(e.AppName, app, StringComparison.OrdinalIgnoreCase));
                if (element == null)
                {
                    element = Instantiate(focusGUIElementPrefab, focusListParent);
                    element.RemoveClicked += OnElementRemoveClicked;
                    _focusGUIList.Add(element);
                }

                element.Setup(app, pair.Value);
            }
        }
    }
}
