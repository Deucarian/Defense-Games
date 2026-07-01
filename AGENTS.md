# Deucarian Defense Games Agent Notes

Package ID: `com.deucarian.defense-games`
Repository: `Deucarian/Defense-Games`

Follow the canonical Deucarian governance docs in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md), especially capability ownership and dependency rules.

## Ownership

This package owns:

- Shared defense-game orchestration for encounter spawn requests, world spawning, world navigation assignment, combat-backed objectives, attacker agents, defense signals, metrics, and snapshots.

Registered capabilities:
- None.

This package must not own:

- Auto-defense genre rules, tower/mounted weapon orchestration, attack definitions, projectile lifecycle, weapon cadence, progression/rewards, persistence, UI, monetization, template scenes, or product-specific balance.

## Dependencies

Allowed dependency shape:

- May depend on Gameplay Foundation, Encounters, Combat, World Spawning, and World Navigation to compose generic defense-game runtime flows.

Required dependencies and why:

- `com.deucarian.gameplay-foundation`: shared gameplay IDs and deterministic primitives.
- `com.deucarian.encounters`: encounter spawn request source data.
- `com.deucarian.combat`: objective damage and combat-backed state.
- `com.deucarian.world-spawning`: spawned attacker/object instances.
- `com.deucarian.world-navigation`: navigation assignment for spawned agents.

Optional/version-defined dependencies:

- None.

Architecture exceptions:

- None.

## Policies

- Keep this package focused on genre-neutral defense orchestration.
- Do not add hard dependencies on Auto Defense, Attacks, Projectiles, Weapon Systems, Run Upgrades, Progression, Persistence, UI, Monetization, or template packages.
- Product-specific defense rules and starter scenes belong in framework/template packages.
- Logging: Do not introduce direct Unity Debug calls.
- Unity object lifetime: Use Common only if production code directly owns transient Unity object cleanup.
- Testing: Test fixture teardown may use Unity `DestroyImmediate` directly.

## Validation

Run the shared validator before committing:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Also run existing repository tests when changing code or asmdefs. Documentation-only updates should still run `git diff --check`.

## Codex Guidance

- Inspect current files before changing anything.
- Work on `develop`; do not edit or merge `main` unless the task is promotion-only.
- Do not edit `Library/PackageCache`.
- Do not guess package versions or dependency versions.
- Do not add package dependencies casually; update asmdefs, `package.json`, `deucarian-package.json`, Package Registry, Package Installer fallback, and Bootstrap fallback together when a dependency is truly required.
- Do not create local copies of shared helpers.
- Keep commits focused and report exactly what changed and what was validated.
