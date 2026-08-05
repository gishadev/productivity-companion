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
    /// Click-through over empty space, solid over interactive UI, re-evaluated every frame from the
    /// cursor position. Tick must run before the UI input module dispatches; LateTick after the frame's
    /// raycasts are meaningful — the two phases are not interchangeable.
    /// </summary>
    public sealed class ClickThroughController : ITickable, ILateTickable, IDisposable
    {
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
        /// Refcounted so independent holders don't clear each other. Needed by anything owning the
        /// cursor outside the uGUI raycast graph. <paramref name="reason"/> is for debugging only.
        /// </summary>
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
        /// Overwrites the Input System's mouse position with the real OS cursor position. When mouse
        /// messages are routed away from this window, Unity's pointer freezes at its last value and
        /// every uGUI interaction dispatches at that stale point. Two causes: WS_EX_TRANSPARENT while
        /// click-through is engaged, and the OS hit-transparency applied to keyed pixels in ColorKey
        /// mode regardless of that flag.
        /// </summary>
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
