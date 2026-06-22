# Cross-Genre Reuse

## Idle Auto Defense

Idle Auto Defense can treat perimeter breaches as `ReportReachedObjective` and automated defeats as `ReportKilled`. Offline rewards and idle timers are intentionally out of scope and should compose through later progression and persistence packages.

## Classic Tower Defense

Classic Tower Defense can resolve each spawn request to a lane path with `DefenseRouteAssignment.FollowPath`. Towers, targeting, projectiles, placement, sell logic, and economy stay outside this package. Defense Games only owns the lifecycle bridge from spawn to route to killed, despawned, or leaked.

## Survivor Horde Defense

A survivor-style game can resolve spawns to player/base destinations. Contact damage can be routed through `IDefenseCombatAdapter`, while weapons, XP, pickups, and upgrade offers remain in later packages.

## Future ECS Boundary

An ECS package can mirror `DefenseRuntime` records into components or replace the adapters with job-friendly systems. The current package intentionally exposes small ports and snapshots so ECS integration can be additive instead of a hard dependency.
