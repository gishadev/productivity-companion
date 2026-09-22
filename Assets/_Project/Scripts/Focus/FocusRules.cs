using System;
using System.Collections.Generic;
using gishadev.companion.SavingLoading;
using gishadev.tools.SavingSystem;

namespace gishadev.companion.Focus
{
    public sealed class FocusRules
    {
        private readonly SaveSlot<FocusRulesData> _slot;
        private readonly FocusRulesData _state;

        // JsonUtility can't serialize HashSet: these are the lookup copy of the saved lists.
        private readonly HashSet<string> _productive = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unproductive = new(StringComparer.OrdinalIgnoreCase);

        public FocusRules(ISaverSystem saver)
        {
            _slot = new SaveSlot<FocusRulesData>(saver, SaveKeys.FocusRules);
            _state = _slot.Load();
            _state.productive ??= new List<string>();
            _state.unproductive ??= new List<string>();

            Fill(_productive, _state.productive);
            Fill(_unproductive, _state.unproductive);
        }

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

        // False when nothing changed (no save, no Changed).
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

            _slot.Save(_state);
            Changed?.Invoke();
        }

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
    }
}
