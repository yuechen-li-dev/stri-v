# M19d — Opt-in scene/transform detach runner cleanup

## 1) Files changed

- `striv/projects/StriV.Engine.Dominatus/Events/TransformLifecycleEvents.cs`
- `striv/projects/StriV.Engine.Dominatus/Events/SceneLifecycleEvents.cs`
- `striv/projects/StriV.Engine.Dominatus/Runtime/TransformLifecycleActuation.cs`
- `striv/projects/StriV.Engine.Dominatus/Runtime/SceneLifecycleActuation.cs`
- `striv/projects/StriV.Engine.Dominatus/Nodes/TransformLifecycleDominatusNodes.cs`
- `striv/projects/StriV.Engine.Dominatus/Nodes/SceneLifecycleDominatusNodes.cs`
- `striv/projects/StriV.Engine.Dominatus.Runtime/StriVEngineLifecycleRunner.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/StriVEngineLifecycleRunnerTests.cs`
- `docs/stri-v/audits/1000+/1930-m19d-opt-in-scene-transform-detach-runner.md`

`Stride.Engine` production files changed: **No**.

## 2) Task scope

This task adds two small opt-in cleanup methods to the existing Dominatus lifecycle runner:

- transform parent detach,
- entity scene detach.

No default runtime rewiring was introduced, and no warning-cleanup work was attempted.

## 3) Runtime additions

- Commands marked as actuation commands:
  - `TransformParentDetachRequested : IActuationCommand`
  - `EntitySceneDetachRequested : IActuationCommand`
- Handlers added:
  - `TransformParentDetachActuationHandler`
  - `EntitySceneDetachActuationHandler`
- Sample-style nodes added:
  - `TransformLifecycleDominatusNodes.DetachTransformParent(AiCtx, Entity)`
  - `SceneLifecycleDominatusNodes.DetachEntityFromScene(AiCtx, Entity)`
- Runner methods added:
  - `DetachTransformParentAsync(Entity, CancellationToken)`
  - `DetachEntityFromSceneAsync(Entity, CancellationToken)`

## 4) Runner design

Public runner methods are explicit opt-in APIs and each registers only the required handler/adapter pair:

- Transform detach: `TransformParentDetachActuationHandler` + `StrideTransformLifecycleActuator`
- Scene detach: `EntitySceneDetachActuationHandler` + `StrideSceneLifecycleActuator`

Both methods reuse existing M19c bounded-tick `RunSingleNodeAsync` behavior and existing cancellation guards before start and per tick.

## 5) Tests

### `StriVEngineLifecycleRunner_DetachTransformParent_RunsThroughDominatusRuntime`

- Setup: parent+child entities with child parented.
- Method: `DetachTransformParentAsync(child)`.
- Assertions: child parent cleared; parent transform children no longer include child transform.
- Ordering isolation: no scene dependency.

### `StriVEngineLifecycleRunner_DetachEntityFromScene_RunsThroughDominatusRuntime`

- Setup: scene+entity with entity attached to scene.
- Method: `DetachEntityFromSceneAsync(entity)`.
- Assertions: entity scene cleared; scene entities no longer include entity.
- Ordering isolation: no transform-parent dependency.

### `StriVEngineLifecycleRunner_DetachCleanupMethods_RejectNullArguments`

- Verifies null rejection for both new cleanup entry points.

## 6) Behavior compatibility

- Default engine behavior unchanged.
- Dominatus adapters remain opt-in through `StriVEngineLifecycleRunner`.
- No direct Dominatus dependency was added to `Stride.Engine`.

## 7) Validation results

### Command
`dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m19d-engine-focused.log`
- Exit code: `0`
- First meaningful warning/error: existing warnings present in focused engine build output; no new M19d warning-cleanup scope changes made
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
- Exit code: `0`
- First meaningful warning/error: pre-existing solution warnings may appear; no M19d warning-cleanup changes attempted
- Pass/fail: **Pass**
- Output truncated: no

### Command
`./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

### Command
`./striv/build/striv-build-core.sh`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **Pass**
- Output truncated: no

## 8) Recommended next task

Recommended next task: **M19e composed attach+detach full lifecycle runner** as a small additive opt-in composition over the now explicit attach/cleanup primitives.
