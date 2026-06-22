# Deucarian Defense Games

`com.deucarian.defense-games` is the shared composition layer for defense-style games.

It coordinates Encounters spawn output, World Spawning objects, World Navigation movement, and Combat-backed objectives. Objective damage flows through `CombatDamageResolver`; this package does not own shield absorption, health damage math, armor, resistance, penetration, critical hits, overkill, tower placement, projectiles, weapon scheduling, upgrades, progression, persistence, UI, audio/VFX, pathfinding, NavMesh, grid construction, balance, or concrete Auto Defense/Tower Defense rules.
