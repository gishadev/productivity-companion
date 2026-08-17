# Village

The simulation the widget displays. `Incremental/` earns the levels; this turns them into villagers.

```
Village/
  VillageMasterSO.cs      prefab, variants, movement and break tuning — edit-time data
  VillageController.cs    entry point; owns the population, derives the activity, drives the AI
  VillageActivity.cs      Neutral / Working / Relaxing
  VillageView.cs          scene anchor: the villagers root and the walkable area
  WalkableArea.cs         trigger box marking where villagers may stand
  YSorting.cs             the one world-Y-to-sorting-order formula
  StaticYSort.cs          applies it to props that do not move
  VillageInstaller.cs     registers this, Viewport/ and Incremental/
  POI/
    VillagePOI.cs           abstract: target position, capacity, claim/release
    JobPOI.cs               one villager, works while the user works
    RelaxPOI.cs             several villagers, hide inside during breaks
    POIRegistry.cs          finds them and hands out claims
  Villagers/
    Villager.cs             renderer and animator; no logic
    VillagerState.cs        Idle, Wander, GoToJob, Working, GoToRelax, Hiding
    VillagersAIController.cs one state machine for the whole population
    VillagersFactory.cs     spawns, despawns, places
  Viewport/               render target and screen↔world mapping (see Incremental/README.md for the widget)
```

Villager count is `IncrementalController.Level + 1`, so a fresh save already has someone in it.

---

## ⚠️ Villagers must be on layer 6 (`Simulation`)

The simulation camera's culling mask is layer 6 only. A villager prefab left on `Default` instantiates
successfully, throws nothing, logs nothing, appears in the hierarchy — and renders nothing. This is the
single most likely way the feature looks broken.

`VillagersFactory` checks the first spawned villager against the camera's actual mask and logs an error
naming the layer. Once per run, not per villager: they all come off one prefab, and the scene lookup is
too expensive to repeat while restoring a large village.

Villagers want sorting layer **`SimEntities`**, which already exists in `TagManager.asset` — and so do
the houses, if they are meant to interleave with villagers. See Depth sorting below.

---

## The view is small, so the cap is not optional

The surface rect is a fixed 450 × 170 at `upscale: 1` and `pixelsPerUnit: 100`, giving
`orthographicSize = 0.85` and a visible world of **4.5 × 1.7 units**. A premade NPC frame is 19 × 24 px
= 0.19 × 0.24 units, so roughly 23 villagers fit edge to edge across the entire view.

`MaxLevel` is 200. Without `maxVillagers`, a long-lived save renders a solid wall of sprites into a
450-pixel texture. The cap is what makes the number a village.

The rig sits at world **(1000, 0, 0)** and never moves. Authoring the spawn region as a collider in the
scene keeps that offset out of the data entirely — `WalkableArea` reports world bounds directly, so
nothing has to know where the rig was parked.

---

## Three decisions that look odd without the context

**The population reconciles to a target; it does not react to level-ups.** `SetTarget(count)` spawns or
despawns until the count matches, and is safe to call repeatedly with the same number. That is what
makes a restored save work: level 20 materialises 21 villagers in one call at `Start`, where a purely
event-driven factory would show an empty village until the next level-up. It also absorbs a tick that
crosses several thresholds and fires `LevelUpEvent` several times.

`VillageController` reads `TargetVillagers` rather than the event's `Level` for the same reason — one
source of truth, so the primed path and the event path cannot disagree.

**One AI controller for everyone, built on a struct array.** `gishadev.tools.StateMachine` keys its
transition table on `IState` *instances*, so states cannot be shared between owners: every villager
would carry its own machine, dictionary, three lists, a transition object and a closure per edge, with
every condition delegate polled every frame. Here the population is a flat `Agent[]` walked in one
loop — no allocation, no virtual dispatch, and a new state is a case label.

It is an **array, not a `List<Agent>`**, because `ref var agent = ref _agents[i]` mutates the element in
place. `List<T>`'s indexer returns a copy of a struct, which would need a write-back on every field
change, and `CollectionsMarshal.AsSpan` is past this project's .NET Standard 2.1 level. Removal
backfills from the end rather than shifting, since order carries no meaning.

See **Wandering** below for the states themselves.

**Plain `Instantiate`, not the package pool.** Villagers are created and effectively never destroyed, so
pooling buys nothing, and `PoolManager.TryActivateAvailableObject` is O(n) over every instance of an
entry — it would scan 200 objects to spawn the 200th villager. It also keeps `VillageMasterSO` clear of
the `PoolData.asset` plus enum-regeneration round trip. Revisit if villagers ever start dying.

---

## Placement

The spawn region is `WalkableArea` — a trigger `BoxCollider2D` in the scene, not numbers in the asset.
It has to line up with the ground art, which is something you place by eye.

`WorldBounds` is derived from the collider's `offset` and `size` rather than read from
`Collider2D.bounds`. Physics only fills `bounds` in for a collider that is enabled on an active object,
and the village can be built while hidden — a zero bounds would silently stack every villager on one
point instead of failing. Rotation is ignored; a rotated spawn box is not a case worth supporting.

`SetTarget` places each villager by rejection sampling: up to `placementAttempts` candidates, taking the
first that is `minSpacing` from every existing villager, then **accepting the last candidate
regardless**. Once the area is full every candidate is too close, and accepting one is what stops that
being an infinite loop — the spacing is a preference, not a guarantee.

With no `WalkableArea` assigned, villagers stack on the villagers root and the factory warns once. That
is a deliberate fallback rather than a refusal to spawn: a visible pile is easier to diagnose than an
empty village.

Character variation is a random pick from `villagerVariants` and is deliberately **not persisted**: it
is re-rolled every launch.

---

## Animation

The Animator owns the sprite, so variety is a **clip set**, not a sprite — `villagerVariants` holds one
`AnimatorOverrideController` per character over a single shared controller and blend tree.

Parameters the controller must declare:

| Parameter | Type | Meaning |
|---|---|---|
| `MoveX` | float | facing X, **always non-negative** |
| `MoveY` | float | facing Y |
| `Speed` | float | 0 = idle, > 0 = walking |
| `Working` | bool | job animation |

`MoveX` is fed `Mathf.Abs(direction.x)` because the side clips are authored facing right and mirrored
for left through `flipX`. Feeding a signed X would require left-facing clips the sheets do not contain.
Three directions — Down, Side, Up — not four.

Parameters are written **on state entry, not per frame**: a leg of travel is a straight line, so the
direction it establishes holds for the whole walk, and `Speed` is binary. Facing is kept when speed
drops to zero, so a villager idles facing wherever it last walked. Hashes are cached in
`static readonly int` fields.

### ⚠️ Re-slice the sheets before authoring clips

The premade NPC sheets ship auto-sliced **tight-cropped with Center pivots** — frame rects vary from
18×24 to 30×35 within a single animation row, and `y` drifts frame to frame. Played as a clip, the
villager visibly bobs and jitters. Re-slice as **Grid By Cell Size 64×64, Pivot: Bottom-Center** first.
No amount of code compensates for this.

Frame layout is uniform across all eight premade NPCs: **0–17 idle** (Down, Side, Up), **18–35 walk**
(Down, Side, Up), then a 4-frame special and any tool-action blocks. Note the tool-action rows are
ordered **Side, Down, Up** — a different order from idle and walk.

---

## What the village reacts to

`VillageController.Activity` collapses the pomodoro and the focused app into one value, passed into the
AI each tick:

| Activity | Condition | Villagers |
|---|---|---|
| `Working` | running Work phase, focused app Productive | one takes each JobPOI |
| `Relaxing` | running break | some hide in RelaxPOIs |
| `Neutral` | anything else, paused included | wander only |

**It reads `FocusController.CurrentCategory`, not `EffectiveCategory`** — the one place in the codebase
that does. `EffectiveCategory` reports `Regular` whenever our own window has focus, which is right for
progression (it stops the widget paying out the last app's multiplier) but wrong here: it would make a
working villager down tools every time the user clicks the widget to check the timer. The village cares
which *app* the user is working in, and the companion is not one.

The cost is a deliberate, small inconsistency — clicking the widget during a work session earns at the
Regular rate while the villager still looks busy. That is the better of the two wrong answers.

---

## POIs

Scene MonoBehaviours, found once by `POIRegistry` with
`FindObjectsByType<VillagePOI>(FindObjectsInactive.Include, ...)` — the same lenient lookup the
installer uses everywhere, and inactive is included because the village can be built while hidden. An
empty scene is inert, not an error: `TryClaimJob`/`TryClaimRelax` simply return null and villagers
carry on wandering.

`VillagePOI` holds the occupancy so both kinds share one claim path. `Release` clamps at zero rather
than just decrementing — a release that outnumbers its claim would drive the count negative and leave
the POI permanently claimable by any number of villagers. `OnDisable` sheds occupants, because the AI
holds POI references across state changes and a disabled POI would otherwise come back still believing
it is full.

**JobPOI** is capacity 1 and exposes `IsOccupied`. **RelaxPOI** has a serialized capacity defaulting to
3: with a handful of buildings and dozens of villagers, a capacity of 1 would make hiding almost never
visible.

---

## Wandering and the daily round

Six states. **Idle** for a random `idleDuration`, then whatever the activity allows.

The destination is an offset from where the villager is standing (`wanderRadius`, via
`Random.insideUnitCircle`) pulled back inside the area with `WalkableArea.ClampInside`. Picking any
point in the area instead would send everyone marching the full width of the village on every walk, and
clamping rather than resampling means a destination is always valid on the first try — no rejection
loop, no chance of failing to find one near an edge.

Movement is `Vector2.MoveTowards` at `walkSpeed`, and `z` is carried through rather than zeroed: it
holds nothing today, but stomping it would be a silent surprise the moment anything uses it for depth.
Sprite facing is set once on entering Wander, not per frame.

Wander ends on arrival **or** on a timeout scaled from the straight-line travel time. The timeout is
the guard against `walkSpeed` being zero, which would otherwise leave a villager walking on the spot
forever. Idle villagers do no transform writes at all.

With no `WalkableArea`, Idle re-enters itself instead of transitioning — villagers stand still rather
than churning through a failed state change every frame.

**Claiming happens only at the end of an idle**, never per frame. That is the natural decision point,
it keeps the registry off the hot path, and it stops a villager abandoning a walk it just began. During
a break the villager rolls against `relaxChance` *before* claiming, so one that decides to stay out
does not briefly hold a slot.

A villager that comes out mid-break wanders and may roll again later, so a long break keeps churning
instead of settling into a frozen tableau.

**Hidden villagers stay in the agent array while their GameObject is disabled.** This is the clearest
payoff of the AI being a plain loop rather than a MonoBehaviour: Unity has stopped ticking the object,
but the agent keeps counting down and can turn itself back on. `agent.View == null` still distinguishes
*destroyed* from merely disabled, which is exactly the distinction that makes this safe.

Leaving `Relaxing` turns **everyone** out at once, however long they have left on the clock — that is
the "work starts, everybody outside" rule, and it is a single condition in `TickHiding`.

---

## Depth sorting

Lower on screen draws in front. `YSorting.OrderFor(worldY)` is the single formula, used by both paths:

- **Villagers** are sorted by `VillagersAIController` as part of the move it already performs. They
  deliberately carry no sorting component — an `Update` per prop is affordable, an `Update` per villager
  is exactly what the AI design exists to avoid. The order is cached per agent, so a walk that stays
  within one rendered pixel of height never touches the renderer.
- **Static props** get `StaticYSort`, which applies on enable and, in the editor only, follows the
  object while it is dragged around the scene.

`OrderPerUnit` is 100, matched to the art's pixels-per-unit so one step is one rendered pixel. It is a
`const` rather than a field on `VillageMasterSO` because `StaticYSort` is a plain scene component with
nothing injected into it — and the two paths agreeing matters far more than being able to tune it. The
result is clamped, since `sortingOrder` is backed by a short and a prop parked far from the origin would
otherwise wrap around and jump in front of everything.

### ⚠️ Sorting order only applies within one sorting layer

A villager on `SimEntities` will draw in front of a house on `SimGround` no matter what Y either is at
— the layer decides first, and the order is only a tiebreak inside it. **For villagers to walk behind
houses, both must sit on the same sorting layer.** Put the props and the villagers on `SimEntities` and
leave only the ground on `SimGround`.

The symptom of getting this wrong is villagers that sort correctly against each other and never against
the buildings.

---

## Scene requirements

- `VillageView` on the `Simulation Root` prefab, with a `villagersRoot` child (falls back to its own
  transform when unassigned) and a `walkableArea`.
- `WalkableArea` on a child, its `BoxCollider2D` sized over the ground. `Reset` marks the collider as a
  trigger — it delimits an area, it does not block anything.
- `StaticYSort` on each house and prop, moved onto the **same sorting layer as the villagers**. The
  ground stays on `SimGround` and needs no component: it is always behind everything.
- An `Animator` on `Villager.prefab` with the controller described above.
- `JobPOI` near a workplace with a `workPos` child where the villager stands; `RelaxPOI` on each
  building with an `enterPos` child at the door. POIs need no layer or sorting setup — they are logic
  markers and are never rendered.
- `VillageMasterSO` assigned on **CompanionLifetimeScope**. Unassigned, the installer logs one error and
  skips the whole village rather than registering things that null-ref every frame — the same shape as
  `IncrementalSettingsSO`.
- Every scene lookup here uses the installer's lenient `FindAnyObjectByType(FindObjectsInactive.Include)`
  and is null-checked, so a missing `VillageView` disables spawning without touching progression.
