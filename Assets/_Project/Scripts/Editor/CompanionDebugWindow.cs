using gishadev.companion.Infrastructure;
using gishadev.companion.Village;
using gishadev.tools.SavingSystem;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace gishadev.companion.EditorTools
{
    /// <summary>
    /// Levelling takes real time by design, which makes anything keyed off level slow to exercise by
    /// hand. This reaches into the running container and moves the level directly.
    /// </summary>
    public sealed class CompanionDebugWindow : EditorWindow
    {
        // The same name CompanionLifetimeScope constructs its FileSaverSystem with. Duplicated rather
        // than exposed, because widening that constant's visibility for a debug window would be the
        // wrong direction of dependency.
        private const string SaveFileName = "companion";

        private int _targetLevel;

        [MenuItem("Tools/Debug")]
        private static void Open() => GetWindow<CompanionDebugWindow>("Companion Debug");

        // Level and progress move every frame in play mode; without this the window shows whatever was
        // true when it last happened to repaint.
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            var incremental = ResolveIncremental();

            DrawLevel(incremental);
            EditorGUILayout.Space();
            DrawReset(incremental);
        }

        private void DrawLevel(IncrementalController incremental)
        {
            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);

            if (incremental == null)
            {
                EditorGUILayout.HelpBox(
                    Application.isPlaying
                        ? "No IncrementalController in the container. Check that VillageMaster and " +
                          "IncrementalSettings are assigned on CompanionLifetimeScope."
                        : "Enter play mode to change the level.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Current", $"{incremental.Level}  ({incremental.Progress:P0})");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Level Down")) incremental.DebugSetLevel(incremental.Level - 1);
                if (GUILayout.Button("Level Up")) incremental.DebugSetLevel(incremental.Level + 1);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _targetLevel = Mathf.Max(0, EditorGUILayout.IntField(_targetLevel));
                if (GUILayout.Button("Apply Level", GUILayout.Width(110f)))
                    incremental.DebugSetLevel(_targetLevel);
            }
        }

        private void DrawReset(IncrementalController incremental)
        {
            EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Wipes the save file and every PlayerPrefs key, including window settings. " +
                "Subsystems other than progression keep their in-memory state until play mode restarts.",
                MessageType.Warning);

            if (!GUILayout.Button("Reset All Data")) return;

            if (!EditorUtility.DisplayDialog(
                    "Reset all data?",
                    "Deletes the companion save file and all PlayerPrefs. This cannot be undone.",
                    "Reset", "Cancel"))
                return;

            ResetAll(incremental);
        }

        private static void ResetAll(IncrementalController incremental)
        {
            // Resolved from the container while playing so the live instance's own cache is cleared too;
            // constructed directly otherwise, since out of play mode there is nothing holding the file.
            var saver = ResolveSaver() ?? new FileSaverSystem(SaveFileName);
            saver.ClearAll();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // In-memory progression is reset explicitly: clearing the file leaves the running controller
            // holding its old level, which would then be written straight back out on the next save.
            if (incremental != null) incremental.DebugResetState();

            Debug.Log("[Companion] Save data and PlayerPrefs cleared.");
        }

        private static IncrementalController ResolveIncremental() => Resolve<IncrementalController>();

        private static ISaverSystem ResolveSaver() => Resolve<ISaverSystem>();

        private static T Resolve<T>() where T : class
        {
            if (!Application.isPlaying) return null;

            var scope = FindAnyObjectByType<CompanionLifetimeScope>();
            if (scope == null || scope.Container == null) return null;

            // Registration is skipped entirely when the scope's assets are unassigned, so a miss here is
            // a normal state rather than a bug worth throwing over.
            return scope.Container.TryResolve<T>(out var resolved) ? resolved : null;
        }
    }
}
