using System;
using System.Collections.Generic;
using gishadev.tools.SavingSystem;
using UnityEngine;

namespace gishadev.companion.Focus
{
    /// <summary>
    /// Persisted process-name sets that classify the foreground application. Keys are matched
    /// case-insensitively and without the ".exe" suffix.
    /// </summary>
    public sealed class FocusRules
    {
        private const string SaveKey = "focus.rules";

        private readonly ISaverSystem _saver;
        private readonly State _state;

        // JsonUtility cannot serialize a HashSet, so State holds lists and these are the lookup copy,
        // rebuilt from them on load and written back to them on save.
        private readonly HashSet<string> _productive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unproductive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public FocusRules(ISaverSystem saver)
        {
            _saver = saver;
            _state = LoadState(saver);

            Fill(_productive, _state.productive);
            Fill(_unproductive, _state.unproductive);
        }

        /// <summary>Raised after a rule change is persisted.</summary>
        public event Action Changed;

        public IReadOnlyCollection<string> Productive => _productive;

        public IReadOnlyCollection<string> Unproductive => _unproductive;

        public FocusCategory Classify(string processName)
        {
            var key = Normalize(processName);
            if (key == null) return FocusCategory.Regular;

            if (_productive.Contains(key)) return FocusCategory.Productive;
            if (_unproductive.Contains(key)) return FocusCategory.Unproductive;
            return FocusCategory.Regular;
        }

        /// <summary>
        /// Assigns a process to one set and removes it from the other; <see cref="FocusCategory.Regular"/>
        /// clears it from both. False when nothing changed, so no write and no <see cref="Changed"/>.
        /// </summary>
        public bool Set(string processName, FocusCategory category)
        {
            var key = Normalize(processName);
            if (key == null) return false;

            var productiveChanged = category == FocusCategory.Productive
                ? _productive.Add(key)
                : _productive.Remove(key);

            var unproductiveChanged = category == FocusCategory.Unproductive
                ? _unproductive.Add(key)
                : _unproductive.Remove(key);

            if (!productiveChanged && !unproductiveChanged) return false;

            Persist();
            return true;
        }

        public bool Remove(string processName) => Set(processName, FocusCategory.Regular);

        private void Persist()
        {
            _state.productive.Clear();
            _state.productive.AddRange(_productive);
            _state.unproductive.Clear();
            _state.unproductive.AddRange(_unproductive);

            _saver.Save(SaveKey, JsonUtility.ToJson(_state));
            Changed?.Invoke();
        }

        /// <summary>One key format for every caller, so hotkeys and any future UI cannot disagree.</summary>
        private static string Normalize(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return null;

            var key = processName.Trim().ToLowerInvariant();
            if (key.EndsWith(".exe", StringComparison.Ordinal))
                key = key.Substring(0, key.Length - 4);

            return key.Length == 0 ? null : key;
        }

        private static void Fill(HashSet<string> set, List<string> source)
        {
            foreach (var entry in source)
            {
                var key = Normalize(entry);
                if (key != null) set.Add(key);
            }
        }

        private static State LoadState(ISaverSystem saver)
        {
            if (!saver.TryLoad(SaveKey, out var json) || string.IsNullOrEmpty(json))
                return new State();

            var loaded = JsonUtility.FromJson<State>(json);
            if (loaded == null) return new State();

            loaded.productive ??= new List<string>();
            loaded.unproductive ??= new List<string>();
            return loaded;
        }

        [Serializable]
        private sealed class State
        {
            public List<string> productive = new List<string>();
            public List<string> unproductive = new List<string>();
        }
    }
}
