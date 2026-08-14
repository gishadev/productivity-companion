using UnityEngine;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Depth from world Y: lower on screen draws in front. Everything that sorts this way — moving
    /// villagers and static props alike — goes through here, because two different formulas would
    /// interleave wrongly against each other and the bug would only show on overlap.
    /// </summary>
    public static class YSorting
    {
        /// <summary>
        /// Order steps per world unit. Matched to the art's 100 pixels per unit, so one step is one
        /// rendered pixel — finer than that cannot be seen, coarser makes near-level sprites flicker
        /// past each other. A const rather than a field on VillageMasterSO because
        /// <see cref="StaticYSort"/> is a plain scene component with nothing injected into it, and both
        /// paths agreeing matters more than being able to tune it.
        /// </summary>
        public const int OrderPerUnit = 100;

        // sortingOrder is backed by a short; a prop parked far from the origin would otherwise wrap
        // around and jump in front of everything.
        private const int MaxOrder = 32000;

        public static int OrderFor(float worldY) =>
            Mathf.Clamp(Mathf.RoundToInt(-worldY * OrderPerUnit), -MaxOrder, MaxOrder);
    }
}
