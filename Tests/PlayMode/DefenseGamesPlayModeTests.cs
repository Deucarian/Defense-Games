using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.DefenseGames.Tests
{
    public sealed class DefenseGamesPlayModeTests
    {
        [UnityTest]
        public IEnumerator DefenseRuntime_CanStartAndStopInPlayMode()
        {
            DefenseRuntime runtime = new DefenseRuntime(
                new DefenseRuntimeDefinition(new[] { new DefenseObjectiveDefinition(new DefenseObjectiveId("objective.play"), lives: 1) }),
                new NullSpawner(),
                new NullNavigator(),
                new NullRouteResolver(),
                null);
            Assert.IsTrue(runtime.Start().Succeeded);
            yield return null;
            Assert.IsTrue(runtime.Stop().Succeeded);
        }

        private sealed class NullSpawner : IDefenseWorldSpawner
        {
            public Deucarian.WorldSpawning.SpawnResult Spawn(Deucarian.Encounters.SpawnRequest request) => default;
            public Deucarian.WorldSpawning.DespawnResult Despawn(Deucarian.WorldSpawning.SpawnInstanceId instanceId, Deucarian.WorldSpawning.DespawnReason reason) => default;
        }
        private sealed class NullNavigator : IDefenseNavigator
        {
            public bool RegisterAndAssign(GameObject spawnedObject, DefenseRouteAssignment assignment, out Deucarian.WorldNavigation.MovementAgentId movementAgentId) { movementAgentId = default; return false; }
            public void Cleanup(Deucarian.WorldNavigation.MovementAgentId movementAgentId) { }
        }
        private sealed class NullRouteResolver : IDefenseRouteResolver
        {
            public bool TryResolveRoute(Deucarian.Encounters.SpawnRequest request, GameObject spawnedObject, out DefenseRouteAssignment assignment) { assignment = default; return false; }
        }
    }
}
