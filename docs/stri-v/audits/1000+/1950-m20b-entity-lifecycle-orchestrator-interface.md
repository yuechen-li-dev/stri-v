# 1950 — M20b entity lifecycle orchestrator interface

## 1) Files changed

- `striv/projects/StriV.Engine.Dominatus.Runtime/IEntityLifecycleOrchestrator.cs`
- `striv/projects/StriV.Engine.Dominatus.Runtime/DominatusEntityLifecycleOrchestrator.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EntityLifecycleOrchestratorTests.cs`
- `striv/projects/StriV.Engine.Dominatus.Runtime/README.md`
- `docs/stri-v/audits/1000+/1950-m20b-entity-lifecycle-orchestrator-interface.md`

`Stride.Engine` production files changed: **none**.

## 2) Task scope

M20b introduced a callsite-facing opt-in orchestrator interface in `StriV.Engine.Dominatus.Runtime`, plus a Dominatus-backed implementation that delegates to the existing runner. No runtime rewiring and no engine callsite migration were performed.

## 3) Interface design

`IEntityLifecycleOrchestrator` mirrors the existing runner primitives:

- `AttachSceneTransformAndProcessorAsync(...)`
- `CleanupProcessorLifecycleAsync(...)`
- `DetachTransformParentAsync(...)`
- `DetachEntityFromSceneAsync(...)`
- `RunSceneTransformProcessorFullCycleAsync(...)`

The interface uses Stride engine lifecycle types (`Scene`, `Entity`, `EntityManager`, `EntityProcessor`) and exposes no Dominatus runtime types.

## 4) Implementation design

`DominatusEntityLifecycleOrchestrator` is a thin adapter over `StriVEngineLifecycleRunner`.

- Default constructor creates a runner for simple opt-in usage.
- Runner-injection constructor validates null and supports explicit composition.
- Each interface method directly delegates to its runner counterpart.

No new behavior, no service registry, and no DI container were added.

## 5) Tests

Added `EntityLifecycleOrchestratorTests` covering:

1. `AttachSceneTransformAndProcessorAsync` via interface:
   - asserts scene attach, transform parenting, processor add callback and target entity.
2. `CleanupProcessorLifecycleAsync` via interface:
   - asserts processor remove callback and processor detachment from manager,
   - confirms entity scene/transform linkage remains intact.
3. `RunSceneTransformProcessorFullCycleAsync` via interface:
   - asserts final cleanup state (scene detach, transform detach, processor removed),
   - asserts add/remove callback counts.
4. constructor guard:
   - `DominatusEntityLifecycleOrchestrator(null!)` throws `ArgumentNullException`.

These tests prove interface calls flow through the same Dominatus-backed runner path.

## 6) Behavior compatibility

- No default engine behavior changed.
- No direct Dominatus dependency was added to `Stride.Engine`.
- Orchestrator surface remains fully opt-in and external to `Stride.Engine`.

## 7) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m20b-engine-focused.log` | 0 | existing nullable warnings in `Stride.Engine` | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 1 | `CS0006` metadata file not found (ref assemblies) | Fail | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 1 | focused build failures for `Stride.BepuPhysics`, `Stride.Input`, `Stride.Games` | Fail | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one test skipped by existing suite metadata | Pass | Yes |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | Yes |

## 8) Recommended next task

**M20c engine-owned neutral orchestrator seam**: define a neutral engine-facing abstraction target (still without runtime rewiring) so first production opt-in migration can bind to that seam without importing Dominatus internals.
