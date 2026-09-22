using VContainer;
using VContainer.Unity;

namespace gishadev.companion.UI
{
    public sealed class UIInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<VillageWindowSettings>(Lifetime.Singleton);
        }
    }
}
