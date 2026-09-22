# Saving & loading

Everything the companion persists goes through one `ISaverSystem` (`FileSaverSystem("companion")`,
registered in `CompanionLifetimeScope`). This folder holds the shape of that data; the systems that
own the rules (clamping, events) stay in their own folders. `WindowSettings` and `TabGroup` are the
exceptions: they use PlayerPrefs.

```
SavingLoading/
  SaveKeys.cs      every key in the file, in one place
  SaveSlot.cs      typed Load()/Save(T) over one key
  Data/            the [Serializable] payloads, one per key
```

A system builds its slot in its constructor and keeps the loaded instance as its live state:

```csharp
_slot = new SaveSlot<PomodoroSettingsData>(saver, SaveKeys.PomodoroSettings);
_state = _slot.Load();   // never null; defaults when missing or unreadable
...
_slot.Save(_state);
```

Field initializers on a data class are its defaults, and also what an older save gains when a field
is added later (JsonUtility leaves missing fields untouched). Renaming a field or a key drops that
value from existing saves.
