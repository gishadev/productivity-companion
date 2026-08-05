using System;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    [Serializable]
    public class GraphicTarget
    {
        public Graphic graphic;
        public ColorBlock colors = ColorBlock.defaultColorBlock;
        public float transitionDuration = 0.1f;
    }
}