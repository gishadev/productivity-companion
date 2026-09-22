using gishadev.companion.Focus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.Village
{
    // One slider, two meanings: level progress or penalty owed, told apart by fill colour.
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

            // Assigned even when null, so a stale icon never lingers.
            progressionImage.sprite = ResolveIcon(category, isOnBreak, isPaused);
        }

        // Pause outranks break, and both outrank the category.
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
