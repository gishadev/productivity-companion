using System;
using gishadev.companion.Focus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    public class FocusGUIElement : MonoBehaviour
    {
        [SerializeField] private TMP_Text appNameTMP;
        [SerializeField] private Button removeButton;
        [SerializeField] private Image tintImage;
        [SerializeField] private Color productiveColor = new(0.35f, 0.75f, 0.4f);
        [SerializeField] private Color unproductiveColor = new(0.85f, 0.35f, 0.35f);

        public event Action<FocusGUIElement> RemoveClicked;

        public string AppName { get; private set; }

        private void Awake() => removeButton.onClick.AddListener(OnRemoveClicked);

        private void OnDestroy() => removeButton.onClick.RemoveListener(OnRemoveClicked);

        public void Setup(string appName, FocusCategory category)
        {
            AppName = appName;
            appNameTMP.text = appName;
            if (tintImage != null)
                tintImage.color = category == FocusCategory.Productive ? productiveColor : unproductiveColor;
        }

        private void OnRemoveClicked() => RemoveClicked?.Invoke(this);
    }
}
