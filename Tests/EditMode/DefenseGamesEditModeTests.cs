using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Deucarian.Combat;
using Deucarian.Encounters;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.DefenseGames.Tests
{
    public sealed class DefenseGamesEditModeTests
    {
        private static readonly DefenseObjectiveId Objective = new DefenseObjectiveId("objective.primary");
        private static readonly SpawnableId Enemy = new SpawnableId("enemy.basic");
        private static readonly SpawnChannelId Channel = new SpawnChannelId("channel.entry");

        [Test]
        public void RuntimeStartStopAndSpawnLifecycle_Work()
        {
            TestHarness harness = TestHarness.Create();
            DefenseRuntime runtime = harness.Runtime;
            Assert.AreEqual(DefenseFailureReason.NotRunning, runtime.ConsumeSpawnRequest(Request()).FailureReason);
            Assert.IsTrue(runtime.Start().Succeeded);
            DefenseSpawnResult spawn = runtime.ConsumeSpawnRequest(Request());
            Assert.IsTrue(spawn.Succeeded);
            Assert.AreEqual(1, runtime.ActiveAgentCount);
            DefenseSignalResult stop = runtime.Stop();
            Assert.IsTrue(stop.Succeeded);
            Assert.AreEqual(0, runtime.ActiveAgentCount);
            Assert.AreEqual(1, harness.Navigator.CleanupCount);
        }

        [Test]
        public void SpawnAndNavigationFailures_AreReported()
        {
            TestHarness spawnFail = TestHarness.Create();
            spawnFail.Spawner.FailSpawn = true;
            spawnFail.Runtime.Start();
            Assert.AreEqual(DefenseFailureReason.SpawnFailed, spawnFail.Runtime.ConsumeSpawnRequest(Request()).FailureReason);

            TestHarness navFail = TestHarness.Create();
            navFail.Navigator.FailRegister = true;
            navFail.Runtime.Start();
            Assert.AreEqual(DefenseFailureReason.NavigationFailed, navFail.Runtime.ConsumeSpawnRequest(Request()).FailureReason);
            Assert.AreEqual(1, navFail.Spawner.DespawnCount);
        }

        [Test]
        public void KilledReachedDespawnAndDuplicateSignals_AreDeterministic()
        {
            TestHarness harness = TestHarness.Create();
            harness.Runtime.Start();
            DefenseAgentId killed = harness.Runtime.ConsumeSpawnRequest(Request(sequence: 1)).AgentId;
            DefenseAgentId reached = harness.Runtime.ConsumeSpawnRequest(Request(sequence: 2)).AgentId;
            DefenseAgentId despawned = harness.Runtime.ConsumeSpawnRequest(Request(sequence: 3)).AgentId;

            Assert.IsTrue(harness.Runtime.ReportKilled(killed).Succeeded);
            Assert.AreEqual(DefenseFailureReason.DuplicateSignal, harness.Runtime.ReportReachedObjective(killed).FailureReason);
            Assert.IsTrue(harness.Runtime.ReportReachedObjective(reached, 10).Succeeded);
            Assert.AreEqual(DefenseFailureReason.DuplicateSignal, harness.Runtime.ReportKilled(reached).FailureReason);
            Assert.IsTrue(harness.Runtime.ReportDespawned(despawned).Succeeded);
            Assert.AreEqual(0, harness.Runtime.ActiveAgentCount);
            Assert.AreEqual(1, harness.Metrics.Count(DefenseMetricKind.AgentDefeated));
            Assert.AreEqual(1, harness.Metrics.Count(DefenseMetricKind.AgentLeaked));
        }

        [Test]
        public void ObjectiveDamageAndFailure_UseCombat()
        {
            TestHarness harness = TestHarness.Create(maxHealth: 10);
            harness.Runtime.Start();
            DefenseAgentId agent = harness.Runtime.ConsumeSpawnRequest(Request()).AgentId;
            DefenseSignalResult result = harness.Runtime.ReportReachedObjective(agent, 10);
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(Contains(result.Events, DefenseEventKind.ObjectiveDamaged));
            Assert.IsTrue(Contains(result.Events, DefenseEventKind.ObjectiveFailed));
            DefenseSnapshot snapshot = harness.Runtime.CreateSnapshot();
            Assert.IsTrue(snapshot.Objectives[0].Failed);
        }

        [Test]
        public void ObjectiveDamage_UsesCombatResolverMitigation()
        {
            var adapter = new CombatDefenseObjectiveAdapter(
                new CombatCatalog(new[] { new DamageTypeDefinition(new DamageTypeId("damage.leak")) }),
                new DamageTypeId("damage.leak"),
                defense: new CombatDefenseSnapshot(armor: 4));
            TestHarness harness = TestHarness.Create(maxHealth: 100, adapter: adapter);
            harness.Runtime.Start();
            DefenseAgentId agent = harness.Runtime.ConsumeSpawnRequest(Request()).AgentId;

            DefenseSignalResult result = harness.Runtime.ReportReachedObjective(agent, 10);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(6, FindValue(result.Events, DefenseEventKind.ObjectiveDamaged));
            Assert.AreEqual(94, harness.Runtime.CreateSnapshot().Objectives[0].Health);
        }

        [Test]
        public void ObjectiveShieldAbsorbsDamage_ThroughCombatResolver()
        {
            TestHarness harness = TestHarness.Create(maxHealth: 20, maxShield: 5);
            harness.Runtime.Start();
            DefenseAgentId agent = harness.Runtime.ConsumeSpawnRequest(Request()).AgentId;

            DefenseSignalResult result = harness.Runtime.ReportReachedObjective(agent, 3);
            DefenseObjectiveSnapshot objective = harness.Runtime.CreateSnapshot().Objectives[0];

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, FindValue(result.Events, DefenseEventKind.ObjectiveDamaged));
            Assert.AreEqual(20, objective.Health);
            Assert.AreEqual(2, objective.Shield);
            Assert.IsFalse(objective.Failed);
        }

        [Test]
        public void InvalidCombatRequest_DoesNotMutateObjectiveState()
        {
            var adapter = new CombatDefenseObjectiveAdapter(
                new CombatCatalog(new[] { new DamageTypeDefinition(new DamageTypeId("damage.valid")) }),
                new DamageTypeId("damage.missing"));
            TestHarness harness = TestHarness.Create(maxHealth: 20, maxShield: 5, adapter: adapter);
            harness.Runtime.Start();
            DefenseAgentId agent = harness.Runtime.ConsumeSpawnRequest(Request()).AgentId;

            DefenseSignalResult result = harness.Runtime.ReportReachedObjective(agent, 10);
            DefenseObjectiveSnapshot objective = harness.Runtime.CreateSnapshot().Objectives[0];

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, FindValue(result.Events, DefenseEventKind.ObjectiveDamaged));
            Assert.AreEqual(20, objective.Health);
            Assert.AreEqual(5, objective.Shield);
            Assert.IsFalse(objective.Failed);
        }

        [Test]
        public void LeakLivesObjective_FailsAtZero()
        {
            TestHarness harness = TestHarness.Create(maxHealth: 0, lives: 1);
            harness.Runtime.Start();
            DefenseAgentId agent = harness.Runtime.ConsumeSpawnRequest(Request()).AgentId;
            DefenseSignalResult result = harness.Runtime.ReportReachedObjective(agent);
            Assert.IsTrue(Contains(result.Events, DefenseEventKind.ObjectiveFailed));
            Assert.AreEqual(0, harness.Runtime.CreateSnapshot().Objectives[0].Lives);
        }

        [Test]
        public void RealWorldSpawningNavigationAndEncounterIntegration_Works()
        {
            GameObject prefab = new GameObject("defense-attacker");
            var spawnService = new WorldSpawnService(
                new SpawnableCatalog(new[] { new SpawnableDefinition(new WorldSpawnableId(Enemy.Value), new GameObjectPrefabProvider(prefab), 2, 4) }),
                new ChannelPoseResolver(new Dictionary<WorldSpawnChannelId, SpawnPose> { [new WorldSpawnChannelId(Channel.Value)] = new SpawnPose(Vector3.zero, Quaternion.identity) }));
            var navService = new WorldNavigationService();
            var runtime = new DefenseRuntime(
                Definition(10, -1),
                new WorldSpawnDefenseAdapter(spawnService),
                new WorldNavigationDefenseAdapter(navService, new ConstantMovementSpeedProvider(1)),
                new FixedRouteResolver(DefenseRouteAssignment.DestinationTo(Objective, Vector3.right)),
                CombatAdapter(),
                new RecordingMetricSink());
            try
            {
                spawnService.Warmup();
                runtime.Start();
                EncounterRuntime encounter = new EncounterRuntime(new EncounterDefinition(
                    new EncounterId("encounter.defense"),
                    Array.Empty<WeightedSpawnTableDefinition>(),
                    new[] { new WaveDefinition(new WaveId("wave.one"), 0, new[] { SpawnGroupDefinition.Fixed(new SpawnGroupId("group.one"), Enemy, 1, 1, 0, 1, Channel) }) },
                    new[] { ObjectiveDefinition.AllWavesEmitted(new EncounterObjectiveId("objective.emitted")) }));
                encounter.Start();
                encounter.AdvanceTicks(1);
                SpawnRequest[] buffer = new SpawnRequest[2];
                int count = encounter.DrainSpawnRequests(buffer).Written;
                DefenseSpawnResult result = runtime.ConsumeSpawnRequest(buffer[0]);
                navService.Tick(1);
                Assert.AreEqual(1, count);
                Assert.IsTrue(result.Succeeded);
                Assert.AreEqual(1, runtime.ActiveAgentCount);
                Assert.AreEqual(EncounterLifecycleState.Completed, encounter.State);
            }
            finally { runtime.Stop(); spawnService.Dispose(); UnityEngine.Object.DestroyImmediate(prefab); }
        }

        [Test]
        public void DonorIdleAndTowerDefenseProofs_Work()
        {
            TestHarness donor = TestHarness.Create();
            donor.Runtime.Start();
            DefenseAgentId donorAgent = donor.Runtime.ConsumeSpawnRequest(Request(new SpawnableId("enemy.ghoul-runner"))).AgentId;
            Assert.IsTrue(donor.Runtime.ReportKilled(donorAgent).Succeeded);

            TestHarness idle = TestHarness.Create();
            idle.Runtime.Start();
            DefenseAgentId idleAgent = idle.Runtime.ConsumeSpawnRequest(Request(channel: new SpawnChannelId("perimeter-north"))).AgentId;
            Assert.IsTrue(idle.Runtime.ReportReachedObjective(idleAgent, 2).Succeeded);

            TestHarness tower = TestHarness.Create(route: DefenseRouteAssignment.FollowPath(Objective, new MovementPath(new[] { new MovementWaypoint(Vector3.right), new MovementWaypoint(new Vector3(2, 0, 0)) })));
            tower.Runtime.Start();
            DefenseAgentId creep = tower.Runtime.ConsumeSpawnRequest(Request(channel: new SpawnChannelId("lane-a-entry"))).AgentId;
            Assert.IsTrue(tower.Runtime.ReportKilled(creep).Succeeded);
        }

        [Test]
        public void SnapshotAndEventOrdering_AreConsistent()
        {
            TestHarness harness = TestHarness.Create();
            harness.Runtime.Start();
            DefenseAgentId b = harness.Runtime.ConsumeSpawnRequest(Request(sequence: 2)).AgentId;
            DefenseAgentId a = harness.Runtime.ConsumeSpawnRequest(Request(sequence: 1)).AgentId;
            DefenseSnapshot snapshot = harness.Runtime.CreateSnapshot();
            Assert.AreEqual(1, snapshot.Agents[0].Id.Value);
            Assert.AreEqual(2, snapshot.Agents[1].Id.Value);
            Assert.Less(b.Value, a.Value);
        }

        [Test]
        public void DurableBenchmark_WritesDefenseLifecycleMeasurements()
        {
            BenchmarkMeasurement one = Measure(1000);
            BenchmarkMeasurement five = Measure(5000);
            BenchmarkMeasurement ten = Measure(10000);
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(logDirectory);
            string path = Path.Combine(logDirectory, "defense-games-benchmark-results.json");
            File.WriteAllText(path, BuildBenchmarkJson(one, five, ten), Encoding.UTF8);
            Assert.AreEqual(1000, one.OperationCount);
            Assert.AreEqual(5000, five.OperationCount);
            Assert.AreEqual(10000, ten.OperationCount);
        }

        private static BenchmarkMeasurement Measure(int count)
        {
            TestHarness harness = TestHarness.Create(maxHealth: 100000000);
            harness.Runtime.Start();
            long before = GC.GetAllocatedBytesForCurrentThread();
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                DefenseAgentId id = harness.Runtime.ConsumeSpawnRequest(Request(sequence: i + 1)).AgentId;
                harness.Runtime.ReportKilled(id);
            }
            sw.Stop();
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            return new BenchmarkMeasurement(count, sw.Elapsed.TotalMilliseconds, bytes);
        }

        private static string BuildBenchmarkJson(params BenchmarkMeasurement[] measurements)
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("{");
            b.AppendLine("  \"unityVersion\": \"6000.3.5f1\",");
            b.AppendLine("  \"runtime\": \"Unity EditMode Mono\",");
            b.AppendLine("  \"configuration\": \"defense-games-phase-1h-spawn-register-kill-cleanup\",");
            b.AppendLine("  \"poolSettings\": \"fake warmed adapter for composition hot path\",");
            b.AppendLine("  \"prefabComplexity\": \"fake GameObject adapter\",");
            b.AppendLine("  \"measurements\": [");
            for (int i = 0; i < measurements.Length; i++)
            {
                BenchmarkMeasurement m = measurements[i];
                b.Append("    { \"operationCount\": ").Append(m.OperationCount)
                    .Append(", \"elapsedMs\": ").Append(m.ElapsedMs.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(", \"bytesAllocated\": ").Append(m.BytesAllocated).Append(" }");
                b.AppendLine(i + 1 == measurements.Length ? string.Empty : ",");
            }
            b.AppendLine("  ]");
            b.AppendLine("}");
            return b.ToString();
        }

        private static SpawnRequest Request(SpawnableId? spawnable = null, SpawnChannelId? channel = null, long sequence = 1)
        {
            return new SpawnRequest(new EncounterId("encounter.test"), new WaveId("wave.test"), new SpawnGroupId("group.test"), spawnable ?? Enemy, channel ?? Channel, 0, sequence, 0, sequence);
        }

        private static DefenseRuntimeDefinition Definition(double maxHealth, int lives, double maxShield = 0) => new DefenseRuntimeDefinition(new[] { new DefenseObjectiveDefinition(Objective, maxHealth, lives, maximumShield: maxShield) });
        private static CombatDefenseObjectiveAdapter CombatAdapter() => new CombatDefenseObjectiveAdapter(new CombatCatalog(new[] { new DamageTypeDefinition(new DamageTypeId("damage.leak")) }), new DamageTypeId("damage.leak"));
        private static bool Contains(IReadOnlyList<DefenseEvent> events, DefenseEventKind kind) { for (int i = 0; i < events.Count; i++) if (events[i].Kind == kind) return true; return false; }
        private static double FindValue(IReadOnlyList<DefenseEvent> events, DefenseEventKind kind) { for (int i = 0; i < events.Count; i++) if (events[i].Kind == kind) return events[i].Value; return double.NaN; }

        private readonly struct BenchmarkMeasurement { public BenchmarkMeasurement(int operationCount, double elapsedMs, long bytesAllocated) { OperationCount = operationCount; ElapsedMs = elapsedMs; BytesAllocated = bytesAllocated; } public int OperationCount { get; } public double ElapsedMs { get; } public long BytesAllocated { get; } }

        private sealed class TestHarness
        {
            public DefenseRuntime Runtime;
            public FakeSpawner Spawner;
            public FakeNavigator Navigator;
            public RecordingMetricSink Metrics;
            public static TestHarness Create(double maxHealth = 10, int lives = -1, DefenseRouteAssignment? route = null, double maxShield = 0, IDefenseCombatAdapter adapter = null)
            {
                var h = new TestHarness { Spawner = new FakeSpawner(), Navigator = new FakeNavigator(), Metrics = new RecordingMetricSink() };
                h.Runtime = new DefenseRuntime(Definition(maxHealth, lives, maxShield), h.Spawner, h.Navigator, new FixedRouteResolver(route ?? DefenseRouteAssignment.DestinationTo(Objective, Vector3.zero)), adapter ?? CombatAdapter(), h.Metrics);
                return h;
            }
        }

        private sealed class FakeSpawner : IDefenseWorldSpawner
        {
            private long _next;
            public bool FailSpawn;
            public int DespawnCount;
            public SpawnResult Spawn(SpawnRequest request) => FailSpawn ? new SpawnResult(false, SpawnFailureReason.UnknownSpawnable, default, null, WorldSpawnDefenseAdapter.ToWorldRequest(request)) : new SpawnResult(true, SpawnFailureReason.None, new SpawnInstanceId(++_next), null, WorldSpawnDefenseAdapter.ToWorldRequest(request));
            public DespawnResult Despawn(SpawnInstanceId instanceId, DespawnReason reason) { DespawnCount++; return new DespawnResult(true, DespawnFailureReason.None, instanceId, null, reason); }
        }

        private sealed class FakeNavigator : IDefenseNavigator
        {
            private long _next;
            public bool FailRegister;
            public int CleanupCount;
            public bool RegisterAndAssign(GameObject spawnedObject, DefenseRouteAssignment assignment, out MovementAgentId movementAgentId) { movementAgentId = new MovementAgentId(++_next); return !FailRegister; }
            public void Cleanup(MovementAgentId movementAgentId) { CleanupCount++; }
        }

        private sealed class FixedRouteResolver : IDefenseRouteResolver
        {
            private readonly DefenseRouteAssignment _assignment;
            public FixedRouteResolver(DefenseRouteAssignment assignment) { _assignment = assignment; }
            public bool TryResolveRoute(SpawnRequest request, GameObject spawnedObject, out DefenseRouteAssignment assignment) { assignment = _assignment; return true; }
        }

        private sealed class RecordingMetricSink : IDefenseEncounterMetricSink
        {
            private readonly List<DefenseMetricKind> _kinds = new List<DefenseMetricKind>();
            public void Record(DefenseMetricKind kind, DefenseAgentId agentId, DefenseObjectiveId objectiveId, long amount) { _kinds.Add(kind); }
            public int Count(DefenseMetricKind kind) { int count = 0; for (int i = 0; i < _kinds.Count; i++) if (_kinds[i] == kind) count++; return count; }
        }
    }
}
