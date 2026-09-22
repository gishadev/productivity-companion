using System.Collections.Generic;
using UnityEngine;

namespace gishadev.companion.Village.Villagers
{
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

        // Idempotent: one tick can fire several level-ups.
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

        // Bounded rejection sampling; spacing is a preference, not a guarantee.
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

                if (((Vector2)other.transform.position - world).sqrMagnitude < sqrSpacing) return false;
            }

            return true;
        }

        // Both mistakes look broken rather than throw, so they're checked once and logged.
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

            // The simulation camera culls other layers, so a wrong layer renders nothing, silently.
            var layer = villager.gameObject.layer;
            if ((rig.Camera.cullingMask & (1 << layer)) != 0) return;

            Debug.LogError(
                $"[Village] Villager prefab is on layer '{LayerMask.LayerToName(layer)}', which the " +
                "simulation camera does not render. Villagers will exist but stay invisible.",
                villager);
        }
    }
}
