using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Pomodoro
{
    /// <summary>Registrations for the Pomodoro system.</summary>
    public sealed class PomodoroInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<PomodoroSettings>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<PomodoroWidgetView>();

            builder.RegisterEntryPoint<PomodoroTimer>().AsSelf();
            builder.RegisterEntryPoint<PomodoroPresenter>();
        }
    }
}
