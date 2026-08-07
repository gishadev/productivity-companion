using gishadev.tools.SavingSystem;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Pomodoro
{
    /// <summary>Registrations for the Pomodoro system.</summary>
    public sealed class PomodoroInstaller : IInstaller
    {
        private const string SaveFileName = "companion";

        public void Install(IContainerBuilder builder)
        {
            builder.Register<ISaverSystem>(_ => new FileSaverSystem(SaveFileName), Lifetime.Singleton);
            builder.Register<PomodoroSettings>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<PomodoroWidgetView>();

            builder.RegisterEntryPoint<PomodoroTimer>().AsSelf();
            builder.RegisterEntryPoint<PomodoroPresenter>();
        }
    }
}
