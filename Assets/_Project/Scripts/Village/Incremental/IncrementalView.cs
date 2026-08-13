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

        private void Awake()
        {
            if (slider == null)
                Debug.LogError($"[Incremental] {name}: no slider assigned; progress will not be shown.", this);

            if (fillImage == null)
                Debug.LogError($"[Incremental] {name}: no fill image assigned; penalties will not be visible.", this);
        }

        public void SetProgress(float normalized, bool isPenalty)
        {
            // SetValueWithoutNotify: the slider is display-only, and onValueChanged would be a loop
            // waiting to happen if anything is ever wired to it in the inspector.
            if (slider != null) slider.SetValueWithoutNotify(normalized);
            if (fillImage != null) fillImage.color = isPenalty ? penaltyColor : normalColor;
        }
    }
}
