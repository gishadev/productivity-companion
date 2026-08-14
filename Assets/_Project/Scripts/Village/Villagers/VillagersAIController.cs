using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace gishadev.companion.Village.Villagers
{
    /// <summary>
    /// One state machine for every villager, rather than one per villager.
    ///
    /// The package's <c>gishadev.tools.StateMachine</c> keys its transition table on <c>IState</c>
    /// instances, so states cannot be shared between owners: each villager would carry its own machine,
    /// dictionary, transition lists and a closure per edge, and every condition delegate would be polled
    /// every frame. Here the whole population is a flat array of structs walked in one loop — no
    /// allocation, no virtual dispatch, and adding a state is a case label.
    /// </summary>
    public sealed class VillagersAIController
    {
        private const int InitialCapacity = 32;

        // Squared, to keep the arrival test off the square root. Well under one art pixel at PPU 100.
        private const float ArrivalSqrDistance = 0.0004f;

        private readonly VillageMasterSO _master;
        private readonly VillageView _view;

        private Agent[] _agents = new Agent[InitialCapacity];
        private int _count;

        public VillagersAIController(VillageMasterSO master, VillageView view)
        {
            _master = master;
            _view = view;
        }

        public int Count => _count;

        public void Add(Villager villager)
        {
            if (villager == null) return;

            if (_count == _agents.Length)
                Array.Resize(ref _agents, _agents.Length * 2);

            // SortingOrder starts unmatchable so the first ApplySorting always writes through: a default
            // of 0 is a real order, and a villager spawning at y = 0 would keep the prefab's instead.
            _agents[_count] = new Agent
            {
                View = villager,
                Tf = villager.transform,
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

                RemoveAt(i);
                return;
            }
        }

        public void Clear()
        {
            // Cleared rather than left in place so the array stops holding destroyed views alive.
            Array.Clear(_agents, 0, _count);
            _count = 0;
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < _count; i++)
            {
                // ref, so the switch mutates the array element instead of a copy. This is why the
                // backing store is an array: List<T>'s indexer returns a copy and would need a
                // write-back, and CollectionsMarshal.AsSpan is past this project's framework level.
                ref var agent = ref _agents[i];

                // Unity's overloaded == is the only null check that catches a destroyed object.
                if (agent.View == null)
                {
                    RemoveAt(i);
                    i--;
                    continue;
                }

                agent.StateTimer += deltaTime;

                switch (agent.State)
                {
                    case VillagerState.Idle:
                        TickIdle(ref agent);
                        break;

                    case VillagerState.Wander:
                        TickWander(ref agent, deltaTime);
                        break;
                }
            }
        }

        private void TickIdle(ref Agent agent)
        {
            if (agent.StateTimer < agent.StateDuration) return;

            // Nowhere to walk to: stay put and try again after another idle, rather than spinning
            // through a state transition every frame.
            if (_view == null || _view.WalkableArea == null)
            {
                EnterIdle(ref agent);
                return;
            }

            EnterWander(ref agent);
        }

        private void TickWander(ref Agent agent, float deltaTime)
        {
            var position = agent.Tf.position;
            var current = new Vector2(position.x, position.y);
            var next = Vector2.MoveTowards(current, agent.Destination, _master.WalkSpeed * deltaTime);

            // z is preserved rather than zeroed: depth comes from sorting order, not from z, and
            // stomping it would be a silent surprise the moment anything else uses it.
            agent.Tf.position = new Vector3(next.x, next.y, position.z);
            ApplySorting(ref agent, next.y);

            // The timeout is the guard against a zero walk speed, which would otherwise never arrive.
            if ((next - agent.Destination).sqrMagnitude > ArrivalSqrDistance &&
                agent.StateTimer < agent.StateDuration) return;

            EnterIdle(ref agent);
        }

        private void EnterIdle(ref Agent agent)
        {
            agent.State = VillagerState.Idle;
            agent.StateTimer = 0f;
            agent.StateDuration = _master.RandomIdleDuration();
        }

        private void EnterWander(ref Agent agent)
        {
            var position = agent.Tf.position;
            var current = new Vector2(position.x, position.y);

            // Offset from where the villager stands, then pulled back inside: picking anywhere in the
            // area would send everyone marching across the whole village on every walk.
            var destination = _view.WalkableArea.ClampInside(
                current + Random.insideUnitCircle * _master.WanderRadius);

            agent.State = VillagerState.Wander;
            agent.StateTimer = 0f;
            agent.Destination = destination;
            agent.StateDuration = TravelTimeout(Vector2.Distance(current, destination));

            agent.View.SetFacingLeft(destination.x < current.x);
        }

        // Cached per agent so a walk that stays within one rendered pixel of height does not touch the
        // renderer at all — most frames of most walks, since the band is far wider than it is tall.
        private static void ApplySorting(ref Agent agent, float worldY)
        {
            var order = YSorting.OrderFor(worldY);
            if (agent.SortingOrder == order) return;

            agent.SortingOrder = order;
            agent.View.SetSortingOrder(order);
        }

        // Generous against the straight-line time, so it only ever fires when something is wrong.
        private float TravelTimeout(float distance)
        {
            var speed = _master.WalkSpeed;
            return speed > 0f ? distance / speed * 2f + 1f : 1f;
        }

        private void RemoveAt(int index)
        {
            // Order is not meaningful, so the last element backfills rather than shifting the tail.
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
            public int SortingOrder;
        }
    }
}
