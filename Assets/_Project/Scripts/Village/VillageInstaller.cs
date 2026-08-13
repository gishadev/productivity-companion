using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Registrations for the simulation world, the surface it is displayed on, and the progression that
    /// drives it. Scene components are looked up leniently rather than through
    /// RegisterComponentInHierarchy, which throws when the object is absent: the rest of the app has to
    /// keep running while the simulation scene is built out. Inactive objects are included because the
    /// surface starts disabled when the window was left hidden.
    /// </summary>
    public sealed class VillageInstaller : IInstaller
    {
        private readonly IncrementalSettingsSO _incrementalSettings;

        public VillageInstaller(IncrementalSettingsSO incrementalSettings)
        {
            _incrementalSettings = incrementalSettings;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.Register(_ => Find<VillageSceneRig>(), Lifetime.Singleton);
            builder.Register(_ => Find<VillageSurface>(), Lifetime.Singleton);

            builder.Register<VillageViewport>(Lifetime.Singleton);
            builder.RegisterEntryPoint<VillageRenderTarget>().AsSelf();

            InstallIncremental(builder);
        }

        private void InstallIncremental(IContainerBuilder builder)
        {
            // Registering the controller anyway would resolve a null asset and then throw from Tick on
            // every frame. Leaving progression out keeps the failure to one line and the widget usable.
            if (_incrementalSettings == null)
            {
                Debug.LogError(
                    "[Incremental] No IncrementalSettingsSO assigned on CompanionLifetimeScope; progression is disabled.");
                return;
            }

            builder.RegisterInstance(_incrementalSettings);
            builder.Register(_ => Find<IncrementalView>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<IncrementalController>().AsSelf();
        }

        private static T Find<T>() where T : Object =>
            Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
    }
}
