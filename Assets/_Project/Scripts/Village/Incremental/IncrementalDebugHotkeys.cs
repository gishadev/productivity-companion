#if DEVELOPMENT_BUILD || UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;

namespace gishadev.companion.Village
{
    // U / I level up/down, J / K fill/clear penalty. Keys only arrive while our window has focus.
    public sealed class IncrementalDebugHotkeys : MonoBehaviour
    {
        private IncrementalController _incremental;

        [Inject]
        public void Construct(IncrementalController incremental) => _incremental = incremental;

        private void Update()
        {
            if (_incremental == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null || IsTyping()) return;

            if (keyboard.uKey.wasPressedThisFrame) _incremental.DebugSetLevel(_incremental.Level + 1);
            if (keyboard.iKey.wasPressedThisFrame) _incremental.DebugSetLevel(_incremental.Level - 1);
            if (keyboard.jKey.wasPressedThisFrame) _incremental.DebugFillPenalty();
            if (keyboard.kKey.wasPressedThisFrame) _incremental.DebugClearPenalty();
        }

        // Letter keys would otherwise fire while typing into settings fields.
        private static bool IsTyping()
        {
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            return selected != null && selected.GetComponent<TMP_InputField>() != null;
        }
    }
}
#endif
