# Incremental mechanic

Turns the focus category into village progress. Regular apps earn a baseline, productive apps a
multiple of it, and unproductive apps instead accrue a capped **penalty** that has to be paid back
before progress resumes at all.

```
Village/Incremental/
  IncrementalSettingsSO.cs  balance data; an asset, not a save-backed settings class
  IncrementalController.cs  the state machine, entry point, owns persistence
  IncrementalView.cs        one slider, two meanings
```

Registered by `VillageInstaller`. `LevelUpEvent`, `PenaltyTriggeredEvent` and `PenaltyClearedEvent`
(in `Events/IncrementalEvents.cs`) fire on the bus and are deliberately **unconsumed** — they are the
seam the simulation hangs off once it has entities.

---

## The rules

Per tick, with `dt` clamped to `MaxTickDelta`:

Nothing at all happens unless the pomodoro is running. While it is:

| Category | Penalty owed | Effect |
|---|---|---|
| Unproductive, on a break | — | **inert** — no penalty, no points |
| Unproductive, working | — | `penalty += dt`, capped. No points. |
| Productive / Regular | yes | `penalty -= recovery(category) * dt`. **No points.** |
| Productive / Regular | no | `progress += basePointsPerSecond * multiplier(category) * pomodoro * dt` |

The middle row is the whole design: 30 seconds in an unproductive app is not a pause, it is a debt.
Time afterwards goes to clearing it, and only then back to levelling.

The earn rate is the product of **what** the user is doing (`FocusCategory` → regular / productive)
and **what the pomodoro is doing**:

| Timer | Rate |
|---|---|
| Running, Work | full |
| Running, Short / Long break | `BreakMultiplier` |
| Not running (paused, reset, never started) | **zero** |

Breaks keep the village ticking over rather than stalling it, so resting is quieter but never
punished — a running break also suspends penalty accrual entirely, which is what a break is for.

A stopped timer freezes **everything**: no points, and the penalty holds its value rather than
accruing or draining, so a session resumes exactly where it left off. The mechanic measures pomodoro
time, and a paused pomodoro is not time.

`PenaltyTriggeredEvent` fires **once** on hitting the cap, latched by `_penaltyFired`. The latch only
rearms when the penalty drains to zero, which is what fires `PenaltyClearedEvent`. The two are a pair:
without the clear, anything that reacts to a penalty by degrading has no signal to recover on. A
partial penalty the user never maxed out drains silently — neither event fires.

The slider is one bar reinterpreted by mode: penalty owed over the cap in red, otherwise progress
through the current level. Rising red = digging; shrinking red = paying it off.

---

## Five decisions that look odd without the context

**The running check sits in `Tick`, above everything else.** Pausing suspends penalties as well as
progress, so rather than threading an `IsRunning` test through each rule, `Advance` simply does not
run. That is also why `PomodoroMultiplier` has no running check of its own — it is unreachable while
stopped. Pausing is therefore neutral rather than exploitable: it buys penalty immunity at the price
of earning nothing, so there is no way to come out ahead by using it.

`IsOnBreak` still requires `IsRunning` even though `Advance` has already established it, because a
stopped timer holds whatever phase it last *finished*. Every completed work session leaves the phase
on a break, so a phase-only test would read as "on a break" indefinitely.

Unproductive time on a break is **inert**, not rewarded: it accrues no penalty, but pays none off and
earns no points either. An owed penalty simply waits for the break to end.


**`_progress` is a `double`, and it is not optional.** It holds a 0–1 fraction, so `float` looks like
the obvious type. At 32-bit precision a frame's increment stops moving the accumulator entirely once
the threshold passes ~2.8e5 — **level 31, about 520 hours of use** — and progress silently freezes on
a bar that reads full. Normalizing into the level does *not* avoid this: the increment is divided by
the same threshold it is racing, so the raw and normalized forms stall at the identical level. Only
the wider mantissa helps, moving the wall to level 98 (~1e14 points, i.e. never). It is stored as a
`float` on the way to disk — only the repeated addition needs the precision, and that also keeps the
save inside the field types `JsonUtility` reliably round-trips.

**Time is accumulated from frame deltas, where `PomodoroTimer` deliberately refuses to.** A pomodoro
must count real minutes even while the app sleeps, hence its UTC end timestamp. This wants the
opposite — only time the user actually spent in an app should count — so deltas *are* the measure.
`RenderThrottle` throttles rendering, not the game loop, so ticks keep arriving while the widget is
unfocused, which is the entire time this needs to be counting. `MaxTickDelta` exists only to stop a
resume-from-sleep frame dumping minutes of progress at once. This whole system is dead if
`runInBackground` is ever turned off.

**The category is read in `Tick`, not subscribed to.** `Focus/README.md` documents subscribe-and-prime,
which is right for reacting to *transitions*. This integrates over time and is already ticking, so
polling `FocusController.EffectiveCategory` removes a subscription and a second copy of the category
that could drift. It also sidesteps an ordering trap: `FocusController.Start()` polls immediately and
can fire its event before a later-registered subscriber exists.

**It reads `EffectiveCategory`, never `CurrentCategory`.** `FocusController` ignores our own window, so
`CurrentCategory` stays pinned to the last *foreign* app — correct for the tagging hotkeys, wrong as a
reward input. Sitting in the widget would otherwise pay out the previous app's multiplier, and if that
app was unproductive it would keep accruing penalty with no way to escape from inside the app.
`EffectiveCategory` reports own-window time as `Regular`.

---

## Storage

Shares `companion.sg` under `incremental.state`:

```json
{"level":3,"progress":0.42}
```

Written on level-up and then **debounced to `PersistInterval`**, plus once on `Dispose`.
`FileSaverSystem` flushes to disk on every `Save`, so a write per tick is not an option — but
persisting only on `Dispose` would lose everything to the force-kill a tray widget eventually gets.

`PenaltySeconds` is **not** persisted: it is an in-session behavioural streak and starts at zero each
launch. Restoring it would also mean restoring `_penaltyFired`, or the cleared event could fire
without a matching trigger.

Restore clamps `level` and `progress` and treats a non-finite `progress` as zero — `NaN` fails every
comparison, so it would freeze progression permanently and invisibly. The `FromJson` call is wrapped
because it runs during container build, where an exception takes the whole app down over a bad save.

---

## Tuning

`IncrementalSettingsSO` is an **asset**, unlike `PomodoroSettings` / `WindowSettings` / 
`VillageWindowSettings`, which are save-backed C# classes. Those are user-editable; this is designer
balance data that is never written at runtime and has no UI, so it belongs in the build rather than in
the save file.

`BaseThreshold` and `GrowthFactor` are re-clamped on read, not just via `[Min]`. The attribute does not
touch values already serialized into an asset, and a zero threshold — or a growth factor below 1 —
makes `Threshold()` collapse toward zero and the level-up `while` loop unbounded. `MaxLevel` caps the
other end so `Math.Pow` cannot reach infinity and feed the slider a `NaN`.

---

## Scene requirements

A **Slider** with `IncrementalView`, its `slider` and Fill `Image` wired, and **Interactable off**.
The controller resolves the view leniently (`FindAnyObjectByType`, inactive included) and null-checks
every access, so a missing or hidden slider disables the display without touching progression.

The view is compared with `== null`, never `?.` or `is null` — a destroyed `UnityEngine.Object` only
reports itself as null through the overloaded operator, and the null-propagating forms would NRE.

`IncrementalSettingsSO` must be assigned on **CompanionLifetimeScope** in the scene. Unassigned, the
installer logs an error and skips registering the controller entirely, rather than registering one
that null-refs every frame.
