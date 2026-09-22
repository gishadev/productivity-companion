using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Focus
{
    public sealed class FocusInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<FocusRules>(Lifetime.Singleton);
            builder.RegisterEntryPoint<FocusController>().AsSelf();
        }
    }
}
