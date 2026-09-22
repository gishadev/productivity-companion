using gishadev.companion.Village.POI;
using gishadev.companion.Village.Placeables;
using gishadev.companion.Village.Villagers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Village
{
    // Scene components are looked up leniently (inactive included) so the app runs without them.
    public sealed class VillageInstaller : IInstaller
    {
        private readonly IncrementalSettingsSO _incrementalSettings;
        private readonly VillageMasterSO _villageMaster;
        private readonly Transform _host;

        public VillageInstaller(
            IncrementalSettingsSO incrementalSettings,
            VillageMasterSO villageMaster,
            Transform host)
        {
            _incrementalSettings = incrementalSettings;
            _villageMaster = villageMaster;
            _host = host;
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
            // Without the asset the controller would throw every Tick; skip progression instead.
            if (_incrementalSettings == null)
            {
                Debug.LogError(
                    "[Incremental] No IncrementalSettingsSO assigned on CompanionLifetimeScope; progression is disabled.");
                return;
            }

            builder.RegisterInstance(_incrementalSettings);
            builder.Register(_ => Find<IncrementalView>(), Lifetime.Singleton);
            builder.RegisterEntryPoint<IncrementalController>().AsSelf();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            builder.RegisterComponentOnNewGameObject<IncrementalDebugHotkeys>(Lifetime.Singleton)
                .UnderTransform(_host);
            // Nothing resolves it otherwise.
            builder.RegisterBuildCallback(c => c.Resolve<IncrementalDebugHotkeys>());
#endif
        }

        private void InstallSimulation(IContainerBuilder builder)
        {
            if (_villageMaster == null)
            {
                Debug.LogError(
                    "[Village] No VillageMasterSO assigned on CompanionLifetimeScope; the village is disabled.");
                return;
            }

            builder.RegisterInstance(_villageMaster);
            builder.Register(_ => Find<VillageView>(), Lifetime.Singleton);
            builder.Register(_ => Find<PenaltyVillageFireView>(), Lifetime.Singleton);

            builder.Register<POIRegistry>(Lifetime.Singleton);

            // Before the population, so the world exists first.
            builder.RegisterEntryPoint<PlaceableController>().AsSelf();

            builder.Register<VillagersAIController>(Lifetime.Singleton);
            builder.Register<VillagersFactory>(Lifetime.Singleton);
            builder.RegisterEntryPoint<VillageController>().AsSelf();
        }

        private static T Find<T>() where T : Object =>
            Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
    }
}
