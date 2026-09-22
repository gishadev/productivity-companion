using System;
using System.Collections.Generic;

namespace gishadev.companion.SavingLoading
{
    [Serializable]
    public sealed class FocusRulesData
    {
        public List<string> productive = new();
        public List<string> unproductive = new();
    }
}
