# 2000 — Stride.Engine Dominatus migration pause report

## 1. Task scope

This milestone is a **pause/report checkpoint only** for the `Stride.Engine` Dominatus migration effort.

- Report only: no new lifecycle features are introduced.
- No runtime behavior changes are made.
- No tests are modified.
- No project references are changed.
- No continuation of M20f migration work is performed in this task.
- No warning cleanup is performed in this task.

The goal is to document what is already built and proven, identify current blockers, and justify a temporary pause while `Stride.Engine` nullability cleanup resumes.

## 2. Executive summary

The Dominatus-based migration path for `Stride.Engine` lifecycle formalization is viable and materially de-risked.

- The bridge architecture is functioning.
- Runtime `Ai.Act(...)` execution is functioning for migrated slices.
- Production adapters are functioning as side-effect boundaries.
- The engine-owned neutral seam is functioning.
- The entity lifecycle slice has a Dominatus-default **test** path.

However:

- No default **production runtime** path has been migrated yet.
- Broader `Stride.Engine` nullability/legacy lifecycle warning drag remains high.

**The migration machine is built and proven; the subsystem is not yet fully migrated into default runtime behavior.**

Given current evidence, Dominatus migration should pause now while `Stride.Engine` nullability cleanup resumes, then restart from a cleaner warning/lifecycle baseline.

## 3. What was built

### Vendored Dominatus

The following vendored runtime layers were brought into the repository:

- `striv/external/Dominatus/src/Dominatus.Core`
- `striv/external/Dominatus/src/Dominatus.OptFlow`
- `striv/external/Dominatus/src/Ariadne.OptFlow`
- `striv/external/Dominatus/src/Dominatus.UtilityLite`

### Bridge project

`striv/projects/StriV.Engine.Dominatus` was established to own Dominatus-facing lifecycle vocabulary and behavior composition, including:

- events/messages,
- actuator-facing interfaces,
- transition helpers,
- sample-style composed nodes,
- runtime actuation handlers.

### Adapter project

`striv/projects/StriV.Engine.Dominatus.Adapters` was established to own production boundary logic around existing Stride APIs, including:

- production wrappers over existing engine operations,
- containment of legacy null-as-detach conventions,
- side-effect isolation boundaries.

### Runtime project

`striv/projects/StriV.Engine.Dominatus.Runtime` was established to host opt-in lifecycle execution surfaces, including:

- `StriVEngineLifecycleRunner` (opt-in runtime runner),
- `IEntityLifecycleOrchestrator`,
- `DominatusEntityLifecycleOrchestrator`.

### Engine seam

`Stride.Engine` added/uses neutral lifecycle seams that do not bind the engine to Dominatus directly:

- `Stride.Engine/Engine/Lifecycle/IEntityLifecycleOrchestrator.cs`
- `EntityManager.RunEntityLifecycleFullCycleAsync(...)`

Important architecture boundary remains intact: `Stride.Engine` does **not** directly reference Dominatus packages nor StriV Dominatus bridge/runtime projects.

## 4. What was proven

### Bridge-level lifecycle transitions

Evidence to date demonstrates bridge-level transition correctness for:

- transform parent attach/detach,
- scene membership attach/detach,
- root-scene set/clear,
- processor system add/remove,
- processor entity add/remove,
- entity clone operation lifecycle proof.

### Runtime `Ai.Act(...)`

Evidence to date demonstrates real runtime execution via Dominatus for:

- transform lifecycle operations,
- scene lifecycle operations,
- processor lifecycle operations,
- sample-style composed node paths including:
  - scene attach (parent),
  - scene attach (child),
  - transform attach,
  - processor system add,
  - processor entity add,
- composed cleanup/remove sequence including:
  - processor entity remove,
  - processor system remove,
- full-cycle runner path (attach + cleanup).

### Engine-owned neutral callsite

`EntityManager.RunEntityLifecycleFullCycleAsync(...)` now delegates through an engine-owned neutral interface, allowing Dominatus-backed implementations from outside `Stride.Engine` without introducing direct Dominatus dependencies into engine internals.

### Test posture migration

For the migrated entity lifecycle slice, tests now default to Dominatus-orchestrated lifecycle behavior, while retaining a legacy direct path as parity/control.

## 5. What we learned

### Dominatus is a runtime kernel, not a plugin

This work confirms Dominatus is not being introduced as an AI behavior plugin module. It is being used as an explicit runtime kernel for lifecycle orchestration policy.

### `Stride.Engine` already behaves like an implicit lifecycle state machine

Managers, factories, callbacks, service registries, null sentinels, scheduler nodes, and implicit ordering together function as an informal lifecycle/HFSM architecture.

### Nullability cleanup alone is not conceptually sufficient

`null` often encodes hidden transitions such as:

- detached,
- cleared,
- not scheduled,
- disposed,
- consumed,
- unavailable.

Mechanically adding `?` can spread symptoms. Durable improvement requires explicit lifecycle state/transition vocabulary.

### But warning/noise drag is still operationally significant

Even with a proven Dominatus path, deeper production migration currently collides with broad legacy nullability/lifecycle warning volume. Warning cleanup remains a practical prerequisite for efficient next-step migration.

### Engine-owned seams are the correct boundary for protected/internal behavior

The M16k pattern (`EntityManager.AddEntityToProcessor(...)` / `RemoveEntityFromProcessor(...)`) validates the seam strategy:

- owner-scoped integration,
- no reflection,
- no private bypass,
- no Dominatus dependency leakage into `Stride.Engine`.

### Ordering policy must be explicit

Observed lifecycle behavior confirms ordering assumptions must be encoded explicitly (for example, scene attach before transform parenting), and explicit cleanup is preferred even where implicit cleanup may also occur.

### Actuators should contain side effects

Nodes should express intent through `Ai.Act(...)`; adapters should perform mutation. This keeps runtime policy composable and side effects bounded.

### C# async/await vs `Ai.Await`

The migration clarified division of concerns:

- public host APIs may use C# `ValueTask`/`async` patterns,
- Dominatus node behavioral suspension should use `Ai.Act(...)` and (for deferred flows) `Ai.Await(...)`,
- C# async/await should not be treated as behavioral agent suspension semantics.

## 6. Current architecture boundary

Current dependency direction:

```text
Stride.Engine
  ↑ referenced by
StriV.Engine.Dominatus.Adapters
  ↑ referenced by
StriV.Engine.Dominatus.Runtime
  ↑ references
StriV.Engine.Dominatus
  ↑ references
Dominatus.Core / Dominatus.OptFlow
```

Current ownership split:

- `Stride.Engine` owns neutral seams only.
- `StriV.Engine.Dominatus` owns lifecycle vocabulary and runtime handlers.
- `StriV.Engine.Dominatus.Adapters` owns production mutation wrappers.
- `StriV.Engine.Dominatus.Runtime` owns opt-in runtime hosting/orchestration.
- Default `Stride.Engine` runtime behavior remains unchanged.

## 7. Current limitations / blockers

1. No default production runtime path is migrated yet.
2. Runner completion currently relies on bounded tick loops because no strong terminal/completion API is exposed for this runner shape.
3. Broader `Stride.Engine` nullability warnings remain.
4. Larger subsystems (`SceneSystem`, `ScriptSystem`, graphics/device lifecycle, content loading) remain untouched.
5. Entity lifecycle migration is test-posture migrated, not default-runtime migrated.
6. Async/deferred actuation with `Ai.Await(...)` is not yet exercised in Stri-V engine lifecycle paths.
7. `Stride.Engine` warning cleanup is now the practical prerequisite before “final boss” production migration.

## 8. Why pause now

Pausing now is the highest-leverage decision based on current proof state:

- Bridge and runner machinery are sufficiently proven for the current slice.
- Additional proof-only iterations now have diminishing returns.
- Deeper production migration in the current warning swamp risks spending effort on noise management instead of architecture migration.
- The immediate practical next step is warning cleanup, with emphasis on nullability and null-as-state clusters.

**The next migration step should be code migration, not more proof, but the codebase needs a cleaner warning surface before that migration can be efficient.**

## 9. Resume criteria

Resume Dominatus production migration after these conditions are materially improved:

- `Stride.Engine` warning count reduced substantially.
- Obvious null-as-state clusters categorized and/or cleaned.
- Lifecycle-sensitive warnings in entity/scene/processor paths reduced.
- Focused engine warning buckets understood and tractable.
- First low-blast-radius production migration candidate selected.

## 10. Recommended immediate next work

Recommended immediate next track:

**Resume `Stride.Engine` nullability cleanup / Shine work.**

Suggested next milestone:

**M21a — `Stride.Engine` nullability cleanup re-baseline and target selection**

Purpose of M21a:

- capture current warning buckets,
- identify highest-yield nullability clusters,
- separate work into:
  - mechanical/local placeholder fixes,
  - lifecycle-field fixes,
  - null-as-state clusters,
  - deferrable scheduler/state-machine clusters,
- choose next file/subsystem target.

## 11. Future migration path after cleanup

After warning cleanup reaches a workable baseline:

1. Finish warning cleanup enough to reduce migration noise.
2. Choose first production migration candidate.
3. Route one real callsite through `EntityManager.RunEntityLifecycleFullCycleAsync(...)` (or another engine-owned seam) under opt-in guard.
4. Add parity tests.
5. Make Dominatus path default for that test path.
6. Remove/quarantine direct legacy lifecycle path.
7. Repeat subsystem-by-subsystem.

Candidate future targets:

- entity lifecycle callsites,
- root scene lifecycle,
- processor lifecycle,
- `EntityCloner` lifecycle (after serializer-host stability),
- later: `ScriptSystem`/scheduler,
- much later: graphics/device lifecycle.

## 12. Validation

This report-only milestone ran the required validation commands below (no code-behavior changes performed in this task):

1. `dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal`
2. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
3. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`

Optional focused/core checks may be run separately as needed.

All migration status claims in this report are grounded in the previously captured audit trail from M16a through M20e.
