# Changelog

## 0.1.0 - 2026-06-22

- Added defense-game composition runtime for Encounters, Combat, World Spawning, and World Navigation.
- Added objective, agent lifecycle, metric, signal, snapshot, adapter, tests, benchmark, docs, and sample scaffolding.
- Updated objective damage integration to use Combat `CombatDamageResolver` results instead of Defense-owned health mutation.
- Breaking for custom adapters: `IDefenseCombatAdapter.ApplyObjectiveDamage` now returns `DamageResolutionResult`.
