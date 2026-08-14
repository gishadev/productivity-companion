using gishadev.companion.Village.Villagers;
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
        private readonly VillageMasterSO _villageMaster;

        public VillageInstaller(IncrementalSettingsSO incrementalSettings, VillageMasterSO villageMaster)
        {
            _incrementalSettings = incrementalSettings;
            _villageMaster = villageMaster;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.Register(_ => Find<VillageSceneRig>(), Lifetime.Singleton);
            builder.Register(_ => Find<VillageSurface>(), Lifetime.Singleton);

            builder.Register<VillageViewport>(Lifetime.Singleton);
            builder.RegisterEntryPoint<VillageRenderTarget>().AsSelf();

            InstallIncremental(builder);
            InstallSimulation(builder);
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

        private void InstallSimulation(IContainerBuilder builder)
        {
            // Same reasoning as above: without the data there is nothing to build, and registering the
            // controller anyway would only move the failure into Tick.
            if (_villageMaster == null)
            {
                Debug.LogError(
                    "[Village] No VillageMasterSO assigned on CompanionLifetimeScope; the village is disabled.");
                return;
            }

            builder.RegisterInstance(_villageMaster);
            builder.Register(_ => Find<VillageView>(), Lifetime.Singleton);

            builder.Register<VillagersAIController>(Lifetime.Singleton);
            builder.Register<VillagersFactory>(Lifetime.Singleton);
            builder.RegisterEntryPoint<VillageController>().AsSelf();
        }

        private static T Find<T>() where T : Object =>
            Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
    }
}
