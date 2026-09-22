using System;
using gishadev.tools.SavingSystem;
using UnityEngine;

namespace gishadev.companion.SavingLoading
{
    public sealed class SaveSlot<T> where T : class, new()
    {
        private readonly ISaverSystem _saver;
        private readonly string _key;

        public SaveSlot(ISaverSystem saver, string key)
        {
            _saver = saver;
            _key = key;
        }

        public bool Exists => _saver.Exists(_key);

        // Never null. Field initializers on T are the defaults for anything missing from the blob.
        public T Load()
        {
            if (!_saver.TryLoad(_key, out var json) || string.IsNullOrEmpty(json)) return new T();

            try
            {
                return JsonUtility.FromJson<T>(json) ?? new T();
            }
            catch (Exception exception)
            {
                // Loads run during container build; a corrupt save must not take the app down.
                Debug.LogWarning($"[Save] Discarding unreadable '{_key}': {exception.Message}");
                return new T();
            }
        }

        public void Save(T data) => _saver.Save(_key, JsonUtility.ToJson(data));
    }
}
