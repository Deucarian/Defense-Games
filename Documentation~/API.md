# Defense Games API

`com.deucarian.defense-games` composes existing Deucarian packages into a narrow defense-session runtime. It does not own spawning pools, pathfinding, damage math, encounter scheduling, progression, persistence, UI, or content balance.

## Runtime

- `DefenseRuntime` starts, stops, consumes encounter `SpawnRequest` values, tracks active attackers, receives terminal signals, and produces deterministic snapshots.
- `DefenseRuntimeDefinition` contains one or more `DefenseObjectiveDefinition` entries.
- `DefenseObjectiveDefinition` can model health, shield, lives, all three, or none. Health and shield damage is delegated through `IDefenseCombatAdapter` and resolved by Combat.
- `DefenseSnapshot`, `DefenseAgentSnapshot`, and `DefenseObjectiveSnapshot` expose sorted, immutable copies for tests, telemetry, and adapters.

## Identifiers

- `DefenseObjectiveId` names a defense target such as a base, lane endpoint, crystal, convoy, or player.
- `DefenseAgentId` is assigned by the runtime in deterministic spawn-consumption order.
- `DefenseRouteId` is reserved for content-side route naming.

## Agent Lifecycle

Agents enter through `ConsumeSpawnRequest`. A successful spawn assigns a route and registers navigation before the agent is counted active.

Terminal signals are explicit:

- `ReportKilled`
- `ReportDespawned`
- `ReportReachedObjective`

After a terminal signal, repeated signals for the same agent return `DefenseFailureReason.DuplicateSignal`.

## Integration Ports

- `IDefenseWorldSpawner` adapts `WorldSpawnService`.
- `IDefenseNavigator` adapts `WorldNavigationService`.
- `IDefenseRouteResolver` translates spawn requests and spawned objects into destination or path assignments.
- `IDefenseCombatAdapter` builds/applies Combat damage requests and returns `DamageResolutionResult`.
- `IDefenseEncounterMetricSink` reports defense results back to encounter/session orchestration.

## Built-In Adapters

- `WorldSpawnDefenseAdapter`
- `WorldNavigationDefenseAdapter`
- `CombatDefenseObjectiveAdapter`

These adapters are intentionally thin. Project-specific health bars, rewards, loot, tower targeting, offline income, saving, and UI events belong outside this package.

## Combat Resolver Boundary

Defense Games owns objective lifecycle, leak signals, objective IDs, event interpretation, and cleanup. It does not own shield absorption, health damage math, resistance, armor, penetration, critical hits, exact-zero death, overkill, or status application.

`CombatDefenseObjectiveAdapter` routes objective damage through `CombatDamageResolver.Resolve`. Custom adapters should do the same when they need project-specific source, defense, or status context.

## Migration Note

Phase 1I changes `IDefenseCombatAdapter.ApplyObjectiveDamage` to return Combat `DamageResolutionResult` instead of `HealthChangeResult`. Built-in callers are updated. Custom adapters should return the resolver result and avoid mutating objective health directly.

## Event Stream

`DefenseSignalResult` and `DefenseSpawnResult` carry ordered `DefenseEvent` arrays. Events are copies, so callers can retain results without observing later runtime mutation.

## Dependencies

Runtime dependencies are:

- `com.deucarian.gameplay-foundation`
- `com.deucarian.encounters`
- `com.deucarian.combat`
- `com.deucarian.world-spawning`
- `com.deucarian.world-navigation`

There is no dependency on Progression, Persistence, UI, Core State, Entities, or scene systems.
