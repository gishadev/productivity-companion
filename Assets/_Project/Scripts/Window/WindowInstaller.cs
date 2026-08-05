using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Registrations for the window system. Install before other subsystems: WindowController strips
    /// the window chrome in its IInitializable phase, and entry points initialize in registration order.
    /// </summary>
    public sealed class WindowInstaller : IInstaller
    {
        private readonly Transform _host;

        public WindowInstaller(Transform host)
        {
            _host = host;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(PlatformWindowFactory.Create()).As<IPlatformWindow>();
            builder.Register<WindowSettings>(Lifetime.Singleton);
            builder.Register<RenderThrottle>(Lifetime.Singleton);

            builder.RegisterEntryPoint<ClickThroughController>().AsSelf();
            builder.RegisterEntryPoint<TopmostWatchdog>().AsSelf();
            builder.RegisterEntryPoint<WindowController>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            builder.RegisterComponentOnNewGameObject<WindowDebugHotkeys>(Lifetime.Singleton)
                .UnderTransform(_host);
            // Nothing resolves it otherwise: it drives itself off Update/OnGUI, not an entry point.
            builder.RegisterBuildCallback(c => c.Resolve<WindowDebugHotkeys>());
#endif
        }
    }
}
