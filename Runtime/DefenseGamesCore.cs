using System;
using System.Collections.Generic;
using Deucarian.Combat;
using Deucarian.Encounters;
using Deucarian.GameplayFoundation;
using Deucarian.WorldNavigation;
using Deucarian.WorldSpawning;
using UnityEngine;

namespace Deucarian.DefenseGames
{
    /// <summary>Stable content identifier for a defense objective such as a base, lane endpoint, crystal, convoy, or player.</summary>
    public readonly struct DefenseObjectiveId : IEquatable<DefenseObjectiveId>, IComparable<DefenseObjectiveId> { private readonly ContentId _value; public DefenseObjectiveId(string value) { _value = new ContentId(value); } public string Value => _value.Value; public bool IsEmpty => _value.IsEmpty; public bool Equals(DefenseObjectiveId other) => _value.Equals(other._value); public override bool Equals(object obj) => obj is DefenseObjectiveId other && Equals(other); public override int GetHashCode() => _value.GetHashCode(); public int CompareTo(DefenseObjectiveId other) => _value.CompareTo(other._value); public override string ToString() => Value; }
    /// <summary>Runtime-assigned identifier for an attacker tracked by a defense session.</summary>
    public readonly struct DefenseAgentId : IEquatable<DefenseAgentId>, IComparable<DefenseAgentId> { public DefenseAgentId(long value) { if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); Value = value; } public long Value { get; } public bool Equals(DefenseAgentId other) => Value == other.Value; public override bool Equals(object obj) => obj is DefenseAgentId other && Equals(other); public override int GetHashCode() => Value.GetHashCode(); public int CompareTo(DefenseAgentId other) => Value.CompareTo(other.Value); public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture); }
    /// <summary>Stable content identifier reserved for project-authored defense routes.</summary>
    public readonly struct DefenseRouteId : IEquatable<DefenseRouteId>, IComparable<DefenseRouteId> { private readonly ContentId _value; public DefenseRouteId(string value) { _value = new ContentId(value); } public string Value => _value.Value; public bool IsEmpty => _value.IsEmpty; public bool Equals(DefenseRouteId other) => _value.Equals(other._value); public override bool Equals(object obj) => obj is DefenseRouteId other && Equals(other); public override int GetHashCode() => _value.GetHashCode(); public int CompareTo(DefenseRouteId other) => _value.CompareTo(other._value); public override string ToString() => Value; }

    public enum DefenseRuntimeState { Created = 0, Running = 1, Stopped = 2 }
    public enum DefenseAgentRole { Attacker = 0 }
    public enum DefenseAgentLifecycle { SpawnRequested = 0, Spawned = 1, Navigating = 2, ReachedObjective = 3, Killed = 4, Despawned = 5, Failed = 6 }
    public enum DefenseFailureReason { None = 0, NotRunning = 1, UnknownObjective = 2, UnknownAgent = 3, DuplicateSignal = 4, SpawnFailed = 5, NavigationFailed = 6, InvalidInput = 7 }
    public enum DefenseEventKind { RuntimeStarted = 0, RuntimeStopped = 1, AgentSpawned = 2, AgentNavigating = 3, AgentKilled = 4, AgentDespawned = 5, AgentReachedObjective = 6, ObjectiveDamaged = 7, ObjectiveFailed = 8, MetricEmitted = 9, SpawnFailed = 10, NavigationFailed = 11 }
    public enum DefenseRouteKind { Destination = 0, Path = 1 }
    public enum DefenseMetricKind { AgentDefeated = 0, AgentLeaked = 1, ObjectiveDamaged = 2 }

    /// <summary>Content definition for a defense objective with optional health and optional lives.</summary>
    public sealed class DefenseObjectiveDefinition
    {
        public DefenseObjectiveDefinition(DefenseObjectiveId id, double maximumHealth = 0d, int lives = -1, IReadOnlyList<GameplayTag> tags = null, double maximumShield = 0d)
        {
            if (id.IsEmpty) throw new ArgumentException("Objective id cannot be empty.", nameof(id));
            if (maximumHealth < 0d) throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (maximumShield < 0d) throw new ArgumentOutOfRangeException(nameof(maximumShield));
            if (lives < -1) throw new ArgumentOutOfRangeException(nameof(lives));
            Id = id; MaximumHealth = maximumHealth; MaximumShield = maximumShield; Lives = lives; Tags = Copy(tags);
        }
        public DefenseObjectiveId Id { get; }
        public double MaximumHealth { get; }
        public double MaximumShield { get; }
        public int Lives { get; }
        public IReadOnlyList<GameplayTag> Tags { get; }
        private static GameplayTag[] Copy(IReadOnlyList<GameplayTag> source) { if (source == null) return Array.Empty<GameplayTag>(); var copy = new GameplayTag[source.Count]; for (int i = 0; i < source.Count; i++) copy[i] = source[i]; return copy; }
    }

    /// <summary>Mutable runtime state for a defense objective.</summary>
    public sealed class DefenseObjectiveState
    {
        public DefenseObjectiveState(DefenseObjectiveDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (definition.MaximumHealth > 0d) Health = new HealthState(new CombatantId("defense.objective/" + definition.Id.Value), definition.MaximumHealth, definition.MaximumHealth, definition.MaximumShield, definition.MaximumShield);
            LivesRemaining = definition.Lives;
        }
        public DefenseObjectiveDefinition Definition { get; }
        public HealthState Health { get; }
        public int LivesRemaining { get; private set; }
        public bool Failed { get; private set; }
        public void ApplyLeak() { if (LivesRemaining >= 0) { LivesRemaining = Math.Max(0, LivesRemaining - 1); if (LivesRemaining == 0) Failed = true; } }
        public void MarkFailed() { Failed = true; }
    }

    /// <summary>Definition for a defense runtime session.</summary>
    public sealed class DefenseRuntimeDefinition
    {
        private readonly DefenseObjectiveDefinition[] _objectives;
        public DefenseRuntimeDefinition(IReadOnlyList<DefenseObjectiveDefinition> objectives)
        {
            if (objectives == null || objectives.Count == 0) throw new ArgumentException("At least one objective is required.", nameof(objectives));
            _objectives = new DefenseObjectiveDefinition[objectives.Count];
            var seen = new HashSet<DefenseObjectiveId>();
            for (int i = 0; i < objectives.Count; i++) { _objectives[i] = objectives[i] ?? throw new ArgumentException("Objective cannot be null."); if (!seen.Add(_objectives[i].Id)) throw new ArgumentException("Duplicate objective: " + _objectives[i].Id); }
        }
        public IReadOnlyList<DefenseObjectiveDefinition> Objectives => _objectives;
    }

    /// <summary>Route assignment produced by project code for an attacker.</summary>
    public readonly struct DefenseRouteAssignment
    {
        private DefenseRouteAssignment(DefenseRouteKind kind, DefenseObjectiveId objectiveId, Vector3 destination, MovementPath path)
        {
            Kind = kind; ObjectiveId = objectiveId; Destination = destination; Path = path;
        }
        public DefenseRouteKind Kind { get; }
        public DefenseObjectiveId ObjectiveId { get; }
        public Vector3 Destination { get; }
        public MovementPath Path { get; }
        public static DefenseRouteAssignment DestinationTo(DefenseObjectiveId objectiveId, Vector3 destination) => new DefenseRouteAssignment(DefenseRouteKind.Destination, objectiveId, destination, null);
        public static DefenseRouteAssignment FollowPath(DefenseObjectiveId objectiveId, MovementPath path) => new DefenseRouteAssignment(DefenseRouteKind.Path, objectiveId, default, path);
    }

    /// <summary>Single deterministic event emitted by a defense runtime operation.</summary>
    public readonly struct DefenseEvent
    {
        public DefenseEvent(DefenseEventKind kind, DefenseAgentId agentId = default, DefenseObjectiveId objectiveId = default, double value = 0d, DefenseFailureReason failure = DefenseFailureReason.None)
        {
            Kind = kind; AgentId = agentId; ObjectiveId = objectiveId; Value = value; FailureReason = failure;
        }
        public DefenseEventKind Kind { get; }
        public DefenseAgentId AgentId { get; }
        public DefenseObjectiveId ObjectiveId { get; }
        public double Value { get; }
        public DefenseFailureReason FailureReason { get; }
    }

    /// <summary>Result from a start, stop, or terminal lifecycle signal.</summary>
    public readonly struct DefenseSignalResult
    {
        public DefenseSignalResult(bool succeeded, DefenseFailureReason failureReason, IReadOnlyList<DefenseEvent> events)
        {
            Succeeded = succeeded; FailureReason = failureReason; Events = Copy(events);
        }
        public bool Succeeded { get; }
        public DefenseFailureReason FailureReason { get; }
        public IReadOnlyList<DefenseEvent> Events { get; }
        private static DefenseEvent[] Copy(IReadOnlyList<DefenseEvent> source) { if (source == null) return Array.Empty<DefenseEvent>(); var copy = new DefenseEvent[source.Count]; for (int i = 0; i < source.Count; i++) copy[i] = source[i]; return copy; }
    }

    /// <summary>Result from consuming a spawn request.</summary>
    public readonly struct DefenseSpawnResult
    {
        public DefenseSpawnResult(bool succeeded, DefenseFailureReason failureReason, DefenseAgentId agentId, IReadOnlyList<DefenseEvent> events)
        {
            Succeeded = succeeded; FailureReason = failureReason; AgentId = agentId; Events = Copy(events);
        }
        public bool Succeeded { get; }
        public DefenseFailureReason FailureReason { get; }
        public DefenseAgentId AgentId { get; }
        public IReadOnlyList<DefenseEvent> Events { get; }
        private static DefenseEvent[] Copy(IReadOnlyList<DefenseEvent> source) { if (source == null) return Array.Empty<DefenseEvent>(); var copy = new DefenseEvent[source.Count]; for (int i = 0; i < source.Count; i++) copy[i] = source[i]; return copy; }
    }

    /// <summary>Maps encounter spawn requests and spawned objects to defense destinations or paths.</summary>
    public interface IDefenseRouteResolver { bool TryResolveRoute(SpawnRequest request, GameObject spawnedObject, out DefenseRouteAssignment assignment); }
    /// <summary>Receives defense metrics for encounter or session orchestration.</summary>
    public interface IDefenseEncounterMetricSink { void Record(DefenseMetricKind kind, DefenseAgentId agentId, DefenseObjectiveId objectiveId, long amount); }
    /// <summary>Applies objective damage through a combat implementation.</summary>
    public interface IDefenseCombatAdapter { DamageResolutionResult ApplyObjectiveDamage(DefenseObjectiveState objective, double damage); }
    /// <summary>Spawns and despawns defense attackers through a world spawning implementation.</summary>
    public interface IDefenseWorldSpawner { SpawnResult Spawn(SpawnRequest request); DespawnResult Despawn(SpawnInstanceId instanceId, DespawnReason reason); }
    /// <summary>Registers spawned attackers with navigation and cleans them up after terminal lifecycle signals.</summary>
    public interface IDefenseNavigator { bool RegisterAndAssign(GameObject spawnedObject, DefenseRouteAssignment assignment, out MovementAgentId movementAgentId); void Cleanup(MovementAgentId movementAgentId); }

    /// <summary>Combat adapter that applies defense objective damage through <see cref="DamageResolver"/>.</summary>
    public sealed class CombatDefenseObjectiveAdapter : IDefenseCombatAdapter
    {
        private readonly CombatCatalog _catalog;
        private readonly DamageTypeId _damageType;
        private readonly CombatSourceSnapshot _source;
        private readonly CombatDefenseSnapshot _defense;
        public CombatDefenseObjectiveAdapter(CombatCatalog catalog, DamageTypeId damageType, CombatSourceSnapshot source = null, CombatDefenseSnapshot defense = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _damageType = damageType;
            _source = source;
            _defense = defense;
        }
        public DamageResolutionResult ApplyObjectiveDamage(DefenseObjectiveState objective, double damage)
        {
            if (objective == null) throw new ArgumentNullException(nameof(objective));
            if (objective.Health == null) return CombatDamageResolver.Resolve(_catalog, null, null, null);
            return CombatDamageResolver.Resolve(_catalog, objective.Health, null, new DamageRequest(objective.Health.Id, new[] { new DamageComponent(_damageType, damage) }, _source, _defense));
        }
    }

    /// <summary>World Spawning adapter for defense attacker creation and cleanup.</summary>
    public sealed class WorldSpawnDefenseAdapter : IDefenseWorldSpawner
    {
        private readonly WorldSpawnService _service;
        public WorldSpawnDefenseAdapter(WorldSpawnService service) { _service = service ?? throw new ArgumentNullException(nameof(service)); }
        public SpawnResult Spawn(SpawnRequest request) => _service.Spawn(request);
        public DespawnResult Despawn(SpawnInstanceId instanceId, DespawnReason reason) => _service.Despawn(instanceId, reason);
    }

    /// <summary>World Navigation adapter for destination and path-following route assignments.</summary>
    public sealed class WorldNavigationDefenseAdapter : IDefenseNavigator
    {
        private readonly WorldNavigationService _service;
        private readonly IMovementSpeedProvider _speedProvider;
        public WorldNavigationDefenseAdapter(WorldNavigationService service, IMovementSpeedProvider speedProvider) { _service = service; _speedProvider = speedProvider; }
        public bool RegisterAndAssign(GameObject spawnedObject, DefenseRouteAssignment assignment, out MovementAgentId movementAgentId)
        {
            movementAgentId = default;
            if (spawnedObject == null) return false;
            MovementAgentHandle handle = _service.Register(new TransformMovementPoseAccessor(spawnedObject.transform), _speedProvider);
            movementAgentId = handle.Id;
            MovementResult result = assignment.Kind == DefenseRouteKind.Path ? _service.FollowPath(handle.Id, assignment.Path) : _service.SetDestination(handle.Id, assignment.Destination);
            return result.Succeeded;
        }
        public void Cleanup(MovementAgentId movementAgentId) { _service.CleanupDespawned(movementAgentId); }
    }

    /// <summary>Immutable snapshot of a defense agent.</summary>
    public readonly struct DefenseAgentSnapshot
    {
        public DefenseAgentSnapshot(DefenseAgentId id, DefenseAgentLifecycle lifecycle, SpawnableId spawnableId) { Id = id; Lifecycle = lifecycle; SpawnableId = spawnableId; }
        public DefenseAgentId Id { get; }
        public DefenseAgentLifecycle Lifecycle { get; }
        public SpawnableId SpawnableId { get; }
    }

    /// <summary>Immutable snapshot of a defense objective.</summary>
    public readonly struct DefenseObjectiveSnapshot
    {
        public DefenseObjectiveSnapshot(DefenseObjectiveId id, double health, int lives, bool failed, double shield = 0d) { Id = id; Health = health; Lives = lives; Failed = failed; Shield = shield; }
        public DefenseObjectiveId Id { get; }
        public double Health { get; }
        public double Shield { get; }
        public int Lives { get; }
        public bool Failed { get; }
    }

    /// <summary>Immutable snapshot of a defense runtime.</summary>
    public sealed class DefenseSnapshot
    {
        public DefenseSnapshot(DefenseRuntimeState state, IReadOnlyList<DefenseAgentSnapshot> agents, IReadOnlyList<DefenseObjectiveSnapshot> objectives)
        {
            State = state; Agents = Copy(agents); Objectives = Copy(objectives);
        }
        public DefenseRuntimeState State { get; }
        public IReadOnlyList<DefenseAgentSnapshot> Agents { get; }
        public IReadOnlyList<DefenseObjectiveSnapshot> Objectives { get; }
        private static T[] Copy<T>(IReadOnlyList<T> source) { if (source == null) return Array.Empty<T>(); var copy = new T[source.Count]; for (int i = 0; i < source.Count; i++) copy[i] = source[i]; return copy; }
    }

    /// <summary>Coordinates defense attackers from spawn requests through navigation and terminal lifecycle signals.</summary>
    public sealed class DefenseRuntime
    {
        private readonly IDefenseWorldSpawner _spawner;
        private readonly IDefenseNavigator _navigator;
        private readonly IDefenseRouteResolver _routeResolver;
        private readonly IDefenseCombatAdapter _combat;
        private readonly IDefenseEncounterMetricSink _metrics;
        private readonly Dictionary<DefenseObjectiveId, DefenseObjectiveState> _objectives = new Dictionary<DefenseObjectiveId, DefenseObjectiveState>();
        private readonly Dictionary<DefenseAgentId, AgentRecord> _agents = new Dictionary<DefenseAgentId, AgentRecord>();
        private long _nextAgentId;

        public DefenseRuntime(DefenseRuntimeDefinition definition, IDefenseWorldSpawner spawner, IDefenseNavigator navigator, IDefenseRouteResolver routeResolver, IDefenseCombatAdapter combat, IDefenseEncounterMetricSink metrics = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            _routeResolver = routeResolver ?? throw new ArgumentNullException(nameof(routeResolver));
            _combat = combat;
            _metrics = metrics;
            for (int i = 0; i < definition.Objectives.Count; i++) _objectives.Add(definition.Objectives[i].Id, new DefenseObjectiveState(definition.Objectives[i]));
        }

        public DefenseRuntimeDefinition Definition { get; }
        public DefenseRuntimeState State { get; private set; }
        public int ActiveAgentCount { get; private set; }

        public DefenseSignalResult Start() { State = DefenseRuntimeState.Running; return Result(true, DefenseFailureReason.None, new DefenseEvent(DefenseEventKind.RuntimeStarted)); }
        public DefenseSignalResult Stop()
        {
            var events = new List<DefenseEvent> { new DefenseEvent(DefenseEventKind.RuntimeStopped) };
            DefenseAgentId[] ids = new DefenseAgentId[_agents.Count]; _agents.Keys.CopyTo(ids, 0); Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++) CleanupAgent(_agents[ids[i]], DespawnReason.Clear, events, DefenseAgentLifecycle.Despawned);
            State = DefenseRuntimeState.Stopped; ActiveAgentCount = 0;
            return Result(true, DefenseFailureReason.None, events);
        }

        public DefenseSpawnResult ConsumeSpawnRequest(SpawnRequest request)
        {
            var events = new List<DefenseEvent>();
            if (State != DefenseRuntimeState.Running) return new DefenseSpawnResult(false, DefenseFailureReason.NotRunning, default, events);
            SpawnResult spawn = _spawner.Spawn(request);
            if (!spawn.Succeeded) { events.Add(new DefenseEvent(DefenseEventKind.SpawnFailed, failure: DefenseFailureReason.SpawnFailed)); return new DefenseSpawnResult(false, DefenseFailureReason.SpawnFailed, default, events); }
            if (!_routeResolver.TryResolveRoute(request, spawn.Instance, out DefenseRouteAssignment route) || !_objectives.ContainsKey(route.ObjectiveId))
            {
                _spawner.Despawn(spawn.InstanceId, DespawnReason.Clear);
                events.Add(new DefenseEvent(DefenseEventKind.NavigationFailed, failure: DefenseFailureReason.NavigationFailed));
                return new DefenseSpawnResult(false, DefenseFailureReason.NavigationFailed, default, events);
            }
            var agentId = new DefenseAgentId(++_nextAgentId);
            if (!_navigator.RegisterAndAssign(spawn.Instance, route, out MovementAgentId movementId))
            {
                _spawner.Despawn(spawn.InstanceId, DespawnReason.Clear);
                events.Add(new DefenseEvent(DefenseEventKind.NavigationFailed, agentId, route.ObjectiveId, failure: DefenseFailureReason.NavigationFailed));
                return new DefenseSpawnResult(false, DefenseFailureReason.NavigationFailed, agentId, events);
            }
            var record = new AgentRecord(agentId, request, spawn.InstanceId, movementId, route.ObjectiveId, request.SpawnableId) { Lifecycle = DefenseAgentLifecycle.Navigating };
            _agents.Add(agentId, record); ActiveAgentCount++;
            events.Add(new DefenseEvent(DefenseEventKind.AgentSpawned, agentId, route.ObjectiveId));
            events.Add(new DefenseEvent(DefenseEventKind.AgentNavigating, agentId, route.ObjectiveId));
            return new DefenseSpawnResult(true, DefenseFailureReason.None, agentId, events);
        }

        public DefenseSignalResult ReportKilled(DefenseAgentId agentId) => TerminalSignal(agentId, DefenseAgentLifecycle.Killed, DefenseEventKind.AgentKilled, DefenseMetricKind.AgentDefeated, 0d);
        public DefenseSignalResult ReportDespawned(DefenseAgentId agentId) => TerminalSignal(agentId, DefenseAgentLifecycle.Despawned, DefenseEventKind.AgentDespawned, DefenseMetricKind.AgentDefeated, 0d, false);
        public DefenseSignalResult ReportReachedObjective(DefenseAgentId agentId, double damage = 1d)
        {
            if (!_agents.TryGetValue(agentId, out AgentRecord record)) return Result(false, DefenseFailureReason.UnknownAgent);
            if (IsTerminal(record.Lifecycle)) return Result(false, DefenseFailureReason.DuplicateSignal);
            var events = new List<DefenseEvent> { new DefenseEvent(DefenseEventKind.AgentReachedObjective, agentId, record.ObjectiveId) };
            DefenseObjectiveState objective = _objectives[record.ObjectiveId];
            if (objective.Health != null && _combat != null)
            {
                DamageResolutionResult result = _combat.ApplyObjectiveDamage(objective, damage);
                events.Add(new DefenseEvent(DefenseEventKind.ObjectiveDamaged, agentId, record.ObjectiveId, result.HealthDamage));
                if (objective.Health.LifeState == LifeState.Dead) { objective.MarkFailed(); events.Add(new DefenseEvent(DefenseEventKind.ObjectiveFailed, agentId, record.ObjectiveId)); }
            }
            else
            {
                objective.ApplyLeak();
                events.Add(new DefenseEvent(DefenseEventKind.ObjectiveDamaged, agentId, record.ObjectiveId, 1));
                if (objective.Failed) events.Add(new DefenseEvent(DefenseEventKind.ObjectiveFailed, agentId, record.ObjectiveId));
            }
            _metrics?.Record(DefenseMetricKind.AgentLeaked, agentId, record.ObjectiveId, 1);
            _metrics?.Record(DefenseMetricKind.ObjectiveDamaged, agentId, record.ObjectiveId, 1);
            events.Add(new DefenseEvent(DefenseEventKind.MetricEmitted, agentId, record.ObjectiveId));
            CleanupAgent(record, DespawnReason.Completed, events, DefenseAgentLifecycle.ReachedObjective);
            return Result(true, DefenseFailureReason.None, events);
        }

        public DefenseSnapshot CreateSnapshot()
        {
            var agents = new DefenseAgentSnapshot[_agents.Count]; int i = 0;
            foreach (AgentRecord record in _agents.Values) agents[i++] = new DefenseAgentSnapshot(record.Id, record.Lifecycle, record.SpawnableId);
            Array.Sort(agents, (a, b) => a.Id.CompareTo(b.Id));
            var objectives = new DefenseObjectiveSnapshot[_objectives.Count]; i = 0;
            foreach (DefenseObjectiveState objective in _objectives.Values) objectives[i++] = new DefenseObjectiveSnapshot(objective.Definition.Id, objective.Health == null ? -1 : objective.Health.CurrentHealth, objective.LivesRemaining, objective.Failed, objective.Health == null ? 0 : objective.Health.CurrentShield);
            Array.Sort(objectives, (a, b) => a.Id.CompareTo(b.Id));
            return new DefenseSnapshot(State, agents, objectives);
        }

        private DefenseSignalResult TerminalSignal(DefenseAgentId agentId, DefenseAgentLifecycle lifecycle, DefenseEventKind eventKind, DefenseMetricKind metric, double value, bool emitMetric = true)
        {
            if (!_agents.TryGetValue(agentId, out AgentRecord record)) return Result(false, DefenseFailureReason.UnknownAgent);
            if (IsTerminal(record.Lifecycle)) return Result(false, DefenseFailureReason.DuplicateSignal);
            var events = new List<DefenseEvent> { new DefenseEvent(eventKind, agentId, record.ObjectiveId, value) };
            if (emitMetric) { _metrics?.Record(metric, agentId, record.ObjectiveId, 1); events.Add(new DefenseEvent(DefenseEventKind.MetricEmitted, agentId, record.ObjectiveId)); }
            CleanupAgent(record, lifecycle == DefenseAgentLifecycle.Killed ? DespawnReason.Killed : DespawnReason.Requested, events, lifecycle);
            return Result(true, DefenseFailureReason.None, events);
        }

        private void CleanupAgent(AgentRecord record, DespawnReason reason, List<DefenseEvent> events, DefenseAgentLifecycle lifecycle)
        {
            _navigator.Cleanup(record.MovementAgentId);
            _spawner.Despawn(record.SpawnInstanceId, reason);
            record.Lifecycle = lifecycle;
            ActiveAgentCount = Math.Max(0, ActiveAgentCount - 1);
            _agents[record.Id] = record;
        }

        private static bool IsTerminal(DefenseAgentLifecycle lifecycle) => lifecycle == DefenseAgentLifecycle.Killed || lifecycle == DefenseAgentLifecycle.Despawned || lifecycle == DefenseAgentLifecycle.ReachedObjective || lifecycle == DefenseAgentLifecycle.Failed;
        private static DefenseSignalResult Result(bool succeeded, DefenseFailureReason reason, params DefenseEvent[] events) => new DefenseSignalResult(succeeded, reason, events);
        private static DefenseSignalResult Result(bool succeeded, DefenseFailureReason reason, IReadOnlyList<DefenseEvent> events) => new DefenseSignalResult(succeeded, reason, events);

        private sealed class AgentRecord
        {
            public AgentRecord(DefenseAgentId id, SpawnRequest request, SpawnInstanceId spawnInstanceId, MovementAgentId movementAgentId, DefenseObjectiveId objectiveId, SpawnableId spawnableId)
            {
                Id = id; Request = request; SpawnInstanceId = spawnInstanceId; MovementAgentId = movementAgentId; ObjectiveId = objectiveId; SpawnableId = spawnableId; Lifecycle = DefenseAgentLifecycle.Spawned;
            }
            public DefenseAgentId Id { get; }
            public SpawnRequest Request { get; }
            public SpawnInstanceId SpawnInstanceId { get; }
            public MovementAgentId MovementAgentId { get; }
            public DefenseObjectiveId ObjectiveId { get; }
            public SpawnableId SpawnableId { get; }
            public DefenseAgentLifecycle Lifecycle { get; set; }
        }
    }
}
