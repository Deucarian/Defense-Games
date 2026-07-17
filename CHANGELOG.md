# Changelog

## 0.1.1 - 2026-07-17

- Aligned package metadata and samples with the portfolio contract; direct Deucarian dependencies now use the coordinated patch versions.
- Aligned the package manifest to require `com.deucarian.world-spawning` `0.2.0`, matching the current generic World Spawning API consumed by Defense Games tests and samples.

## 0.1.0 - 2026-06-22

- Added defense-game composition runtime for Encounters, Combat, World Spawning, and World Navigation.
- Added objective, agent lifecycle, metric, signal, snapshot, adapter, tests, benchmark, docs, and sample scaffolding.
- Updated objective damage integration to use Combat `CombatDamageResolver` results instead of Defense-owned health mutation.
- Breaking for custom adapters: `IDefenseCombatAdapter.ApplyObjectiveDamage` now returns `DamageResolutionResult`.
