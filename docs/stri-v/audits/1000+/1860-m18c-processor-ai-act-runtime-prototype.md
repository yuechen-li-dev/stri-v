# 1860 — M18c processor `Ai.Act(...)` runtime prototype

## 1. Files changed

- `striv/projects/StriV.Engine.Dominatus/Events/ProcessorLifecycleEvents.cs`
- `striv/projects/StriV.Engine.Dominatus/Runtime/ProcessorLifecycleActuation.cs` (new)
- `striv/projects/StriV.Engine.Dominatus/Nodes/ProcessorLifecycleDominatusNodes.cs` (new)
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/ProcessorLifecycleRuntimeTests.cs` (new)

Stride.Engine production files changed: **none**.

## 2. Task scope

M18c added runtime opt-in processor lifecycle actuation for Dominatus `Ai.Act(...)` on:
- `ProcessorSystemAddRequested`
- `ProcessorEntityAddRequested`

No runtime rewiring was performed, and no warning cleanup work was attempted.

## 3. Runtime design

- Processor lifecycle request records now implement `IActuationCommand` (add/remove system + add/remove entity requests).
- Added runtime handlers:
  - `ProcessorSystemAddActuationHandler`
  - `ProcessorEntityAddActuationHandler`
- Each handler calls `ProcessorLifecycleTransition` helper and returns `ActuatorHost.HandlerResult.CompletedWithPayload(completedEvent)`.
- Transition helpers call `IProcessorLifecycleActuator` (production adapter: `StrideProcessorLifecycleActuator`).
- Dominatus node surface added in `ProcessorLifecycleDominatusNodes`:
  - `AddProcessorToSystem(...)`
  - `AddEntityToProcessor(...)`
  - `AddProcessorAndEntity(...)`
- Runtime execution path remains the real Dominatus path:
  - `ActuatorHost.Register(...)`
  - `AiWorld`
  - `AiAgent`
  - `HfsmInstance`
  - `world.Tick(dt)`

## 4. Tests

Added runtime tests in `ProcessorLifecycleRuntimeTests`:

1. `DominatusRuntime_AddProcessorToSystem_ActsThroughProductionAdapter`
   - Runtime path: node yields `Ai.Act(ProcessorSystemAddRequested)`.
   - Asserts processor is attached to manager and retrievable from manager.
   - Adapter: `StrideProcessorLifecycleActuator` via `ProcessorSystemAddActuationHandler`.

2. `DominatusRuntime_AddEntityToProcessor_ActsThroughProductionAdapter`
   - Runtime path: node yields `Ai.Act(ProcessorEntityAddRequested)`.
   - Asserts processor callback fires exactly once and entity identity matches.
   - Adapter: `StrideProcessorLifecycleActuator` via `ProcessorEntityAddActuationHandler`.

3. `DominatusRuntime_AddProcessorAndEntity_ComposesThroughProductionAdapter`
   - Runtime path: node yields system add then entity add.
   - Asserts processor bound to manager and add callback count is exactly one with expected entity.
   - Adapter: same production adapter through both runtime handlers.

Completion behavior observed: all runtime tests pass with completed actuation payload semantics (handler returns completed payload only after transition success).

## 5. Behavior compatibility

- No `Stride.Engine` behavior was rewired.
- No direct Dominatus dependency was added to `Stride.Engine`.
- Adapter path remains opt-in/test-wired via Dominatus runtime host registration.

## 6. Runtime harness observations

There is repeated `AiWorld` / `AiAgent` / `HfsmInstance` setup across runtime tests. For M18c this stayed minimal with a local helper in `ProcessorLifecycleRuntimeTests`. M18d could extract a small shared runtime harness helper if duplication continues to grow.

## 7. Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` | 0 | Existing Stride warning set (e.g., `CS1030` perf #warning in `Stride.Core`) | Pass | Yes |
| `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m18c-engine-focused.log` | 0 | Existing engine warnings (e.g., nullability warnings) | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | Existing warnings across legacy areas | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | Existing compile warnings upstream of test build | Pass | Yes |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | Existing obsolete warnings in tests | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | 1 known skipped test | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | None | Pass | No |

## 8. Recommendation

Proceed with **M18d composed scene+transform+processor runtime prototype**, and consider a small shared runtime harness extraction only if test setup duplication grows further.
