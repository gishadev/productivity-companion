using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Simulation
{
    /// <summary>
    /// Registrations for the simulation world and the surface it is displayed on. Both scene components
    /// are looked up leniently rather than through RegisterComponentInHierarchy, which throws when the
    /// object is absent: the rest of the app has to keep running while the simulation scene is built out.
    /// Inactive objects are included because the surface starts disabled when the window was left hidden.
    /// </summary>
    public sealed class SimulationInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register(_ => Find<SimulationSceneRig>(), Lifetime.Singleton);
            builder.Register(_ => Find<SimulationSurface>(), Lifetime.Singleton);

            builder.Register<SimulationViewport>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SimulationRenderTarget>().AsSelf();
        }

        private static T Find<T>() where T : Object =>
            Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
    }
}
