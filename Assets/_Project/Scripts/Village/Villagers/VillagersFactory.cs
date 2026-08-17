using System.Collections.Generic;
using UnityEngine;

namespace gishadev.companion.Village.Villagers
{
    /// <summary>
    /// Owns the villager population. Reconciles to a target count rather than reacting to individual
    /// level-ups, which is what lets a restored save materialise its whole village in one call.
    /// </summary>
    public sealed class VillagersFactory
    {
        private readonly VillageMasterSO _master;
        private readonly VillageView _view;
        private readonly VillagersAIController _ai;

        private readonly List<Villager> _villagers = new List<Villager>();

        private bool _setupChecked;

        public VillagersFactory(VillageMasterSO master, VillageView view, VillagersAIController ai)
        {
            _master = master;
            _view = view;
            _ai = ai;
        }

        public int Count => _villagers.Count;

        /// <summary>
        /// Idempotent: spawns or despawns until the population matches. A tick can cross several levels
        /// and fire several events, so this has to be safe to call with the same number repeatedly.
        /// </summary>
        public void SetTarget(int count)
        {
            if (_view == null || _master == null || _master.VillagerPrefab == null) return;

            var target = Mathf.Clamp(count, 0, _master.MaxVillagers);

            while (_villagers.Count > target) DespawnLast();
            while (_villagers.Count < target) Spawn();
        }

        private void Spawn()
        {
            var villager = Object.Instantiate(_master.VillagerPrefab, PickPosition(), Quaternion.identity,
                _view.VillagersRoot);

            villager.SetVariant(_master.RandomVariant());

            CheckSetupOnce(villager);

            _villagers.Add(villager);
            _ai.Add(villager);
        }

        private void DespawnLast()
        {
            var last = _villagers.Count - 1;
            var villager = _villagers[last];
            _villagers.RemoveAt(last);

            if (villager == null) return;

            _ai.Remove(villager);
            Object.Destroy(villager.gameObject);
        }

        // Rejection sampling with a bounded retry: once the area is full every candidate is too close,
        // and accepting the last one is what stops that becoming an infinite loop. The spacing is a
        // preference, not a guarantee.
        private Vector3 PickPosition()
        {
            var area = _view.WalkableArea;
            if (area == null) return _view.VillagersRoot.position;

            var spacing = _master.MinSpacing;
            var attempts = _master.PlacementAttempts;

            var candidate = area.RandomPoint();
            for (var i = 1; i < attempts && spacing > 0f && !IsClear(candidate, spacing); i++)
                candidate = area.RandomPoint();

            return candidate;
        }

        private bool IsClear(Vector2 world, float spacing)
        {
            var sqrSpacing = spacing * spacing;

            for (var i = 0; i < _villagers.Count; i++)
            {
                var other = _villagers[i];
                if (other == null) continue;

                // Compared in 2D: the art is flat, and z only carries sorting.
                if (((Vector2)other.transform.position - world).sqrMagnitude < sqrSpacing) return false;
            }

            return true;
        }

        // Both of these produce a village that looks broken rather than one that throws, so they are
        // worth naming explicitly. Checked once for the whole run, not once per villager: every villager
        // comes off the same prefab, and the scene lookup is not cheap enough to repeat 200 times
        // through a restore.
        private void CheckSetupOnce(Villager villager)
        {
            if (_setupChecked) return;
            _setupChecked = true;

            if (_view.WalkableArea == null)
                Debug.LogWarning(
                    "[Village] No WalkableArea assigned on VillageView; villagers will all stack on the " +
                    "villagers root.", _view);

            var rig = Object.FindAnyObjectByType<VillageSceneRig>(FindObjectsInactive.Include);
            if (rig == null || rig.Camera == null) return;

            // The simulation camera culls everything but its own layer, so a prefab authored on the
            // wrong one spawns fine, logs nothing and renders nothing.
            var layer = villager.gameObject.layer;
            if ((rig.Camera.cullingMask & (1 << layer)) != 0) return;

            Debug.LogError(
                $"[Village] Villager prefab is on layer '{LayerMask.LayerToName(layer)}', which the " +
                "simulation camera does not render. Villagers will exist but stay invisible.",
                villager);
        }
    }
}
