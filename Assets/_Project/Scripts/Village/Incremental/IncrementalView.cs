using gishadev.companion.Focus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Renders what it is told; <see cref="IncrementalController"/> owns all state. The slider is one
    /// bar with two meanings — progress toward the next level, or the penalty still owed — separated by
    /// the fill colour.
    /// </summary>
    public sealed class IncrementalView : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Color normalColor = new Color(0.35f, 0.78f, 0.42f);
        [SerializeField] private Color penaltyColor = new Color(0.85f, 0.25f, 0.25f);

        [Header("Activity")]
        [SerializeField] private Image progressionImage;
        [SerializeField] private Sprite productiveIcon;
        [SerializeField] private Sprite unproductiveIcon;
        [SerializeField] private Sprite regularIcon;
        [SerializeField] private Sprite breakIcon;
        [SerializeField] private Sprite pauseIcon;

        [Header("Level")]
        [SerializeField] private TMP_Text levelLabel;

        private void Awake()
        {
            if (slider == null)
                Debug.LogError($"[Incremental] {name}: no slider assigned; progress will not be shown.", this);

            if (fillImage == null)
                Debug.LogError($"[Incremental] {name}: no fill image assigned; penalties will not be visible.", this);

            if (progressionImage == null)
                Debug.LogError($"[Incremental] {name}: no progression image assigned; the activity icon is dead.", this);

            if (levelLabel == null)
                Debug.LogError($"[Incremental] {name}: no level label assigned; the level will not be shown.", this);
        }

        public void SetProgress(float normalized, bool isPenalty)
        {
            // SetValueWithoutNotify: the slider is display-only, and onValueChanged would be a loop
            // waiting to happen if anything is ever wired to it in the inspector.
            if (slider != null) slider.SetValueWithoutNotify(normalized);
            if (fillImage != null) fillImage.color = isPenalty ? penaltyColor : normalColor;
        }

        public void SetLevel(int level)
        {
            if (levelLabel != null) levelLabel.text = level.ToString();
        }

        public void SetActivity(FocusCategory category, bool isOnBreak, bool isPaused)
        {
            if (progressionImage == null) return;

            // Assigned even when null, so an unset sprite reads as a missing icon rather than leaving
            // the previous one on screen claiming a state the user is no longer in.
            progressionImage.sprite = ResolveIcon(category, isOnBreak, isPaused);
        }

        // Both flags outrank the category, and pause outranks the break: neither is a judgement on the
        // focused app, so showing one would accuse the user of something not being counted either way.
        private Sprite ResolveIcon(FocusCategory category, bool isOnBreak, bool isPaused)
        {
            if (isPaused) return pauseIcon;

            return isOnBreak ? breakIcon : IconFor(category);
        }

        private Sprite IconFor(FocusCategory category)
        {
            switch (category)
            {
                case FocusCategory.Productive: return productiveIcon;
                case FocusCategory.Unproductive: return unproductiveIcon;
                default: return regularIcon;
            }
        }
    }
}
