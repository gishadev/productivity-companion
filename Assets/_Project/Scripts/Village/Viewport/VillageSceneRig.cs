using UnityEngine;

namespace gishadev.companion.Village
{
    // The world sits at a fixed offset and never moves, so dragging the widget can't shift world coordinates.
    [ExecuteAlways]
    public sealed class VillageSceneRig : MonoBehaviour
    {
        [Tooltip("Renders the simulation into the render target. Must not be tagged MainCamera.")]
        [SerializeField] private Camera simulationCamera;

        [Tooltip("Canvas units per rendered pixel. Higher means a chunkier, crisper upscale.")]
        [Min(1)] [SerializeField] private int upscale = 3;

        [Tooltip("Pixels per unit of the simulation's art. Fixes how much world one rendered pixel covers.")]
        [Min(1f)] [SerializeField] private float pixelsPerUnit = 32f;

        public Camera Camera => simulationCamera;

        public int Upscale => Mathf.Max(1, upscale);

        public float PixelsPerUnit => Mathf.Max(1f, pixelsPerUnit);

        public bool IsValid => simulationCamera != null;

        private void Awake()
        {
            if (simulationCamera == null)
            {
                Debug.LogError($"[Simulation] {name}: no simulation camera assigned; the world will not render.", this);
                return;
            }

            // WindowController rewrites Camera.main's clear colour for transparency; a second MainCamera breaks that.
            if (simulationCamera.CompareTag("MainCamera"))
                Debug.LogError(
                    $"[Simulation] {name}: the simulation camera is tagged MainCamera, which window transparency claims.",
                    this);

            simulationCamera.orthographic = true;
            simulationCamera.clearFlags = CameraClearFlags.SolidColor;
            simulationCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }

#if UNITY_EDITOR
        private VillageRenderTarget _preview;
        private VillageSurface _previewSurface;

        // Drives the render target outside play mode for authoring.
        private void Update()
        {
            if (Application.isPlaying) return;

            if (_previewSurface == null)
                _previewSurface = FindAnyObjectByType<VillageSurface>(FindObjectsInactive.Include);
            if (_previewSurface == null) return;

            _preview ??= new VillageRenderTarget(this, _previewSurface);
            _preview.Sync();
        }

        private void OnDisable()
        {
            if (Application.isPlaying) return;

            _preview?.Dispose();
            _preview = null;
            _previewSurface = null;
        }
#endif
    }
}
