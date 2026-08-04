using System;
using System.Collections.Generic;
using gishadev.companion.Window.Native;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using VContainer.Unity;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Makes the widget click-through over empty space and solid over interactive UI, re-evaluated
    /// every frame from the cursor position.
    /// </summary>
    /// <remarks>
    /// The cursor is read from Win32 rather than the Input System on purpose: a window carrying
    /// WS_EX_TRANSPARENT receives no mouse messages, so Unity's cursor position freezes the instant
    /// click-through engages and the controller could never turn it back off.
    ///
    /// The two loop phases are not interchangeable. <see cref="ITickable"/> maps to the old Update and
    /// must run before the UI input module dispatches; <see cref="ILateTickable"/> maps to LateUpdate,
    /// after the frame's raycasts are meaningful.
    /// </remarks>
    public sealed class ClickThroughController : ITickable, ILateTickable, IDisposable
    {
        /// <summary>Minimize state barely changes, so it is polled instead of queried every frame.</summary>
        private const float MinimizedPollInterval = 0.25f;

        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private readonly HashSet<Block> _blocks = new HashSet<Block>();

        private readonly IPlatformWindow _window;
        private readonly WindowSettings _settings;

        private PointerEventData _pointerData;
        private bool _minimized;
        private float _nextMinimizedPoll;
        private bool _applied;

        public ClickThroughController(IPlatformWindow window, WindowSettings settings)
        {
            _window = window;
            _settings = settings;
        }

        public bool IsBlocked => _blocks.Count > 0;

        /// <summary>
        /// Forces the window to keep accepting clicks regardless of the hit test, until disposed.
        /// Needed by anything that owns the cursor outside the uGUI raycast graph — an IMGUI overlay,
        /// or a drag that must survive the cursor crossing empty space.
        /// </summary>
        /// <remarks>
        /// Refcounted rather than a plain flag so independent systems can hold it at once without
        /// clearing each other's block. <paramref name="reason"/> is for debugging only.
        /// </remarks>
        public IDisposable AcquireBlock(string reason = null)
        {
            var block = new Block(this, reason);
            _blocks.Add(block);
            return block;
        }

        public void Dispose()
        {
            // Never leave the window stuck in click-through if this controller goes away.
            if (!_applied) return;
            _window?.SetClickThrough(false);
            _applied = false;
        }

        void ITickable.Tick() => SyncPointerToOsCursor();

        void ILateTickable.LateTick()
        {
            if (_window == null || !_window.IsAvailable) return;

            if (!_settings.ClickThrough || IsBlocked)
            {
                if (!_applied) return;
                _window.SetClickThrough(false);
                _applied = false;
                return;
            }

            if (IsMinimized())
            {
                // No point hit-testing a window nobody can see.
                if (!_applied) return;
                _window.SetClickThrough(false);
                _applied = false;
                return;
            }

            var shouldPassThrough = !IsCursorOverInteractiveContent();
            _window.SetClickThrough(shouldPassThrough);
            _applied = shouldPassThrough;
        }

        /// <summary>
        /// Overwrites the Input System's mouse position with the real OS cursor position.
        /// </summary>
        /// <remarks>
        /// Whenever mouse messages are being routed away from this window, Unity's own pointer
        /// position silently freezes at wherever it last was, and every uGUI interaction is then
        /// dispatched at that stale point — clicks land nowhere near the cursor. Two separate
        /// mechanisms cause it: WS_EX_TRANSPARENT while click-through is engaged, and, in ColorKey
        /// mode, the OS hit-transparency Windows applies to keyed pixels regardless of that flag.
        /// Feeding the position from Win32 keeps the pointer truthful under both.
        /// </remarks>
        private void SyncPointerToOsCursor()
        {
            if (_window == null || !_window.IsAvailable) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            if (!_window.TryGetCursorPosition(out var position)) return;
            if (((Vector2)mouse.position.ReadValue() - position).sqrMagnitude < 0.01f) return;

            InputState.Change(mouse.position, position);
        }

        private bool IsCursorOverInteractiveContent()
        {
            // Outside the client area entirely: nothing of ours can be under the cursor.
            if (!_window.TryGetCursorPosition(out var screenPosition)) return false;

            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            _pointerData ??= new PointerEventData(eventSystem);
            _pointerData.Reset();
            _pointerData.position = screenPosition;

            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerData, _raycastResults);
            return _raycastResults.Count > 0;
        }

        private bool IsMinimized()
        {
            if (Time.unscaledTime < _nextMinimizedPoll) return _minimized;

            _nextMinimizedPoll = Time.unscaledTime + MinimizedPollInterval;
            _minimized = _window.IsMinimized;
            return _minimized;
        }

        private sealed class Block : IDisposable
        {
            private readonly ClickThroughController _owner;
            private bool _disposed;

            internal Block(ClickThroughController owner, string reason)
            {
                _owner = owner;
                Reason = reason;
            }

            internal string Reason { get; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _owner._blocks.Remove(this);
            }

            public override string ToString() => $"ClickThroughBlock({Reason ?? "unnamed"})";
        }
    }
}
