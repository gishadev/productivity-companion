using UnityEngine;

namespace gishadev.companion.Simulation
{
    /// <summary>
    /// Handle onto the simulation world's scene objects. The world sits at a fixed offset from the
    /// origin and never moves: the widget it is displayed in gets dragged and re-docked constantly, so
    /// anything anchored to that rect would have its cell and path coordinates shifted underneath it.
    /// </summary>
    [ExecuteAlways]
    public sealed class SimulationSceneRig : MonoBehaviour
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

            // Camera.main is the transparency target WindowController rewrites the clear colour on, so
            // a second MainCamera would leave which window is transparent up to scene ordering.
            if (simulationCamera.CompareTag("MainCamera"))
                Debug.LogError(
                    $"[Simulation] {name}: the simulation camera is tagged MainCamera, which window transparency claims.",
                    this);

            simulationCamera.orthographic = true;
            simulationCamera.clearFlags = CameraClearFlags.SolidColor;
            simulationCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }

#if UNITY_EDITOR
        private SimulationRenderTarget _preview;
        private SimulationSurface _previewSurface;

        // The runtime target is a VContainer entry point, which only exists in play mode. Driving the
        // same path by hand here is what lets the widget show the world while it is being authored.
        private void Update()
        {
            if (Application.isPlaying) return;

            if (_previewSurface == null)
                _previewSurface = FindAnyObjectByType<SimulationSurface>(FindObjectsInactive.Include);
            if (_previewSurface == null) return;

            _preview ??= new SimulationRenderTarget(this, _previewSurface);
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
