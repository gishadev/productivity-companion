using gishadev.companion.Pomodoro;
using gishadev.companion.Window;
using VContainer;
using VContainer.Unity;

namespace gishadev.companion
{
    /// <summary>
    /// Scene scope for the companion's own features. Lives in <c>Game.unity</c> and chains under the
    /// window scope, so both the window services and the project root's registrations resolve here.
    /// </summary>
    /// <remarks>
    /// Plain <see cref="LifetimeScope"/> rather than <c>gishadev.tools.AutoInjectLifetimeScope</c>: the
    /// root scope already sweeps the scene for [Inject] members, and nothing registered here uses them —
    /// scene components are pulled in with RegisterComponentInHierarchy and delivered to entry points as
    /// constructor dependencies instead. A second sweep would be pure cost.
    /// </remarks>
    public sealed class CompanionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<PomodoroWidgetView>();

            new PomodoroInstaller().Install(builder);

            // Scene components cannot take constructor dependencies; this pushes them in instead.
            builder.RegisterEntryPoint<WindowSceneBinder>();
        }

        /// <summary>
        /// Chains this scope under <see cref="WindowLifetimeScope"/> rather than letting VContainer fall
        /// back to the project root.
        /// </summary>
        /// <remarks>
        /// Without this the two are siblings: neither declares a parent, so both land on the root scope
        /// through <c>GetRuntimeParent</c>'s last-resort branch, and nothing registered here can resolve
        /// <see cref="RenderThrottle"/> or <c>IPlatformWindow</c>.
        ///
        /// The window scope is built at BeforeSceneLoad, so it is always present by the time this scene
        /// scope awakes. Should its bootstrap ever fail, returning null falls through to the root and the
        /// first window-service resolve throws at build time instead of silently no-opping.
        /// </remarks>
        protected override LifetimeScope FindParent() => Find<WindowLifetimeScope>();
    }
}
