# ADR-0001: Defense Games Composition Boundary

## Status

Accepted for 0.1.0.

## Decision

`com.deucarian.defense-games` is the first defense-game composition layer. It consumes Encounters spawn requests, delegates object creation to World Spawning, delegates movement to World Navigation, applies objective damage through a Combat-type adapter, tracks defense agents, emits explicit events/results, and reports encounter metrics through an injected sink.

## Boundary

Lower packages remain focused:

- Encounters says what should spawn and when.
- World Spawning creates and recycles Unity objects.
- World Navigation moves registered objects.
- Combat provides health, damage, and result vocabulary for objective damage.
- Defense Games coordinates these systems into generic defense lifecycle rules.

## Dependency Choices

Runtime depends on Gameplay Foundation, Encounters, Combat, World Spawning, and World Navigation. It does not depend on Progression, Persistence, UI, Core State, or Unity.Entities because rewards, saves, presentation, app state, and ECS are later layers.

## Objective Model

Objectives are generic IDs with optional Combat health and optional lives/leak counters. They are not named core, base, planet, tower, or exit in runtime APIs.

## Agent Lifecycle

Agents progress through requested, spawned, navigating, reached-objective, killed, despawned, or failed states. Signals after terminal states return explicit duplicate/invalid results.

## Spawn, Navigation, And Combat Boundaries

Spawn requests map to spawned world objects through an adapter. Spawned objects map to navigation agents through a route resolver. Reaching an objective applies damage through a Combat adapter or consumes a leak/life. Combat does not know Defense Games exists.

## Encounter Metrics

Defense Games does not complete Encounters directly. It writes explicit metric deltas to `IDefenseEncounterMetricSink`; the owner decides how to apply them.

## Determinism

Spawn requests and signals are processed by sequence/agent ID order. Events are returned explicitly, not broadcast through a global bus.

## Cleanup

Kill, reach, despawn, and stop paths unregister navigation and despawn world objects through adapters. No hidden global state is created.

## Future ECS

Future ECS integration should be a separate adapter that maps defense agents to entity lifecycles. This package remains the managed GameObject composition layer.
