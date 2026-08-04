using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Owns the window system and brings itself up before the first scene loads, with no scene or
    /// prefab authoring required — the widget must be reshaped as early as possible, and hand-editing
    /// scene YAML to place a bootstrap object is needlessly fragile.
    /// </summary>
    /// <remarks>
    /// App-lifetime on purpose. Registering these services in a scene scope instead would tie them to
    /// that scene's container: <see cref="WindowSettings"/> and <see cref="RenderThrottle"/> would be
    /// disposed on scene unload while the native window kept its old state.
    ///
    /// Everything registered here is a plain C# entry point rather than a MonoBehaviour, which is what
    /// lets dependencies arrive by constructor. Attribute injection is not an option: the project's root
    /// scope (<c>gishadev.tools.AutoInjectLifetimeScope</c>) re-injects every [Inject]-bearing
    /// MonoBehaviour found by FindObjectsByType on each scene load — including DontDestroyOnLoad objects
    /// like these would be — and since these services live in this scope rather than the root container,
    /// that pass would log an injection failure for each one, every single scene load.
    ///
    /// One global side effect to be aware of: resolving a parent here pulls VContainer's root scope
    /// creation forward. VContainerSettings normally builds the root in OnFirstSceneLoaded, but because
    /// this runs at BeforeSceneLoad the root — and with it gishadev.tools' scene-wide inject pass — is
    /// built before any scene exists. That first pass therefore reports zero components, and the real
    /// injection happens on the sceneLoaded callback that follows.
    /// </remarks>
    public sealed class WindowLifetimeScope : LifetimeScope
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Find<WindowLifetimeScope>() != null) return;

            // Built inactive so Configure and every entry point run after DontDestroyOnLoad is applied,
            // not midway through AddComponent.
            var host = new GameObject(nameof(WindowLifetimeScope));
            host.SetActive(false);
            host.AddComponent<WindowLifetimeScope>();
            DontDestroyOnLoad(host);
            host.SetActive(true);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // The factory is the one place that decides whether native calls are live.
            builder.RegisterInstance(PlatformWindowFactory.Create()).As<IPlatformWindow>();
            builder.Register<WindowSettings>(Lifetime.Singleton);
            builder.Register<RenderThrottle>(Lifetime.Singleton);

            // AsSelf() because RegisterEntryPoint exposes only the implemented interfaces, and both of
            // these are taken as concrete constructor dependencies elsewhere.
            builder.RegisterEntryPoint<TopmostWatchdog>().AsSelf();
            builder.RegisterEntryPoint<ClickThroughController>().AsSelf();
            builder.RegisterEntryPoint<WindowController>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            RegisterDebugHotkeys(builder);
#endif
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <remarks>
        /// The one component that cannot stop being a MonoBehaviour: it draws through OnGUI, which has
        /// no entry point equivalent. It is created by hand rather than with
        /// RegisterComponentOnNewGameObject because that injects through [Inject] attributes, which is
        /// exactly the scene-load error spam described in the class remarks.
        /// </remarks>
        private void RegisterDebugHotkeys(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                var host = new GameObject(nameof(WindowDebugHotkeys));
                host.SetActive(false);
                host.transform.SetParent(transform, false);

                host.AddComponent<WindowDebugHotkeys>().Initialize(
                    container.Resolve<WindowSettings>(),
                    container.Resolve<RenderThrottle>(),
                    container.Resolve<IPlatformWindow>(),
                    container.Resolve<ClickThroughController>());

                host.SetActive(true);
            });
        }
#endif
    }
}
