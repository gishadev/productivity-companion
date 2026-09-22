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
    // Tick must run before the UI input module dispatches; LateTick after raycasts. Not interchangeable.
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

        // Refcounted: keeps the window clickable until disposed.
        public IDisposable AcquireBlock(string reason = null)
        {
            var block = new Block(this, reason);
            _blocks.Add(block);
            return block;
        }

        public void Dispose()
        {
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

        // Unity's pointer freezes when mouse messages are routed away (WS_EX_TRANSPARENT, or keyed pixels
        // in ColorKey mode), so feed it the real OS cursor.
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
