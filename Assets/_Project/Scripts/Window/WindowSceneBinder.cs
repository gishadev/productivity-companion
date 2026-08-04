using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Hands the window services to the scene-authored components that need them, replacing the static
    /// accessors the bootstrap used to expose.
    /// </summary>
    /// <remarks>
    /// Scene MonoBehaviours cannot take constructor dependencies, and attribute injection is off the
    /// table for anything the root scope's scene-wide pass would find — see
    /// <see cref="WindowLifetimeScope"/>. A binder is the remaining option: it runs from the scene scope,
    /// so it resolves the window services from the parent container and pushes them in explicitly.
    ///
    /// A search rather than <c>RegisterComponentInHierarchy</c> because that resolves a single component,
    /// and a layout can legitimately carry more than one drag handle. Bound once at scene start; handles
    /// spawned later would need initializing by whatever spawns them.
    /// </remarks>
    public sealed class WindowSceneBinder : IStartable
    {
        private readonly ClickThroughController _clickThrough;
        private readonly RenderThrottle _renderThrottle;

        public WindowSceneBinder(ClickThroughController clickThrough, RenderThrottle renderThrottle)
        {
            _clickThrough = clickThrough;
            _renderThrottle = renderThrottle;
        }

        void IStartable.Start()
        {
            var handles = Object.FindObjectsByType<WidgetDragHandle>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var handle in handles)
                handle.Initialize(_clickThrough, _renderThrottle);
        }
    }
}
