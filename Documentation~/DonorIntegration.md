# Donor Integration Proof

Donor project:

`C:\Repositories\JorisHoef\Codex-Attempted-Vampire-Project\Codex-Attempted-Vampire-Project`

The donor project combines enemy spawning, movement, health, contact damage, rewards, presentation, and run-state transitions in scene-level gameplay classes. Phase 1H does not migrate those systems. It proves only the generic defense workflow that can replace the donor's cross-cutting enemy lifecycle coordinator later.

## Clean Mappings

- Donor enemy spawn requests map to Encounters `SpawnRequest` values consumed by `DefenseRuntime`.
- Donor active enemy count maps to `DefenseRuntime.ActiveAgentCount`.
- Donor enemy death maps to `ReportKilled`.
- Donor despawn or run clear maps to `ReportDespawned` or `Stop`.
- Donor base/player contact or leak maps to `ReportReachedObjective`.
- Donor run victory checks can observe defense metrics and active counts without owning spawn or navigation internals.

## Adapter Requirements

- A donor route resolver must translate spawn channels and enemy archetypes into a destination or path.
- A donor combat adapter should translate contact damage into Combat `DamageRequest` values.
- Presentation, hit flashes, reward drops, and UI counters remain project-side adapters.
- Progression rewards remain outside Defense Games and should subscribe to killed/leaked metrics.

## Assumptions To Discard

- Enemy lifecycle should not require scene-object searches.
- Reward calculation should not be baked into enemy destruction.
- Objective damage should not be directly coupled to UI presentation.
- Enemy movement and spawning should not require each enemy to own independent orchestration update loops.

## Survivor Specificity Review

The API is not survivor-game-specific. It uses attackers, objectives, spawn requests, routes, and terminal signals. The same shape supports a survivor arena, Idle Auto Defense perimeter, tower-defense lane endpoint, escort convoy, or base-defense encounter.
