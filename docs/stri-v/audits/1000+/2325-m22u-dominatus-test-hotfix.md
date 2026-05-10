# 2325 — M22u Dominatus test hotfix

## 1) Files changed
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/StriVEngineLifecycleRunnerTests.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Integration/EngineLifecycleCompositionTests.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EngineLifecycleRuntimeTests.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EntityLifecycleTestDriver.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/RootSceneLifecycleBridgeTests.cs`
- `docs/stri-v/audits/1000+/2325-m22u-dominatus-test-hotfix.md`

## 2) Task scope
Hotfix only. No new warning-cleanup bucket started. No camera/model-node cleanup continuation.

## 3) Failure reproduction
Command:
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v normal 2>&1 | tee /tmp/striv-m22u-hotfix-dominatus-tests.log`

Failing tests (initial reproduction):
- `...EntityLifecycleOrchestratorCallsiteIntegrationTests.EntityLifecycleDefaultPath_FullCycle_UsesDominatusOrchestratorThroughEngineCallsite`
- `...StriVEngineLifecycleRunnerTests.StriVEngineLifecycleRunner_AttachSceneTransformAndProcessor_CanceledBeforeStart_ThrowsOperationCanceledException`
- `...StriVEngineLifecycleRunnerTests.StriVEngineLifecycleRunner_CleanupProcessorLifecycle_RunsThroughDominatusRuntime`
- `...StriVEngineLifecycleRunnerTests.StriVEngineLifecycleRunner_RunSceneTransformProcessorFullCycle_RunsThroughDominatusRuntime`
- `...EngineLifecycleCompositionTests.RootSceneComposition_RemoveProcessorAndClearRootScene_ComposeThroughProductionAdapters`
- `...EngineLifecycleCompositionTests.RootSceneCleanupPolicy_ClearRootScene_ImplicitlyRemovesProcessorEntityMembership`
- `...RootSceneLifecycleBridgeTests.RootSceneLifecycleTransition_ClearRootScene_InvokesActuatorAndReturnsCompletedEvent`
- `...EntityLifecycleParityTests.EntityLifecycleParity_LegacyDirectAndDominatusOrchestratedFullCycle_ProduceSameSnapshot`
- `...EngineLifecycleRuntimeTests.DominatusRuntime_ComposedLifecycle_AddThenProcessorCleanup_ComposesThroughSampleStyleNode`

Assertion/exception details:
- `System.InvalidOperationException : Processor [RecordingProcessor] is not attached to an EntityManager.`
- `System.InvalidOperationException : Entity [RootEntity] is not registered with an EntityManager.`

Relevant first stack frames:
- `Stride.Engine.EntityProcessor.get_EntityManager()`
- `Stride.Engine.Entity.get_EntityManager()`
- Dominatus test callsites asserting `Assert.Null(processor.EntityManager)` / `Assert.Null(entity.EntityManager)` or equivalent null probing.

## 4) Root cause
Classification: **M22r guarded accessor impact + stale test expectation**.

The failing Dominatus tests probed detached lifecycle state by directly reading guarded non-null lifecycle accessors (`EntityProcessor.EntityManager`, `Entity.EntityManager`) and expecting null. After guarded accessor behavior, detached reads now correctly throw `InvalidOperationException` instead of returning null.

This was **not** caused by M22u camera/model-node nullable contract changes in production lifecycle paths.

## 5) Fix applied
Smallest correct fix: update affected Dominatus tests to assert detached state via explicit lifecycle flags (`IsAttached`, `IsManaged`) and additionally assert guarded accessors throw on detached reads.

No production lifecycle behavior changed.

## 6) Contract preservation
- Dominatus lifecycle assertions preserved: attach/detach sequencing and counts remain asserted.
- M22r guarded lifecycle property pattern preserved.
- M22u nullable contracts preserved unchanged.

## 7) Warning state
Focused warning count after hotfix build of `Stride.Engine`: **470** (same as M22u target result).

No warning-cascade expansion introduced by this hotfix.

## 8) Validation results
- `dotnet test ...StriV.Engine.Dominatus.Tests.csproj -v normal ...` → exit 0 (after fix rerun), pass.
- `dotnet test ...StriV.Engine.Dominatus.Tests.csproj --filter ... -v normal` → exit 0, pass.
- `dotnet test ...StriV.Engine.Dominatus.Tests.csproj -v minimal` → exit 0, pass (80/80).
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit 0, pass (68/68).
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, pass, warnings present expectedly.
- warning count extraction commands (`grep ...`, `wc -l ...`) → exit 0, count = 470.

Output truncated: yes for several long commands in terminal capture; no errors hidden in the summarized sections above.

## 9) Next recommendation
Green for hotfix objective. Resume camera slot lifecycle contract pass / finishing sweep.
