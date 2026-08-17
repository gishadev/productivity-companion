using System.Collections.Generic;
using UnityEngine;

namespace gishadev.companion.Village.POI
{
    /// <summary>
    /// Finds the POIs in the scene and hands out claims. Scanned once and lazily rather than on a
    /// registration callback: POIs are static scene content authored on the village prefab, and the
    /// lenient one-shot lookup matches how every other village scene reference is resolved.
    /// </summary>
    public sealed class POIRegistry
    {
        private readonly List<JobPOI> _jobs = new List<JobPOI>();
        private readonly List<RelaxPOI> _relax = new List<RelaxPOI>();

        private bool _scanned;

        public IReadOnlyList<JobPOI> Jobs
        {
            get
            {
                EnsureScanned();
                return _jobs;
            }
        }

        public IReadOnlyList<RelaxPOI> RelaxSpots
        {
            get
            {
                EnsureScanned();
                return _relax;
            }
        }

        /// <summary>Null when every post is taken, or when the scene has none — both are normal.</summary>
        public JobPOI TryClaimJob() => Claim(Jobs);

        public RelaxPOI TryClaimRelax() => Claim(RelaxSpots);

        /// <summary>Re-scan, for when POIs stop being fixed scene content.</summary>
        public void Refresh()
        {
            _scanned = false;
            EnsureScanned();
        }

        private static T Claim<T>(IReadOnlyList<T> candidates) where T : VillagePOI
        {
            // Scanned in order rather than at random: with a handful of POIs the bias is invisible, and
            // a stable choice keeps the same villager returning to the same post across a session.
            for (var i = 0; i < candidates.Count; i++)
            {
                var poi = candidates[i];
                if (poi == null || !poi.isActiveAndEnabled) continue;

                if (poi.TryClaim()) return poi;
            }

            return null;
        }

        private void EnsureScanned()
        {
            if (_scanned) return;
            _scanned = true;

            _jobs.Clear();
            _relax.Clear();

            // Inactive included, for the same reason the installer does it: the village can be built
            // while the widget is hidden.
            var found = Object.FindObjectsByType<VillagePOI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < found.Length; i++)
            {
                switch (found[i])
                {
                    case JobPOI job:
                        _jobs.Add(job);
                        break;

                    case RelaxPOI relax:
                        _relax.Add(relax);
                        break;
                }
            }
        }
    }
}
