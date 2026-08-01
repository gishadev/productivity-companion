using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Brings the window system up before the first scene loads, with no scene or prefab authoring
    /// required — the widget must be reshaped as early as possible, and hand-editing scene YAML to
    /// place a bootstrap object is needlessly fragile.
    /// </summary>
    /// <remarks>
    /// Dependencies are resolved from the scope and handed over through explicit Initialize calls
    /// rather than [Inject] attributes. The project's root scope
    /// (<c>gishadev.tools.AutoInjectLifetimeScope</c>) re-injects every [Inject]-bearing MonoBehaviour
    /// found by FindObjectsByType on each scene load — which includes DontDestroyOnLoad objects like
    /// this one. Since the window services live in this child scope and not the root container, that
    /// pass would log an injection failure for each component every single scene load. Explicit
    /// initialization sidesteps it while keeping the registrations in VContainer.
    /// </remarks>
    public static class WindowBootstrap
    {
        private static GameObject _host;

        /// <summary>The scope owning the window services; null until bootstrap runs.</summary>
        public static LifetimeScope Scope { get; private set; }

        // Scene components (a drag handle, a future settings panel) are authored in the scene and so
        // cannot be initialized by this bootstrap. They also cannot be injected by the project's root
        // scope, which does not know these registrations — see the class remarks. These accessors are
        // the seam for that case; anything created by the bootstrap gets its dependencies passed in.
        public static IPlatformWindow Window { get; private set; }
        public static WindowSettings Settings { get; private set; }
        public static RenderThrottle RenderThrottle { get; private set; }
        public static ClickThroughController ClickThrough { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_host != null) return;

            Scope = LifetimeScope.Create(new WindowInstaller(), "WindowLifetimeScope");
            Object.DontDestroyOnLoad(Scope.gameObject);

            var window = Window = Scope.Container.Resolve<IPlatformWindow>();
            var settings = Settings = Scope.Container.Resolve<WindowSettings>();
            var renderThrottle = RenderThrottle = Scope.Container.Resolve<RenderThrottle>();

            // Built inactive so each Awake runs after initialization, not during AddComponent.
            _host = new GameObject("WindowSystem");
            _host.SetActive(false);

            var watchdog = _host.AddComponent<TopmostWatchdog>();
            watchdog.Initialize(window);

            var clickThrough = ClickThrough = _host.AddComponent<ClickThroughController>();
            clickThrough.Initialize(window, settings);

            var controller = _host.AddComponent<WindowController>();
            controller.Initialize(window, settings, renderThrottle);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _host.AddComponent<WindowDebugHotkeys>().Initialize(settings, renderThrottle, window);
#endif

            Object.DontDestroyOnLoad(_host);
            _host.SetActive(true);
        }
    }
}
