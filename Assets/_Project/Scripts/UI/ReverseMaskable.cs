using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace gishadev.companion.UI
{
    // Inverts the stencil test so a ReverseMask punches a hole. Must sit below the Graphic in
    // component order (runs after its IMaterialModifier pass).
    [AddComponentMenu("UI/Reverse Maskable", 15)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ReverseMaskable : MonoBehaviour, IMaterialModifier
    {
        private Graphic _graphic;
        private Material _invertedMaterial;

        private Graphic Graphic => _graphic != null ? _graphic : _graphic = GetComponent<Graphic>();

        private void OnEnable()
        {
            if (Graphic == null)
            {
                Debug.LogError($"[UI] {name}: ReverseMaskable needs a Graphic (Image, RawImage, TMP text) on the same object.", this);
                return;
            }

            Graphic.SetMaterialDirty();
        }

        private void OnDisable()
        {
            StencilMaterial.Remove(_invertedMaterial);
            _invertedMaterial = null;
            if (Graphic != null) Graphic.SetMaterialDirty();
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled) return baseMaterial;

            var root = MaskUtilities.FindRootSortOverrideCanvas(transform);
            var stencilId = (1 << MaskUtilities.GetStencilDepth(transform, root)) - 1;
            if (stencilId <= 0) return baseMaterial;

            var inverted = StencilMaterial.Add(baseMaterial, stencilId, StencilOp.Keep, CompareFunction.NotEqual,
                ColorWriteMask.All, stencilId, 0);

            StencilMaterial.Remove(_invertedMaterial);
            _invertedMaterial = inverted;
            return _invertedMaterial;
        }
    }
}
