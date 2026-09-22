using System.Collections.Generic;
using UnityEngine;

namespace gishadev.companion.Village.POI
{
    // Scanned lazily once; call Refresh after placeables change.
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

        public JobPOI TryClaimJob() => Claim(Jobs);

        public RelaxPOI TryClaimRelax() => Claim(RelaxSpots);

        public void Refresh()
        {
            _scanned = false;
            EnsureScanned();
        }

        private static T Claim<T>(IReadOnlyList<T> candidates) where T : VillagePOI
        {
            // In order, not random: keeps a villager returning to the same post.
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

            // Inactive included: the village can be built while the widget is hidden.
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
