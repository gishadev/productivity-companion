# Focus system

Tracks which application owns the foreground and classifies it as **Productive**, **Unproductive** or
**Regular**, firing `FocusCategoryChangedEvent` on the event bus whenever that changes. This is the
input the incremental mechanic runs on — nothing here rewards or scores anything yet.

```
Focus/
  FocusCategory.cs    Productive / Unproductive / Regular (0 = the default for anything unlisted)
  FocusRules.cs       the persisted process-name sets; save key "focus.rules"
  FocusController.cs  polls, classifies, fires the event
  FocusInstaller.cs
```

The native read lives in `Window/Native/` (`IForegroundWindowProvider`) so the rule that nothing
outside that folder touches user32 still holds. Unlike `IPlatformWindow` it is live in the editor,
so all of this can be exercised in play mode.

---

## Classification

Keyed on **process name only** — lowercase, trimmed, no `.exe`. `FocusRules.Normalize` is the single
place that decides the key format, so hotkeys and any future UI cannot disagree about it.

```csharp
rules.Set("Discord.exe", FocusCategory.Unproductive);  // stored as "discord"
rules.Classify("discord");                             // Unproductive
rules.Remove("discord");                               // back to Regular
```

`Set` is mutually exclusive: assigning a category removes the process from the other set, and
`Regular` clears it from both. It returns `false` when nothing changed, so no write and no `Changed`.

`ForegroundWindowInfo` also carries the window **title**, which nothing classifies on today. It is
captured so title rules ("contains YouTube → unproductive") can be added without reopening the native
seam.

Two known limits of a process-name key: one browser is one entry regardless of what is on screen, and
every UWP app reports `applicationframehost`.

---

## Three decisions that look odd without the context

**The player's own window is ignored.** When the user clicks the widget, our process takes the
foreground; the poll returns early and the category sticks. Without this, interacting with the widget
would reset the user's activity every time. It also means `FocusController.CurrentProcessName` is
always the last *foreign* app, which is what makes the debug hotkeys usable at all — see the
`Window/README.md` overlay section.

**The event fires on change only.** A subscriber that starts late reads `CurrentCategory` to prime
itself, the same contract `PomodoroPresenter` has with `PomodoroTimer`. Sitting in one app produces no
traffic; alt-tabbing between two apps in the same category produces none either.

**`FocusRules.Changed` re-classifies immediately.** Tagging an app takes effect on the spot rather
than up to `PollInterval` later, which is the difference between the hotkeys feeling broken and not.

---

## Consuming it

```csharp
public sealed class Something : IStartable, IDisposable
{
    public Something(IEventBus eventBus, FocusController focus) { ... }

    void IStartable.Start()
    {
        _eventBus.Subscribe<FocusCategoryChangedEvent>(OnFocusChanged);
        Apply(_focus.CurrentCategory);   // prime: the event already fired if it was going to
    }

    public void Dispose() => _eventBus.Unsubscribe<FocusCategoryChangedEvent>(OnFocusChanged);
}
```

`FocusController` is registered `.AsSelf()`, so it can be injected directly for `CurrentCategory` /
`CurrentProcessName` / `CurrentTitle` without going through the bus.

The user-facing editor is `UI/Focus/FocusSettingsView`. Its list is rebuilt from `FocusRules` on
`Changed` rather than saved on its own — `FocusRules` stays the single persisted source.

---

## Storage

Shares `companion.sg` with the other subsystems, under the `focus.rules` key:

```json
{"productive":["rider","code"],"unproductive":["discord"]}
```

`JsonUtility` cannot serialize a `HashSet`, so `State` holds `List<string>` and `FocusRules` keeps an
`OrdinalIgnoreCase` `HashSet` beside it for lookups — rebuilt from the lists on load, written back to
them right before each save. Do not "simplify" one of the two away.
