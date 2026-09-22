using gishadev.companion.Infrastructure;
using gishadev.companion.Village;
using gishadev.tools.SavingSystem;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace gishadev.companion.EditorTools
{
    public sealed class CompanionDebugWindow : EditorWindow
    {
        // Must match CompanionLifetimeScope.SaveFileName.
        private const string SaveFileName = "companion";

        private int _targetLevel;

        [MenuItem("Tools/Debug")]
        private static void Open() => GetWindow<CompanionDebugWindow>("Companion Debug");

        // Level and progress change every frame in play mode.
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            var incremental = ResolveIncremental();

            DrawLevel(incremental);

            if (incremental != null)
            {
                EditorGUILayout.Space();
                DrawPenalty(incremental);
            }

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

        private static void DrawPenalty(IncrementalController incremental)
        {
            EditorGUILayout.LabelField("Penalty", EditorStyles.boldLabel);

            var owed = incremental.PenaltySeconds;
            var state = incremental.DebugPenaltyFired ? "triggered" : "not triggered";
            EditorGUILayout.LabelField("Owed", $"{owed:0.0}s  ({state})");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Penalty")) incremental.DebugFillPenalty();
                if (GUILayout.Button("Clear Penalty")) incremental.DebugClearPenalty();
            }

            if (incremental.DebugPenaltyFired)
                EditorGUILayout.HelpBox(
                    "A running work phase will drain this as soon as you are in a productive or regular app.",
                    MessageType.None);
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
            // Live saver while playing, so its in-memory cache is cleared too.
            var saver = ResolveSaver() ?? new FileSaverSystem(SaveFileName);
            saver.ClearAll();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // Otherwise the running controller writes its old level straight back.
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

            return scope.Container.TryResolve<T>(out var resolved) ? resolved : null;
        }
    }
}
