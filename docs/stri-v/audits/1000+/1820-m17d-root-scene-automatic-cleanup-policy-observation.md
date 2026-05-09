# 1820 — M17d root-scene automatic cleanup policy observation

## 1) Files changed
- `striv/tests/StriV.Engine.Dominatus.Tests/Integration/EngineLifecycleCompositionTests.cs`
- `docs/stri-v/audits/1000+/1820-m17d-root-scene-automatic-cleanup-policy-observation.md`

Production files changed: **none**.

## 2) Task scope
This slice is an **observational cleanup policy proof** only.
No runtime behavior was modified, no adapter wiring changed, and no cleanup policy was imposed or rewritten.

## 3) Current behavior inspection
Inspection findings:
- `SceneInstance.RootScene = null` calls `Remove(rootScene)` before clearing renderer types.
- `SceneInstance.Remove(Scene)` iterates scene entities and calls `Remove(entity)`.
- `EntityManager.Remove(entity)` calls `InternalRemoveEntity(entity, true)`.
- `InternalRemoveEntity` calls `CheckEntityWithProcessors(entity, forceRemove: true, ...)`.
- `CheckEntityWithProcessors(... forceRemove: true ...)` routes component matches through processor `ProcessEntityComponent(..., forceRemove: true)` and then unmanages entity (`entity.EntityManager = null`).
- `EntityProcessor<TComponent, TData>.ProcessEntityComponent(..., forceRemove: true)` takes the remove branch for previously associated entries and invokes `OnEntityComponentRemoved`.

Therefore current code path suggests root-scene clear does pass through processor removal callbacks and unmanages entities.

## 4) Observation test
Added test:
- `RootSceneCleanupPolicy_ClearRootScene_ImplicitlyRemovesProcessorEntityMembership`

Setup and sequence:
1. Create `SceneInstance`, root `Scene`, entity with test component, and recording processor.
2. Set root scene via `SceneLifecycleTransition.SetRootSceneAsync` + `StrideSceneLifecycleActuator`.
3. Add processor to system via `ProcessorLifecycleTransition.AddProcessorToSystemAsync` + `StrideProcessorLifecycleActuator`.
4. Add entity to processor via `ProcessorLifecycleTransition.AddEntityToProcessorAsync` and verify add callback count.
5. Clear root scene **without explicit processor entity removal** via `SceneLifecycleTransition.ClearRootSceneAsync`.
6. Observe and assert processor remove callback count.

Observed result: `RemovedCount == 1`, with the same entity captured as removed.

## 5) Policy conclusion
Observed behavior: **root-scene clear currently does implicitly trigger processor entity removal callbacks/membership cleanup** in this path.

Recommended Dominatus policy: keep **explicit processor cleanup before root-scene clear** as deterministic doctrine for lifecycle clarity/composition guarantees, while recognizing current engine behavior also performs implicit removal during root-scene clear.

## 6) Behavior compatibility
- No engine behavior changed.
- No direct Dominatus dependency added to `Stride.Engine`.
- Adapters remain opt-in and unchanged.

## 7) Validation results
1. `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing nullable/#warning warnings from legacy Stride dependencies.
   - Pass/Fail: Pass
   - Output truncated: Yes
2. `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
3. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
4. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m17d-engine-focused.log`
   - Exit code: 0
   - First meaningful warning/error: existing Stride.Engine nullability warnings
   - Pass/Fail: Pass
   - Output truncated: Yes
5. `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing solution warnings (e.g., analyzer/nullability/obsolete usage)
   - Pass/Fail: Pass
   - Output truncated: Yes
6. `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
   - Exit code: 0
   - First meaningful warning/error: none (focused summary shows pass)
   - Pass/Fail: Pass
   - Output truncated: No
7. `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
8. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
9. `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing obsolete-field warnings in test code
   - Pass/Fail: Pass
   - Output truncated: No
10. `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
11. `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
12. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
13. `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
14. `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
15. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
16. `./striv/build/striv-build-core.sh`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: Yes

## 8) Recommended next task
**EntityCloner bounded operation lifecycle proof**.
Rationale: it is a bounded follow-up that can extend the lifecycle evidence chain without runtime policy rewiring.
