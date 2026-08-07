using System;
using gishadev.tools.SavingSystem;
using UnityEngine;

namespace gishadev.companion.UI
{
    /// <summary>Persisted state of the simulation window.</summary>
    public sealed class SimulationWindowSettings
    {
        private const string SaveKey = "simulation.window.settings";

        private readonly ISaverSystem _saver;
        private readonly State _state;

        public SimulationWindowSettings(ISaverSystem saver)
        {
            _saver = saver;
            _state = LoadState(saver);
        }

        /// <summary>Raised after any value is persisted.</summary>
        public event Action Changed;

        /// <summary>Takes the side from where the widget sits on screen, leaving <see cref="Flip"/> unused.</summary>
        public bool AutoFlip
        {
            get => _state.autoFlip;
            set => SetBool(ref _state.autoFlip, value);
        }

        /// <summary>
        /// Which side to use while <see cref="AutoFlip"/> is off; true is the bottom position. Kept
        /// intact while auto-flipping so turning it back off restores what the user picked by hand.
        /// </summary>
        public bool Flip
        {
            get => _state.flip;
            set => SetBool(ref _state.flip, value);
        }

        public bool IsHidden
        {
            get => _state.isHidden;
            set => SetBool(ref _state.isHidden, value);
        }

        private void SetBool(ref bool field, bool value)
        {
            if (field == value) return;

            field = value;
            _saver.Save(SaveKey, JsonUtility.ToJson(_state));
            Changed?.Invoke();
        }

        private static State LoadState(ISaverSystem saver)
        {
            if (!saver.TryLoad(SaveKey, out var json) || string.IsNullOrEmpty(json))
                return new State();

            return JsonUtility.FromJson<State>(json) ?? new State();
        }

        // Field initializers double as the defaults: JsonUtility leaves anything the saved blob is
        // missing untouched, so an older file gains new settings rather than zeroing them.
        [Serializable]
        private sealed class State
        {
            public bool autoFlip = true;
            public bool flip;
            public bool isHidden;
        }
    }
}
