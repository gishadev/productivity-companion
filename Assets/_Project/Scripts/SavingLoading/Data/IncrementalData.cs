using System;

namespace gishadev.companion.SavingLoading
{
    // Saved as float: only the accumulation needs double precision.
    [Serializable]
    public sealed class IncrementalData
    {
        public int level;
        public float progress;
    }
}
