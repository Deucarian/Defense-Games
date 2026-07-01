# Deucarian Defense Games

`com.deucarian.defense-games` is the shared composition layer for defense-style games.

It coordinates Encounters spawn output, World Spawning objects, World Navigation movement, and Combat-backed objectives. Objective damage flows through `CombatDamageResolver`; this package does not own shield absorption, health damage math, armor, resistance, penetration, critical hits, overkill, tower placement, projectiles, weapon scheduling, upgrades, progression, persistence, UI, audio/VFX, pathfinding, NavMesh, grid construction, balance, or concrete Auto Defense/Tower Defense rules.

## Install

Stable:

```json
"com.deucarian.defense-games": "https://github.com/Deucarian/Defense-Games.git#main"
```

Development:

```json
"com.deucarian.defense-games": "https://github.com/Deucarian/Defense-Games.git#develop"
```

Use `#main` for stable package consumption and `#develop` when testing active package work.

## When To Use This

Use this package when you need Shared defense-game orchestration for encounter spawn requests, world spawning, world navigation, combat-backed objectives, agents, signals, metrics, and snapshots.

Do not use this package to take ownership of capabilities outside its `AGENTS.md` boundary. Reusable behavior should stay with the package that owns that capability in the Package Registry governance docs.

## Quick Start

1. Install the package through Deucarian Package Installer or Unity Package Manager using the URL above.
2. Let Unity finish resolving packages and compiling assemblies.
3. Import the `Defense Games Minimal` sample if you want a working reference scene or setup.
4. Start from the package README sections above and the public runtime/editor APIs in this repository.

## Integrations

Direct Deucarian package dependencies:

- `com.deucarian.gameplay-foundation`
- `com.deucarian.encounters`
- `com.deucarian.combat`
- `com.deucarian.world-spawning`
- `com.deucarian.world-navigation`

Install optional companion packages only when their owned capability is needed by production code, samples, or tests.

## Validation

Run the shared package validator from this repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Documentation-only updates should still pass:

```powershell
git diff --check
```

## Troubleshooting

- Package does not resolve: confirm the stable or development Git URL matches the Package Registry entry and that required Deucarian dependencies are installed.
- Unity compile errors after install: let Package Manager finish resolving dependencies, then check asmdef references against `package.json` dependencies.
- Behavior appears to belong in another package: consult `AGENTS.md` and the Package Registry governance docs before moving or duplicating code.

## License

MIT. See `LICENSE.md`.
