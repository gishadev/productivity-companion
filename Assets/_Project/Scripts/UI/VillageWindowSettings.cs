using System;
using gishadev.companion.SavingLoading;
using gishadev.tools.SavingSystem;

namespace gishadev.companion.UI
{
    public sealed class VillageWindowSettings
    {
        private readonly SaveSlot<VillageWindowData> _slot;
        private readonly VillageWindowData _state;

        public VillageWindowSettings(ISaverSystem saver)
        {
            _slot = new SaveSlot<VillageWindowData>(saver, SaveKeys.VillageWindow);
            _state = _slot.Load();
        }

        public event Action Changed;

        public bool AutoFlip
        {
            get => _state.autoFlip;
            set => SetBool(ref _state.autoFlip, value);
        }

        // Used while AutoFlip is off; true is bottom. Kept while auto-flipping so it comes back.
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
            _slot.Save(_state);
            Changed?.Invoke();
        }
    }
}
