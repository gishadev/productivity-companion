using gishadev.companion.Focus;
using gishadev.companion.Pomodoro;
using gishadev.companion.Village;
using gishadev.companion.UI;
using gishadev.companion.Window;
using gishadev.tools.SavingSystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Infrastructure
{
    // Build is deferred until the root scope exists, so scene components are injected via
    // autoInjectGameObjects after Awake. Don't rely on Awake for injected fields.
    public sealed class CompanionLifetimeScope : LifetimeScope
    {
        private const string SaveFileName = "companion";

        [Tooltip("Balance data for the progression mechanic. Progression is disabled when unassigned.")]
        [SerializeField] private IncrementalSettingsSO incrementalSettings;

        [Tooltip("Villager prefab, sprites and placement. The village is disabled when unassigned.")]
        [SerializeField] private VillageMasterSO villageMaster;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISaverSystem>(_ => new FileSaverSystem(SaveFileName), Lifetime.Singleton);

            new WindowInstaller(transform).Install(builder);
            new FocusInstaller().Install(builder);
            new PomodoroInstaller().Install(builder);
            new UIInstaller().Install(builder);
            new VillageInstaller(incrementalSettings, villageMaster, transform).Install(builder);
        }
    }
}
