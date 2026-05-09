# 1910 — M19b opt-in processor cleanup runner

## 1) Files changed
- `striv/projects/StriV.Engine.Dominatus.Runtime/StriVEngineLifecycleRunner.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/StriVEngineLifecycleRunnerTests.cs`

`Stride.Engine` production files changed: **none**.

## 2) Task scope
M19b adds the **second opt-in runner method** for lifecycle execution in `StriV.Engine.Dominatus.Runtime` and keeps cleanup scope constrained to processor operations only:
- remove processor entity membership;
- remove processor system registration.

No scene detach/transform detach cleanup was added. No runtime rewiring or generic runtime framework was introduced.

## 3) Runner design
- Added public method:
  - `CleanupProcessorLifecycleAsync(EntityManager entityManager, Entity child, EntityProcessor processor, CancellationToken cancellationToken = default)`
- Method validates all arguments and cancellation token.
- Registers existing production handlers/adapters only:
  - `ProcessorEntityRemoveActuationHandler(new StrideProcessorLifecycleActuator())`
  - `ProcessorSystemRemoveActuationHandler(new StrideProcessorLifecycleActuator())`
- Executes existing cleanup node:
  - `ProcessorLifecycleDominatusNodes.RemoveProcessorAndEntity(processor, entityManager, child)`
- Preserves bounded tick strategy with existing constants (`MaxTicks = 1`, fixed delta).
- Extracted a **private-only** helper to avoid duplication:
  - `RunSingleNodeAsync(ActuatorHost actuatorHost, AiNode node, CancellationToken cancellationToken)`

## 4) Tests
### `StriVEngineLifecycleRunner_CleanupProcessorLifecycle_RunsThroughDominatusRuntime`
- Setup uses real scene/entity manager/entities/processor and first calls `AttachSceneTransformAndProcessorAsync`.
- Pre-cleanup assertions check processor registration/add callback behavior.
- Executes `CleanupProcessorLifecycleAsync(entityManager, child, processor)`.
- Asserts remove callback count/entity, processor detachment from `EntityManager`, and manager processor collection removal.
- Intentionally does **not** assert scene/transform detach behavior (out of M19b scope).

### `StriVEngineLifecycleRunner_CleanupProcessorLifecycle_RejectsNullArguments`
- Verifies null rejection for representative inputs:
  - `entityManager`
  - `child`
  - `processor`

## 5) Behavior compatibility
- No default engine behavior changed.
- Dominatus adapters remain opt-in through runtime project usage.
- No direct Dominatus dependency added to `Stride.Engine`.

## 6) Validation results
1. `dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: pass
   - Output truncated: no

2. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: pass
   - Output truncated: no

3. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m19b-engine-focused.log`
   - Exit code: `0`
   - First meaningful warning/error: existing `Stride.Engine` nullability warning (`CS8767` in `Animations/AnimationChannel.cs`)
   - Pass/fail: pass
   - Output truncated: yes

4. `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: existing assembly processor warning (`CS1030` ObjectIdBuilder PERF warning)
   - Pass/fail: pass
   - Output truncated: yes

5. `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: pass
   - Output truncated: no

6. `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: pass
   - Output truncated: no

7. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: pass
   - Output truncated: no

8. `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: existing `CS0618` obsolete warnings in tests
   - Pass/fail: pass
   - Output truncated: yes

9. `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: pass
   - Output truncated: no

10. `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
    - Exit code: `0`
    - First meaningful warning/error: none
    - Pass/fail: pass
    - Output truncated: no

11. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
    - Exit code: `0`
    - First meaningful warning/error: none
    - Pass/fail: pass
    - Output truncated: no

12. `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
    - Exit code: `0`
    - First meaningful warning/error: none
    - Pass/fail: pass
    - Output truncated: no

13. `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
    - Exit code: `0`
    - First meaningful warning/error: none
    - Pass/fail: pass
    - Output truncated: no

14. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
    - Exit code: `0`
    - First meaningful warning/error: one existing skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`)
    - Pass/fail: pass
    - Output truncated: no

15. `./striv/build/striv-build-core.sh`
    - Exit code: `0`
    - First meaningful warning/error: none
    - Pass/fail: pass
    - Output truncated: yes

## 7) Recommended next task
**M19c runtime runner completion contract hardening**: codify and test completion/timeout/cancellation behavior for opt-in runner methods while keeping scope inside the runtime project.
