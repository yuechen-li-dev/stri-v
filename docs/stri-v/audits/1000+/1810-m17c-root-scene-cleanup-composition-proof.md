# 1810 — M17c Root-Scene Cleanup Composition Proof

## 1) Files changed
- `striv/tests/StriV.Engine.Dominatus.Tests/Integration/EngineLifecycleCompositionTests.cs`
- `docs/stri-v/audits/1000+/1810-m17c-root-scene-cleanup-composition-proof.md`

Production files changed: **No**.

## 2) Task scope
This slice adds a bounded integration proof for explicit cleanup composition through existing Dominatus transitions and production adapters:
- root-scene set/clear via `SceneLifecycleTransition`,
- processor system add/remove via `ProcessorLifecycleTransition`,
- processor entity add/remove via `ProcessorLifecycleTransition`.

No runtime rewiring, no SceneSystem migration, no scheduler/script migration, and no warning cleanup were introduced.

## 3) Composition flow
Proven transition sequence (explicit cleanup ordering):
1. `RootSceneSetRequested` -> `SceneLifecycleTransition.SetRootSceneAsync(...)` -> `RootSceneSet`
2. `ProcessorSystemAddRequested` -> `ProcessorLifecycleTransition.AddProcessorToSystemAsync(...)` -> `ProcessorSystemAdded`
3. `ProcessorEntityAddRequested` -> `ProcessorLifecycleTransition.AddEntityToProcessorAsync(...)` -> `ProcessorEntityAdded`
4. `ProcessorEntityRemoveRequested` -> `ProcessorLifecycleTransition.RemoveEntityFromProcessorAsync(...)` -> `ProcessorEntityRemoved`
5. `ProcessorSystemRemoveRequested` -> `ProcessorLifecycleTransition.RemoveProcessorFromSystemAsync(...)` -> `ProcessorSystemRemoved`
6. `RootSceneClearRequested` -> `SceneLifecycleTransition.ClearRootSceneAsync(...)` -> `RootSceneCleared`

## 4) Tests
### Updated integration proof
`RootSceneComposition_RemoveProcessorAndClearRootScene_ComposeThroughProductionAdapters`

Adapters used:
- `StrideSceneLifecycleActuator`
- `StrideProcessorLifecycleActuator`

Assertions include:
- completed-event identity at every stage;
- root-scene set state and managed entity membership;
- processor bound to manager after add;
- processor add callback evidence (`AddedCount`, entity identity);
- explicit processor-entity remove callback evidence (`RemovedCount`, entity identity);
- explicit processor-system remove evidence (`processor.EntityManager == null`, processor no longer in manager collection);
- root-scene clear state (`sceneInstance.RootScene == null`) and entity unmanaged state (`entity.EntityManager == null`).

## 5) Observed lifecycle policy
- Cleanup ordering is explicit in the proof: remove entity from processor first, remove processor from system second, clear root scene last.
- The proof does not assume root-scene clear performs processor cleanup implicitly.
- Root-scene clear is asserted only for root-scene cleared state and entity manager unmanagement.
- Ordering keeps policy deterministic and avoids coupling to unproven implicit processor removal behavior.

## 6) Behavior compatibility
- No engine runtime behavior changed.
- No direct Dominatus dependency added to `Stride.Engine`.
- Adapters remain opt-in composition boundaries.

## 7) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` | 0 | existing transitive warnings (e.g., `CS1030 PERF: Do not copy byte-for-byte.`) | Pass | Yes |
| `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m17c-engine-focused.log` | 0 | existing engine warnings (e.g., `CS8765` nullability mismatch) | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | existing warning set (e.g., `CS1030`, `RS1036`) | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | expected legacy/fixture warnings (e.g., `CS0618 OldCollectionDescriptor`) | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one expected skip (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | Yes |

## 8) Recommended next task
**Root-scene automatic cleanup policy proof** (bounded follow-up):
- add a dedicated observation proof to characterize whether current Stride root-scene clear has any implicit processor callback effects,
- keep assertions observational (no behavior imposition),
- preserve adapter/transition-only scope.
