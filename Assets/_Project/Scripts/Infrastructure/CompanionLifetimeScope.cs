using gishadev.companion.Pomodoro;
using gishadev.companion.Window;
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
        protected override void Configure(IContainerBuilder builder)
        {
            new WindowInstaller(transform).Install(builder);
            new PomodoroInstaller().Install(builder);
        }
    }
}
