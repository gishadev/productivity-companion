using gishadev.companion.Window.Native;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    // Install first: WindowController strips chrome in Initialize, and entry points run in registration order.
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
            builder.RegisterInstance(PlatformWindowFactory.CreateForegroundWindowProvider())
                .As<IForegroundWindowProvider>();
            builder.Register<WindowSettings>(Lifetime.Singleton);
            builder.Register<RenderThrottle>(Lifetime.Singleton);

            builder.RegisterEntryPoint<ClickThroughController>().AsSelf();
            builder.RegisterEntryPoint<TopmostWatchdog>().AsSelf();
            builder.RegisterEntryPoint<WindowController>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            builder.RegisterComponentOnNewGameObject<WindowDebugHotkeys>(Lifetime.Singleton)
                .UnderTransform(_host);
            // Nothing resolves it otherwise.
            builder.RegisterBuildCallback(c => c.Resolve<WindowDebugHotkeys>());
#endif
        }
    }
}
