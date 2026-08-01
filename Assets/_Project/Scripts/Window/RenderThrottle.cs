using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace gishadev.companion.Window
{
    /// <summary>
    /// Drives <see cref="OnDemandRendering.renderFrameInterval"/> from a refcount so independent
    /// systems (an animation, a tooltip, a drag) can each hold a "keep rendering" lease without
    /// stomping each other. Rendering resumes the moment any lease is taken or focus returns.
    /// </summary>
    /// <remarks>
    /// renderFrameInterval throttles <em>rendering</em>, not the game loop: Update still runs at
    /// <c>Application.targetFrameRate</c>. That is deliberate — it's what keeps the click-through
    /// hit test responsive while the widget is unfocused and effectively not drawing.
    /// </remarks>
    public sealed class RenderThrottle
    {
        /// <summary>High enough to be a practical render pause without fully freezing the window.</summary>
        public const int DefaultIdleInterval = 15;

        private readonly HashSet<Lease> _leases = new HashSet<Lease>();

        private int _idleInterval = DefaultIdleInterval;
        private bool _focused = true;

        /// <summary>Render interval applied when unfocused and no lease is held.</summary>
        public int IdleInterval
        {
            get => _idleInterval;
            set
            {
                var clamped = value < 1 ? 1 : value;
                if (_idleInterval == clamped) return;
                _idleInterval = clamped;
                Apply();
            }
        }

        public int ActiveLeaseCount => _leases.Count;

        public bool IsRendering => _focused || _leases.Count > 0;

        /// <summary>
        /// Holds rendering at full rate until disposed. <paramref name="reason"/> is for debugging
        /// only — it is never used for lookup, so duplicate reasons are fine.
        /// </summary>
        public IDisposable AcquireLease(string reason = null)
        {
            var lease = new Lease(this, reason);
            _leases.Add(lease);
            Apply();
            return lease;
        }

        public void SetFocused(bool focused)
        {
            if (_focused == focused) return;
            _focused = focused;
            Apply();
        }

        /// <summary>Re-asserts the current interval, e.g. after something else has written to OnDemandRendering.</summary>
        public void Apply()
        {
            OnDemandRendering.renderFrameInterval = IsRendering ? 1 : _idleInterval;
        }

        private void Release(Lease lease)
        {
            if (!_leases.Remove(lease)) return;
            Apply();
        }

        private sealed class Lease : IDisposable
        {
            private readonly RenderThrottle _owner;
            private bool _disposed;

            internal Lease(RenderThrottle owner, string reason)
            {
                _owner = owner;
                Reason = reason;
            }

            internal string Reason { get; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _owner.Release(this);
            }

            public override string ToString() => $"RenderLease({Reason ?? "unnamed"})";
        }
    }
}
