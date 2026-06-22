using System;
using Deucarian.Combat;
using Deucarian.DefenseGames;
using Deucarian.Encounters;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;

public static class DefenseGamesMinimalSample
{
    public static DefenseSnapshot Run()
    {
        var objectiveId = new DefenseObjectiveId("objective.base");
        var runtime = new DefenseRuntime(
            new DefenseRuntimeDefinition(new[] { new DefenseObjectiveDefinition(objectiveId, maximumHealth: 25) }),
            new NullSpawner(),
            new NullNavigator(),
            new FixedRouteResolver(DefenseRouteAssignment.DestinationTo(objectiveId, Vector3.zero)),
            new CombatDefenseObjectiveAdapter(
                new CombatCatalog(new[] { new DamageTypeDefinition(new DamageTypeId("damage.leak")) }),
                new DamageTypeId("damage.leak")));

        runtime.Start();
        DefenseSpawnResult spawn = runtime.ConsumeSpawnRequest(new SpawnRequest(
            new EncounterId("encounter.sample"),
            new WaveId("wave.one"),
            new SpawnGroupId("group.one"),
            new SpawnableId("enemy.sample"),
            new SpawnChannelId("channel.entry"),
            0,
            1,
            0,
            1));
        runtime.ReportKilled(spawn.AgentId);
        return runtime.CreateSnapshot();
    }

    private sealed class NullSpawner : IDefenseWorldSpawner
    {
        public SpawnResult Spawn(SpawnRequest request) => new SpawnResult(true, SpawnFailureReason.None, new SpawnInstanceId(1), null, WorldSpawnDefenseAdapter.ToWorldRequest(request));
        public DespawnResult Despawn(SpawnInstanceId instanceId, DespawnReason reason) => new DespawnResult(true, DespawnFailureReason.None, instanceId, null, reason);
    }

    private sealed class NullNavigator : IDefenseNavigator
    {
        public bool RegisterAndAssign(GameObject spawnedObject, DefenseRouteAssignment assignment, out MovementAgentId movementAgentId)
        {
            movementAgentId = new MovementAgentId(1);
            return true;
        }

        public void Cleanup(MovementAgentId movementAgentId) { }
    }

    private sealed class FixedRouteResolver : IDefenseRouteResolver
    {
        private readonly DefenseRouteAssignment _assignment;
        public FixedRouteResolver(DefenseRouteAssignment assignment) { _assignment = assignment; }
        public bool TryResolveRoute(SpawnRequest request, GameObject spawnedObject, out DefenseRouteAssignment assignment) { assignment = _assignment; return true; }
    }
}
