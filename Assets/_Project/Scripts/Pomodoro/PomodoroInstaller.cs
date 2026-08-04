using VContainer;
using VContainer.Unity;

namespace gishadev.companion.Pomodoro
{
    /// <summary>
    /// Registrations for the Pomodoro system. Kept as an <see cref="IInstaller"/> so it can move to a
    /// different scope — or a runtime bootstrap, as the window system does — without changes.
    /// </summary>
    public sealed class PomodoroInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<PomodoroSettings>(Lifetime.Singleton);

            // RegisterEntryPoint only exposes the implemented interfaces, so AsSelf() is what lets the
            // presenter take the concrete timer. Registered ahead of the presenter so a phase boundary
            // resolved in the timer's Tick is rendered by the presenter in that same frame.
            builder.RegisterEntryPoint<PomodoroTimer>().AsSelf();
            builder.RegisterEntryPoint<PomodoroPresenter>();
        }
    }
}
