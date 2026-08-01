using gishadev.companion.Window.Native;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Registrations for the window system. Kept as an <see cref="IInstaller"/> so it can be used by
    /// the runtime bootstrap or dropped into a scene LifetimeScope later without changes.
    /// </summary>
    public sealed class WindowInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // The factory is the one place that decides whether native calls are live.
            builder.RegisterInstance(PlatformWindowFactory.Create()).As<IPlatformWindow>();
            builder.Register<WindowSettings>(Lifetime.Singleton);
            builder.Register<RenderThrottle>(Lifetime.Singleton);
        }
    }
}
