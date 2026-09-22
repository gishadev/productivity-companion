using System;
using gishadev.companion.Village.POI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace gishadev.companion.Village.Villagers
{
    // One loop over a struct array for all villagers, instead of a StateMachine per villager.
    public sealed class VillagersAIController
    {
        private const int InitialCapacity = 32;

        private const float ArrivalSqrDistance = 0.0004f;

        private readonly VillageMasterSO _master;
        private readonly VillageView _view;
        private readonly POIRegistry _pois;

        private Agent[] _agents = new Agent[InitialCapacity];
        private int _count;

        public VillagersAIController(VillageMasterSO master, VillageView view, POIRegistry pois)
        {
            _master = master;
            _view = view;
            _pois = pois;
        }

        public int Count => _count;

        public void Add(Villager villager)
        {
            if (villager == null) return;

            if (_count == _agents.Length)
                Array.Resize(ref _agents, _agents.Length * 2);

            // Unmatchable, so the first ApplySorting always writes.
            _agents[_count] = new Agent
            {
                View = villager,
                Tf = villager.transform,
                Facing = Vector2.down,
                SortingOrder = int.MinValue
            };

            EnterIdle(ref _agents[_count]);
            ApplySorting(ref _agents[_count], villager.transform.position.y);

            _count++;
        }

        public void Remove(Villager villager)
        {
            if (villager == null) return;

            for (var i = 0; i < _count; i++)
            {
                if (_agents[i].View != villager) continue;

                ReleasePoi(ref _agents[i]);
                RemoveAt(i);
                return;
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _count; i++)
                ReleasePoi(ref _agents[i]);

            Array.Clear(_agents, 0, _count);
            _count = 0;
        }

        public void Tick(float deltaTime, VillageActivity activity)
        {
            for (var i = 0; i < _count; i++)
            {
                // Array + ref so the switch mutates in place.
                ref var agent = ref _agents[i];

                // Unity's == catches destroyed views; a hiding villager is only disabled.
                if (agent.View == null)
                {
                    ReleasePoi(ref agent);
                    RemoveAt(i);
                    i--;
                    continue;
                }

                agent.StateTimer += deltaTime;

                switch (agent.State)
                {
                    case VillagerState.Idle:
                        TickIdle(ref agent, activity);
                        break;

                    case VillagerState.Wander:
                        TickWander(ref agent, deltaTime);
                        break;

                    case VillagerState.GoToJob:
                        TickGoToJob(ref agent, deltaTime, activity);
                        break;

                    case VillagerState.Working:
                        TickWorking(ref agent, activity);
                        break;

                    case VillagerState.GoToRelax:
                        TickGoToRelax(ref agent, deltaTime, activity);
                        break;

                    case VillagerState.Hiding:
                        TickHiding(ref agent, activity);
                        break;
                }
            }
        }

        // Claim only at the end of an idle, to keep the registry off the hot path.
        private void TickIdle(ref Agent agent, VillageActivity activity)
        {
            if (agent.StateTimer < agent.StateDuration) return;

            if (activity == VillageActivity.Working && TryStartJob(ref agent)) return;
            if (activity == VillageActivity.Relaxing && TryStartRelax(ref agent)) return;

            if (_view == null || _view.WalkableArea == null)
            {
                EnterIdle(ref agent);
                return;
            }

            EnterWander(ref agent);
        }

        private void TickWander(ref Agent agent, float deltaTime)
        {
            if (MoveToward(ref agent, deltaTime)) EnterIdle(ref agent);
        }

        private void TickGoToJob(ref Agent agent, float deltaTime, VillageActivity activity)
        {
            if (activity != VillageActivity.Working)
            {
                ReleasePoi(ref agent);
                EnterIdle(ref agent);
                return;
            }

            if (!MoveToward(ref agent, deltaTime)) return;

            agent.State = VillagerState.Working;
            agent.StateTimer = 0f;

            // Zero speed first, or the locomotion tree stays in walk under the job animation.
            agent.View.SetMovement(agent.Facing, 0f);
            agent.View.SetWorking(true);
        }

        private void TickWorking(ref Agent agent, VillageActivity activity)
        {
            if (activity == VillageActivity.Working) return;

            agent.View.SetWorking(false);
            ReleasePoi(ref agent);
            EnterIdle(ref agent);
        }

        private void TickGoToRelax(ref Agent agent, float deltaTime, VillageActivity activity)
        {
            if (activity != VillageActivity.Relaxing)
            {
                ReleasePoi(ref agent);
                EnterIdle(ref agent);
                return;
            }

            if (!MoveToward(ref agent, deltaTime)) return;

            agent.State = VillagerState.Hiding;
            agent.StateTimer = 0f;
            agent.StateDuration = _master.RandomHideDuration();

            agent.View.SetMovement(agent.Facing, 0f);
            agent.View.SetVisible(false);
        }

        // Ticked while the GameObject is disabled, which is why the AI isn't a MonoBehaviour.
        private void TickHiding(ref Agent agent, VillageActivity activity)
        {
            if (activity == VillageActivity.Relaxing && agent.StateTimer < agent.StateDuration) return;

            if (agent.Poi != null) agent.Tf.position = ToWorld(agent.Poi.TargetPosition, agent.Tf.position.z);

            agent.View.SetVisible(true);
            ApplySorting(ref agent, agent.Tf.position.y);

            ReleasePoi(ref agent);
            EnterIdle(ref agent);
        }

        private bool TryStartJob(ref Agent agent)
        {
            var poi = _pois.TryClaimJob();
            if (poi == null) return false;

            agent.Poi = poi;
            EnterTravel(ref agent, VillagerState.GoToJob, poi.TargetPosition);
            return true;
        }

        private bool TryStartRelax(ref Agent agent)
        {
            // Rolled before claiming, so a villager that stays out doesn't hold a slot.
            if (Random.value > _master.RelaxChance) return false;

            var poi = _pois.TryClaimRelax();
            if (poi == null) return false;

            agent.Poi = poi;
            EnterTravel(ref agent, VillagerState.GoToRelax, poi.TargetPosition);
            return true;
        }

        private void EnterIdle(ref Agent agent)
        {
            agent.State = VillagerState.Idle;
            agent.StateTimer = 0f;
            agent.StateDuration = _master.RandomIdleDuration();

            agent.View.SetMovement(agent.Facing, 0f);
        }

        private void EnterWander(ref Agent agent)
        {
            var current = Flat(agent.Tf.position);

            // Offset from the current position, so walks stay local.
            var destination = _view.WalkableArea.ClampInside(
                current + Random.insideUnitCircle * _master.WanderRadius);

            EnterTravel(ref agent, VillagerState.Wander, destination);
        }

        private void EnterTravel(ref Agent agent, VillagerState state, Vector2 destination)
        {
            var current = Flat(agent.Tf.position);
            var offset = destination - current;

            agent.State = state;
            agent.StateTimer = 0f;
            agent.Destination = destination;
            agent.StateDuration = TravelTimeout(offset.magnitude);

            if (offset.sqrMagnitude > Mathf.Epsilon) agent.Facing = offset.normalized;
            agent.View.SetMovement(agent.Facing, _master.WalkSpeed);
        }

        // True on arrival or timeout.
        private bool MoveToward(ref Agent agent, float deltaTime)
        {
            var position = agent.Tf.position;
            var current = Flat(position);
            var next = Vector2.MoveTowards(current, agent.Destination, _master.WalkSpeed * deltaTime);

            agent.Tf.position = new Vector3(next.x, next.y, position.z);
            ApplySorting(ref agent, next.y);

            // The timeout guards against a zero walk speed.
            return (next - agent.Destination).sqrMagnitude <= ArrivalSqrDistance ||
                   agent.StateTimer >= agent.StateDuration;
        }

        private void ReleasePoi(ref Agent agent)
        {
            if (agent.Poi != null) agent.Poi.Release();
            agent.Poi = null;
        }

        // Cached so walks within one pixel of height don't touch the renderer.
        private static void ApplySorting(ref Agent agent, float worldY)
        {
            var order = YSorting.OrderFor(worldY);
            if (agent.SortingOrder == order) return;

            agent.SortingOrder = order;
            agent.View.SetSortingOrder(order);
        }

        private float TravelTimeout(float distance)
        {
            var speed = _master.WalkSpeed;
            return speed > 0f ? distance / speed * 2f + 1f : 1f;
        }

        private static Vector2 Flat(Vector3 position) => new Vector2(position.x, position.y);

        private static Vector3 ToWorld(Vector2 flat, float z) => new Vector3(flat.x, flat.y, z);

        private void RemoveAt(int index)
        {
            // Swap-remove; order doesn't matter.
            _count--;
            _agents[index] = _agents[_count];
            _agents[_count] = default;
        }

        private struct Agent
        {
            public Villager View;
            public Transform Tf;
            public VillagerState State;
            public float StateTimer;
            public float StateDuration;
            public Vector2 Destination;
            public Vector2 Facing;
            public VillagePOI Poi;
            public int SortingOrder;
        }
    }
}
