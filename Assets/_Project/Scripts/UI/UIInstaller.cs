using VContainer;
using VContainer.Unity;

namespace gishadev.companion.UI
{
    /// <summary>Registrations for the UI layer.</summary>
    public sealed class UIInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<SimulationWindowSettings>(Lifetime.Singleton);
        }
    }
}
