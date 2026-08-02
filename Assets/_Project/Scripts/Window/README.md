# Window system

Makes the Windows standalone player behave like a desktop widget: transparent, selectively
click-through, optionally always-on-top and hidden from the taskbar, and cheap to run while unfocused.

Every behaviour is a persisted, independently toggleable setting in `WindowSettings` (PlayerPrefs,
keys prefixed `window.`).

---

## ⚠️ Required project settings

**Transparency depends on all six of these. Any one of them flipped back breaks it silently — the
window just renders opaque or washed out, with no error anywhere.** None of this is visible from the
code, which is why this file exists.

| Setting | Required value | Where | Why |
|---|---|---|---|
| Graphics API | **Direct3D11** (never D3D12) | Player → Rendering | D3D12 always uses flip-model presentation, which bypasses the layered-window redirection surface. Both transparency modes are inert. Auto Graphics API picks D3D12 on most GPUs — leave it **unchecked**. |
| `useFlipModelSwapchain` | `0` | Player → Rendering | Same reason, for D3D11. Only has any effect on D3D11. |
| `preserveFramebufferAlpha` | `1` | Player → Rendering | Without it Unity discards the alpha channel on present, so the camera's `a = 0` clear never reaches DWM. |
| `m_SupportsHDR` | `0` | UniversalRP.asset | The HDR resolve writes `alpha = 1` into the backbuffer. |
| `m_AllowPostProcessAlphaOutput` | `1` | UniversalRP.asset ("Alpha Processing") | Gates whether alpha survives to the backbuffer at all. Without it everything renders uniformly translucent, **including fully opaque content**. |
| `fullscreenMode` | `3` (Windowed) | Player → Resolution | A layered window cannot be exclusive fullscreen. Also forced at runtime in `WindowController.Awake`, because Unity persists the last screen mode in the registry. |

Camera clear flags and background colour are set at runtime by `WindowController` and should not be
authored in the scene.

---

## Architecture

```
Window/
  Native/               all Win32; nothing outside this folder touches user32/dwmapi
    Win32Interop.cs       raw DllImports + constants, no Unity types
    IPlatformWindow.cs    the only surface the rest of the app sees
    Win32PlatformWindow.cs
    NullPlatformWindow.cs no-op; used in the editor and on every non-Windows target
    PlatformWindowFactory.cs  the single place that decides whether native calls are live
  WindowSettings.cs     persisted settings; raises Changed(WindowSetting)
  WindowController.cs   applies settings to the window and to Unity player state
  ClickThroughController.cs
  TopmostWatchdog.cs
  RenderThrottle.cs
  WidgetDragHandle.cs   scene component: drag to reposition the widget
  WindowBootstrap.cs    [RuntimeInitializeOnLoadMethod] entry point
  WindowDebugHotkeys.cs dev-only overlay (stripped from release builds)
```

Two decisions that look odd without the context:

**Components get explicit `Initialize(...)` calls, not `[Inject]`.** The project's root scope
(`gishadev.tools.AutoInjectLifetimeScope`) re-injects every `[Inject]`-bearing MonoBehaviour found by
`FindObjectsByType` on each scene load — and that sweep includes `DontDestroyOnLoad` objects. The
window services live in a child scope the root container knows nothing about, so that pass would log
an injection failure for every component on every scene load. Registrations still live in VContainer
(`WindowInstaller`); only the delivery is manual. Scene-authored components (e.g. `WidgetDragHandle`)
reach services through the static accessors on `WindowBootstrap`, which is the same problem with no
cleaner answer.

**`RenderThrottle` and `ClickThroughController` hand out refcounted `IDisposable` leases**, not bools,
so independent systems (a drag, an overlay, an animation) can each hold "keep rendering" or "keep
accepting clicks" without clearing each other's.

---

## Transparency modes

| | PerPixelAlpha (default) | ColorKey |
|---|---|---|
| Mechanism | `DwmExtendFrameIntoClientArea` glass + `WS_EX_LAYERED`/`LWA_ALPHA` | `WS_EX_LAYERED` + `LWA_COLORKEY` |
| Soft edges, partial alpha | ✅ | ❌ **impossible** |
| Click-through over empty space | needs the raycast in `ClickThroughController` | free — Windows hit-tests keyed pixels as transparent |
| Driver reliability | can misbehave on some GPUs | robust |

`WS_EX_LAYERED` is **required** in PerPixelAlpha mode even though DWM does the actual compositing:
`WS_EX_TRANSPARENT` only passes clicks reliably on a layered window, and a layered window that never
receives `SetLayeredWindowAttributes` is not guaranteed to paint. Removing it breaks click-through and
leaves the desktop unusable behind a full-screen invisible window.

### ColorKey is a binary test

`LWA_COLORKEY` punches out pixels that match the key **exactly**. There is no partial state, so
anti-aliased edges and semi-transparent pixels blend toward the key and show up as magenta fringe or
magenta fill. That is inherent, not a bug. If the widget needs soft edges, PerPixelAlpha is a
requirement, not a preference.

The key is `RGB(255, 0, 255)` (`WindowController.DefaultColorKey`). Only `0` and `255` are used per
channel deliberately: the project renders in **Linear** colour space, so the clear colour round-trips
through sRGB→linear→sRGB and intermediate values (254 was the first attempt) come back off by a unit
and stop matching.

For ColorKey to work at all, sprites must import with **Compression: None** and **Filter Mode: Point**.
BC3 compression quantises alpha in 4×4 blocks, turning clean zeros into small non-zero values, so
"transparent" regions blend to *near* magenta, miss the exact match, and stay visible as magenta fill.

---

## Settings

| Setting | Default | Notes |
|---|---|---|
| `TransparencyMode` | `PerPixelAlpha` | |
| `ClickThrough` | `true` | **Coupled to the transparency default on purpose.** The window covers the whole screen; transparent without click-through leaves the user unable to click anything on their desktop. |
| `AlwaysOnTop` | `false` | Starts/stops `TopmostWatchdog` |
| `HideFromTaskbar` | `false` | `WS_EX_TOOLWINDOW`; needs a hide/show cycle to take effect |
| `TargetFrameRate` | `60` | 15 / 30 / 60 |
| `PreventDisplaySleep` | `false` | |

Cleared by deleting `HKCU\Software\DefaultCompany\productivity-companion` (keys prefixed `window.`).

---

## Debug overlay

Development builds only. **F7** toggles it; it force-blocks click-through while the cursor is over the
panel, otherwise it would be unreachable.

`F1` transparency · `F2` click-through · `F3` always-on-top · `F4` hide from taskbar ·
`F5` target FPS · `F6` prevent sleep · `F7` overlay

The `Native window: available / UNAVAILABLE` line is the first thing to check: `UNAVAILABLE` means
HWND resolution failed and no Win32 call ran, which is a completely different problem from a
rendering one.

---

## Troubleshooting

| Symptom | Cause |
|---|---|
| Both modes fully opaque | Graphics API is D3D12 |
| PerPixelAlpha shows a black background | `preserveFramebufferAlpha` is off |
| Everything uniformly translucent, *including opaque content* | URP Alpha Processing off (or HDR on) |
| ColorKey shows magenta fill in transparent regions | Sprite texture compression is on |
| ColorKey shows magenta outlines | Anti-aliased edges — inherent to color keying |
| Clicks/drags land in the wrong place, or need a mouse jiggle first | Unity's pointer position went stale. `ClickThroughController` syncs it from the OS cursor every frame; if that regresses, this returns |
| Click-through never blocks over content | No EventSystem/GraphicRaycaster in the scene — `RaycastAll` can never report a hit |
| Nothing visible on first run | Correct with an empty scene: transparent + click-through + no Canvas. Press F7 |

Transparency **never works in the editor** — `PlatformWindowFactory` returns `NullPlatformWindow`
there. A standalone build is required to test any of this.

---

## Scene requirements

- An **EventSystem** and a **Canvas** with a **GraphicRaycaster** — required for click-through to
  block over content and for `WidgetDragHandle` to work
- `WidgetDragHandle` needs a Graphic with **Raycast Target** enabled. In ColorKey mode it must also be
  **visibly opaque**, or the OS keys it out and the clicks never reach Unity

---

## Not implemented

- **System tray icon** — needs `Shell_NotifyIcon` plus a subclassed `WndProc` message pump to receive
  click callbacks. Only taskbar hiding (`WS_EX_TOOLWINDOW`) exists today.
- **Monitor targeting** — the window is sized from Player Settings (1920×1080) rather than the actual
  monitor, so it will not cover a display of a different resolution or under DPI scaling. Fixing that
  and adding monitor choice is the same work.
- **macOS / Linux.** The `IPlatformWindow` seam is where they slot in. For macOS, use
  [UniWindowController](https://github.com/kirurobo/UniWindowController) (MIT) rather than hand-rolling
  — it needs a compiled Objective-C bundle, not P/Invoke. It does **not** support Linux; X11 would be
  custom, and Wayland has no standard protocol letting an ordinary client set itself always-on-top or
  click-through, so a widget like this largely cannot work there.

## Behaviour over other fullscreen apps

Borderless/windowed-fullscreen apps: the widget draws over them normally. True exclusive fullscreen
bypasses DWM entirely and nothing at this layer can appear over it — that requires hooking the other
app's present chain, which is what Steam and Discord overlays do. Note also that a topmost screen-sized
window can prevent other apps from entering exclusive fullscreen.
