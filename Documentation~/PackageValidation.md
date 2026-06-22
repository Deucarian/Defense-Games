# Package Validation

Validation target:

- Unity editor: `6000.3.5f1`
- Validation project: `C:\Repositories\Deucarian\DefenseGames-TestProject`
- Package path: `C:\Repositories\Deucarian\Defense-Games`
- Package reference mode: local file package

## Completion Gate

- Package imports without errors.
- EditMode tests pass twice.
- PlayMode tests pass twice.
- Real World Spawning, World Navigation, Encounters, and Combat adapters are covered by tests.
- Donor, Idle Auto Defense, and classic Tower Defense proofs are covered by tests and documentation.
- No circular dependencies or duplicated lower-package responsibility.
- Allocation and timing benchmark is recorded honestly in `Logs/defense-games-benchmark-results.json`.

## Commands

```powershell
Unity.exe -batchmode -quit -projectPath C:\Repositories\Deucarian\DefenseGames-TestProject -logFile C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\import.log
Unity.exe -batchmode -projectPath C:\Repositories\Deucarian\DefenseGames-TestProject -runTests -testPlatform EditMode -testResults C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\editmode-results.xml -logFile C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\editmode.log
Unity.exe -batchmode -projectPath C:\Repositories\Deucarian\DefenseGames-TestProject -runTests -testPlatform PlayMode -testResults C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\playmode-results.xml -logFile C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\playmode.log
```

## Results

## Phase 1L Results

- EditMode pass 1: `12` total, `12` passed, `0` failed. Results: `TestResults-Phase1L-DefenseGames-EditMode-1.xml`.
- EditMode pass 2: `12` total, `12` passed, `0` failed. Results: `TestResults-Phase1L-DefenseGames-EditMode-2.xml`.
- PlayMode pass 1: `1` total, `1` passed, `0` failed. Results: `TestResults-Phase1L-DefenseGames-PlayMode-1.xml`.
- PlayMode pass 2: `1` total, `1` passed, `0` failed. Results: `TestResults-Phase1L-DefenseGames-PlayMode-2.xml`.

Defense Games remains the composition layer that consumes Encounter `SpawnRequest` values and maps them through `WorldSpawnDefenseAdapter` to generic World Spawning requests.

- Import: passed, no compiler or package-manager errors. The log contains a Unity licensing token warning, but entitlement resolution succeeds.
- EditMode pass 1: `9` total, `9` passed, `0` failed. Results: `C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\editmode-results-1.xml`.
- EditMode pass 2: `9` total, `9` passed, `0` failed. Results: `C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\editmode-results-2.xml`.
- PlayMode pass 1: `1` total, `1` passed, `0` failed. Results: `C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\playmode-results-1.xml`.
- PlayMode pass 2: `1` total, `1` passed, `0` failed. Results: `C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\playmode-results-2.xml`.

## Benchmark

`C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\defense-games-benchmark-results.json`

- `1,000` spawn/register/kill/cleanup operations: `8.140 ms`, `0` bytes allocated.
- `5,000` spawn/register/kill/cleanup operations: `21.078 ms`, `0` bytes allocated.
- `10,000` spawn/register/kill/cleanup operations: `43.661 ms`, `0` bytes allocated.

The benchmark uses fake warmed adapters so it measures the Defense Games composition hot path, not GameObject instantiation, pooling warmup, rendering, physics, or pathfinding.

## Phase 1I Closeout Results

- Import command: `Unity.exe -batchmode -quit -projectPath C:\Repositories\Deucarian\DefenseGames-TestProject -logFile C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\phase1i-import.log`
- Import result: passed, no compiler or package-manager errors. The log contains a Unity licensing token warning, but entitlement resolution succeeds.
- EditMode pass 1: `12` total, `12` passed, `0` failed. Results: `C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\phase1i-editmode-results-2.xml`.
- EditMode pass 2: `12` total, `12` passed, `0` failed. Results: `C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\phase1i-editmode-results-3.xml`.
- PlayMode pass 1: `1` total, `1` passed, `0` failed. Results: `C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\phase1i-playmode-results-2.xml`.
- PlayMode pass 2: `1` total, `1` passed, `0` failed. Results: `C:\Repositories\Deucarian\DefenseGames-TestProject\Logs\phase1i-playmode-results-3.xml`.

Phase 1I added focused coverage that objective damage uses Combat resolver mitigation, objective shield absorption comes from Combat, objective failure comes from Combat life state, invalid Combat requests do not mutate objective health/shield, enemy death still updates active count, and leak/objective contact events still emit.

Benchmark output after the repeat run:

- `1,000` spawn/register/kill/cleanup operations: `3.475 ms`, `0` bytes allocated.
- `5,000` spawn/register/kill/cleanup operations: `20.567 ms`, `0` bytes allocated.
- `10,000` spawn/register/kill/cleanup operations: `44.045 ms`, `0` bytes allocated.
