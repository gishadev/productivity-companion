using gishadev.companion.Pomodoro;
using gishadev.companion.UI;
using gishadev.companion.Window;
using gishadev.tools.SavingSystem;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Infrastructure
{
    /// <summary>
    /// Scene components are injected through the inherited autoInjectGameObjects list, which VContainer
    /// runs when the container is actually built. This scope's build is deferred until the root scope
    /// exists, so anything keyed off Awake would run against a null Container.
    /// </summary>
    public sealed class CompanionLifetimeScope : LifetimeScope
    {
        private const string SaveFileName = "companion";

        protected override void Configure(IContainerBuilder builder)
        {
            // Shared by every subsystem that persists anything, so it sits above the installers.
            builder.Register<ISaverSystem>(_ => new FileSaverSystem(SaveFileName), Lifetime.Singleton);

            new WindowInstaller(transform).Install(builder);
            new PomodoroInstaller().Install(builder);
            new UIInstaller().Install(builder);
        }
    }
}
