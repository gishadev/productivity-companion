using System;
using UnityEngine;
using VContainer.Unity;

namespace gishadev.companion.Village
{
    /// <summary>
    /// Owns the render texture the simulation camera draws into and the surface displays. Sized from the
    /// surface rect divided by the rig's upscale rather than from screen resolution: point filtering a
    /// small texture up to the widget is what keeps the pixel art crisp instead of resampled.
    /// </summary>
    public sealed class VillageRenderTarget : IStartable, ITickable, IDisposable
    {
        // The 2D renderer is configured with a depth stencil buffer, which masks and sorting rely on.
        private const int DepthBits = 24;

        private readonly VillageSceneRig _rig;
        private readonly VillageSurface _surface;

        private RenderTexture _texture;
        private Vector2Int _size;

        public VillageRenderTarget(VillageSceneRig rig, VillageSurface surface)
        {
            _rig = rig;
            _surface = surface;
        }

        public RenderTexture Texture => _texture;

        void IStartable.Start() => Sync();

        void ITickable.Tick() => Sync();

        public void Dispose() => Release();

        // Polled rather than driven by events: the surface is shown and hidden by
        // DynamicSimulationWindowView toggling the GameObject, and resized by the canvas scaler on a
        // resolution change. Neither raises anything to subscribe to. Public so the editor preview on
        // SimulationSceneRig can drive the same path outside play mode.
        public void Sync()
        {
            if (_rig == null || !_rig.IsValid || _surface == null) return;

            var visible = _surface.isActiveAndEnabled;
            if (_rig.Camera.enabled != visible) _rig.Camera.enabled = visible;
            if (!visible) return;

            var size = DesiredSize();
            if (_texture != null && size == _size) return;

            Allocate(size);
        }

        private Vector2Int DesiredSize()
        {
            var rect = _surface.Rect.rect;
            var upscale = _rig.Upscale;

            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(rect.width / upscale)),
                Mathf.Max(1, Mathf.RoundToInt(rect.height / upscale)));
        }

        private void Allocate(Vector2Int size)
        {
            Release();

            _size = size;
            _texture = new RenderTexture(size.x, size.y, DepthBits, RenderTextureFormat.ARGB32)
            {
                name = "Simulation",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1,
                // Keeps the editor preview's texture from being serialized into the scene or the prefab.
                hideFlags = HideFlags.HideAndDontSave
            };
            _texture.Create();

            _rig.Camera.targetTexture = _texture;
            // Half the rendered height in world units, so one texel always covers one pixel of art.
            _rig.Camera.orthographicSize = size.y * 0.5f / _rig.PixelsPerUnit;

            _surface.Image.texture = _texture;
        }

        private void Release()
        {
            if (_texture == null) return;

            if (_rig != null && _rig.Camera != null && _rig.Camera.targetTexture == _texture)
                _rig.Camera.targetTexture = null;

            if (_surface != null && _surface.Image != null && _surface.Image.texture == _texture)
                _surface.Image.texture = null;

            _texture.Release();
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_texture);
            else
                UnityEngine.Object.DestroyImmediate(_texture);

            _texture = null;
            _size = Vector2Int.zero;
        }
    }
}
