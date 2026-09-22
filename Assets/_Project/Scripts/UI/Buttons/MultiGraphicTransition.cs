using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    [Serializable]
    public class MultiGraphicTransition
    {
        [SerializeField] private List<GraphicTarget> targets = new();

        private Coroutine[] _coroutines;
        private MonoBehaviour _owner;

        public void Initialize(MonoBehaviour owner)
        {
            _owner = owner;
            _coroutines = new Coroutine[targets.Count];
        }

        // Fixes ColorBlocks left at Unity's zeroed default (colorMultiplier 0 = invisible). Call from OnValidate.
        public void Validate()
        {
            foreach (var target in targets)
                if (target != null && target.colors.colorMultiplier <= 0f)
                    target.colors = ColorBlock.defaultColorBlock;
        }

        public void Transition(UIState state, bool immediate = false)
        {
            EnsureCoroutineArray();

            for (int i = 0; i < targets.Count; i++)
                TransitionTarget(i, state, immediate);
        }

        private void TransitionTarget(int index, UIState state, bool immediate)
        {
            var target = targets[index];
            if (target.graphic == null) return;

            Color color = ResolveColor(target.colors, state) * target.colors.colorMultiplier;

            if (_coroutines[index] != null)
                _owner.StopCoroutine(_coroutines[index]);

            if (immediate || target.transitionDuration <= 0f || !_owner.gameObject.activeInHierarchy)
            {
                target.graphic.color = color;
                return;
            }

            _coroutines[index] = _owner.StartCoroutine(
                LerpColor(target.graphic, color, target.transitionDuration)
            );
        }

        private void EnsureCoroutineArray()
        {
            if (_coroutines == null || _coroutines.Length != targets.Count)
                _coroutines = new Coroutine[targets.Count];
        }

        public static Color ResolveColor(ColorBlock block, UIState state) => state switch
        {
            UIState.Normal => block.normalColor,
            UIState.Highlighted => block.highlightedColor,
            UIState.Pressed => block.pressedColor,
            UIState.Selected => block.selectedColor,
            UIState.Disabled => block.disabledColor,
            _ => block.normalColor
        };

        private static IEnumerator LerpColor(Graphic graphic, Color target, float duration)
        {
            Color start = graphic.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                graphic.color = Color.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            graphic.color = target;
        }
    }
}