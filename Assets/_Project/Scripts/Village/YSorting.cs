using UnityEngine;

namespace gishadev.companion.Village
{
    // Lower on screen draws in front. Everything that Y-sorts must use this, or overlaps interleave wrongly.
    public static class YSorting
    {
        // One step per rendered pixel at 100 PPU.
        public const int OrderPerUnit = 100;

        // sortingOrder is a short.
        private const int MaxOrder = 32000;

        public static int OrderFor(float worldY) =>
            Mathf.Clamp(Mathf.RoundToInt(-worldY * OrderPerUnit), -MaxOrder, MaxOrder);
    }
}
