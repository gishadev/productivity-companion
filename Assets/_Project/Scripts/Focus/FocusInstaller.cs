using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Focus
{
    /// <summary>
    /// Registrations for focus tracking. Install after <c>WindowInstaller</c>, which owns the native
    /// foreground provider this consumes.
    /// </summary>
    public sealed class FocusInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<FocusRules>(Lifetime.Singleton);
            // AsSelf so the debug hotkeys can resolve it directly.
            builder.RegisterEntryPoint<FocusController>().AsSelf();
        }
    }
}
