using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace gishadev.companion.Window
{
    // Throttles rendering only; Update keeps running, which keeps the click-through hit test responsive.
    public sealed class RenderThrottle
    {
        public const int DefaultIdleInterval = 15;

        private readonly HashSet<Lease> _leases = new HashSet<Lease>();

        private int _idleInterval = DefaultIdleInterval;
        private bool _focused = true;

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

        // Refcounted: full-rate rendering until disposed.
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
