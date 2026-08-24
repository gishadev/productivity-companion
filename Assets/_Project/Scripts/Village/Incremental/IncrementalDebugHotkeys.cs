#if DEVELOPMENT_BUILD || UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Development-only shortcuts for progression, which otherwise takes real minutes to move. Stripped
    /// from release builds. Like the window hotkeys, the keys only arrive while our own window has
    /// focus, so alt-tab back to the widget before pressing anything.
    ///
    /// U / I — level up, level down. J / K — fill the penalty, clear it.
    /// </summary>
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

        // These are ordinary letters, unlike the window hotkeys' function keys, so they would otherwise
        // fire while the user is typing into the settings fields — the raw key is readable whether or
        // not the field accepts the character.
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
